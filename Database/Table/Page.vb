Imports System.IO
Imports System.Threading

Public Class Page
    Implements IDisposable

    Public DatabaseConfig As DatabaseConfig

    Public TableName As String
    Public ID As Int64
    Public FilePath As String   'Page File Path
    Public IfFileExists As Boolean

    Public PendingRemoveBlocksFilePath As String    'Page File's Pending Remove Data Blocks. StartPOS(8 Bytes)+BlockLength(6 Bytes)

    Public PageFileBufferWriter As FileBufferWriter

    Public Sub New(ByVal TableName As String, ByVal ID As Int64, ByRef DatabaseConfig As DatabaseConfig)
        Me.TableName = TableName
        Me.ID = ID
        Me.DatabaseConfig = DatabaseConfig

        'Init Mutexes
        CreateMutexes()

        'Cal FilePath
        FilePath = GetPageFilePath(ID)
        PendingRemoveBlocksFilePath = GetPageRBFilePath(ID)

        'Create PageHeaderMemory
        CreatePageHeaderMemory()

        'Check if need to Create PageFile
        If File.Exists(FilePath) Then
            IfFileExists = True
        Else
            IfFileExists = False
            CreatePageFile()
            If File.Exists(FilePath) Then IfFileExists = True
        End If

        'Check if need to Create PendingRemoveBlocksFile
        If File.Exists(PendingRemoveBlocksFilePath) = False Then
            CreatePendingRemoveBlocksFile()
        End If

        'Create Buffer Writer if SupportWriteBuffer
        If DatabaseConfig.SupportWriteBuffer Then
            PageFileBufferWriter = New FileBufferWriter(FilePath, DatabaseConfig.DataPageWriteBufferSize, DatabaseConfig.WriteBufferFlushMSeconds)
        End If
    End Sub

#Region "Mutexes"

    Private FileMutex As MutexACL
    Private FileRBMutex As MutexACL
    Private HeaderMutex As MutexACL

    Private IndexMutexes As New Concurrent.ConcurrentDictionary(Of Integer, MutexACL)

    Private Sub CreateMutexes()
        FileMutex = New MutexACL("Global\FDB-" & DatabaseConfig.DatabaseKey & "-" & TableName & "-P" & ID & "FileMutex")
        FileRBMutex = New MutexACL("Global\FDB-" & DatabaseConfig.DatabaseKey & "-" & TableName & "-P" & ID & "RBMutex")
        HeaderMutex = New MutexACL("Global\FDB-" & DatabaseConfig.DatabaseKey & "-" & TableName & "-P" & ID & "HeaderMutex")
    End Sub

    Private CreateIndexMutexLock As New Object

    Private Function GetOneIndexMutex(ByVal DataID As Int64) As MutexACL
        'Get IndexMutexsID
        Dim IndexMutexID As Integer
        IndexMutexID = DataID Mod DatabaseConfig.PageHeaderIndexMutexesSize

        'Check if exists
        If IndexMutexes.ContainsKey(IndexMutexID) Then Return IndexMutexes(IndexMutexID)

        'Create a new one
        SyncLock CreateIndexMutexLock
            If IndexMutexes.ContainsKey(IndexMutexID) Then Return IndexMutexes(IndexMutexID)

            Dim FMutex As New MutexACL("Global\FDBP" & ID & "Index" & IndexMutexID & "Mutex")
            IndexMutexes.TryAdd(IndexMutexID, FMutex)

            Return FMutex
        End SyncLock
    End Function


    Private Sub DisposeAllMutexes()
        If FileMutex IsNot Nothing Then
            Try
                FileMutex.Dispose()
                FileMutex = Nothing
            Catch ex As Exception
                If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
            End Try
        End If

        If FileRBMutex IsNot Nothing Then
            Try
                FileRBMutex.Dispose()
                FileRBMutex = Nothing
            Catch ex As Exception
                If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
            End Try
        End If

        If HeaderMutex IsNot Nothing Then
            Try
                HeaderMutex.Dispose()
                HeaderMutex = Nothing
            Catch ex As Exception
                If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
            End Try
        End If

        If IndexMutexes IsNot Nothing Then
            Try
                For Each item In IndexMutexes
                    Try
                        If item.Value IsNot Nothing Then
                            item.Value.Dispose()
                        End If
                    Catch ex2 As Exception
                        If Settings.IfDebugMode Then Console.WriteLine(ex2.ToString)
                    End Try
                Next

                IndexMutexes = New Concurrent.ConcurrentDictionary(Of Integer, MutexACL)
            Catch ex As Exception
                If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
            End Try
        End If
    End Sub

#End Region

#Region "FileReated: CreatePageFile, FileLength"

    Public Sub CreatePageFile()
        FileMutex.WaitOne()

        If File.Exists(FilePath) Then
            FileMutex.Release()
            Return
        End If

        Try
            Dim PageFolderPath As String = New FileInfo(FilePath).DirectoryName
            If Directory.Exists(PageFolderPath) = False Then
                Directory.CreateDirectory(PageFolderPath)
            End If

            Dim FStream As New FileStream(FilePath, FileMode.CreateNew)
            FStream.SetLength(DatabaseConfig.DatabasePageFileInitSize)  'use to fast create file
            FStream.Flush()
            FStream.Close()
            FStream.Dispose()

            If DatabaseConfig.SupportPageHeaderBuffer Then
                WriteLengthToFile(DatabaseConfig.DataPageHeaderSize)
                WriteToHeaderMemory(ReadFromHeaderFile)
            Else
                WriteLengthToMemory(DatabaseConfig.DataPageHeaderSize)
                WriteLengthToFile(DatabaseConfig.DataPageHeaderSize)
            End If

        Catch ex As Exception
            If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
        End Try

        FileMutex.Release()
    End Sub


    Public Sub CreatePendingRemoveBlocksFile()
        FileRBMutex.WaitOne()

        Try
            If File.Exists(PendingRemoveBlocksFilePath) Then
                FileRBMutex.Release()
                Return
            End If

            Dim PageFolderPath As String = New FileInfo(PendingRemoveBlocksFilePath).DirectoryName
            If Directory.Exists(PageFolderPath) = False Then
                Directory.CreateDirectory(PageFolderPath)
            End If

            Dim FStream As New FileStream(PendingRemoveBlocksFilePath, FileMode.CreateNew)
            FStream.Flush()
            FStream.Close()
            FStream.Dispose()

        Catch ex As Exception
            If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
        End Try

        FileRBMutex.Release()
    End Sub


    ''' <summary>
    ''' Page File Size. If -1, means file not exists
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property FileLength As Int64
        Get
            If File.Exists(FilePath) Then
                theCachedFileLength = New System.IO.FileInfo(FilePath).Length
                CachedFileLengthLastUpdateTime = Now
                Return theCachedFileLength
            Else
                Return -1
            End If
        End Get
    End Property

    Private theCachedFileLength As Int64 = -1
    Private CachedFileLengthLastUpdateTime As DateTime
    Public ReadOnly Property CachedFileLength As Int64
        Get
            If theCachedFileLength >= 0 AndAlso Now.Subtract(CachedFileLengthLastUpdateTime).TotalMilliseconds < 500 Then Return theCachedFileLength
            Return FileLength
        End Get
    End Property

#End Region

#Region "FileStreams"

    Private FileStreams As New HashSet(Of FileStream)
    Private FileStreamsLock As New Object
    Private FileStreamsStack As New Concurrent.ConcurrentStack(Of FileStream)

    Private Function GetOneFileStream() As FileStream
        Dim FStream As FileStream
        Do While FileStreamsStack.Count > 0
            FileStreamsStack.TryPop(FStream)
            If FStream Is Nothing Then
                If FileStreams.Contains(FStream) Then FileStreams.Remove(FStream)
            Else
                Return FStream
            End If
        Loop

        SyncLock FileStreamsLock
            Dim NewFileStream As FileStream = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)
            FileStreams.Add(NewFileStream)

            'Console.WriteLine("FileStreams.Count: " & FileStreams.Count)

            Return NewFileStream
        End SyncLock
    End Function

    Private Sub ReturnOneFileStream(ByRef FStream As FileStream)
        If FStream Is Nothing Then Return
        SyncLock FileStreamsLock
            FileStreamsStack.Push(FStream)
        End SyncLock
    End Sub

    Private Sub DestoryOneFileStream(ByRef FStream As FileStream)
        If FStream Is Nothing Then Return

        SyncLock FileStreamsLock
            If FileStreams.Contains(FStream) Then
                FileStreams.Remove(FStream)
            End If
        End SyncLock

        FStream.Flush()
        FStream.Close()
        FStream.Dispose()

        FStream = Nothing
    End Sub

    Private Sub FlushAllFileStreams()
        For I = 0 To FileStreams.Count - 1 Step 1
            If I >= FileStreams.Count Then Exit For

            Try
                Dim FStream As FileStream = FileStreams.ElementAt(I)
                FStream.Flush()
            Catch ex As Exception
                If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
            End Try
        Next
    End Sub

    Private Sub DestoryAllFileStreams()
        Do While FileStreams.Count > 0
            Try
                Dim FStream As FileStream = FileStreams.First
                FileStreams.Remove(FStream)

                If FStream IsNot Nothing Then
                    FStream.Flush()
                    FStream.Close()
                    FStream.Dispose()

                    FStream = Nothing
                End If
            Catch ex As Exception
                If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
            End Try
        Loop

        FileStreamsStack.Clear()
    End Sub

#End Region

#Region "Length: Real Data Length"
    'Real Data Length of this Page file, Store in Memory

    ''' <summary>
    ''' Will Return the StartPOS. If -1, means do not write data block
    ''' </summary>
    ''' <param name="AppendLength"></param>
    ''' <returns></returns>
    Public Function AddLength(ByVal AppendLength As Int64) As Int64
        'Init Parameters
        Dim StartPOS As Int64 = -1

        'Check Input
        If AppendLength <= 0 Then Return StartPOS

        'Wait Sign
        HeaderMutex.WaitOne()

        'Execute
        Try
            Dim Length As Int64 = ReadLengthFromMemory()
            'Console.WriteLine("Length from Memory: " & Length)
            StartPOS = Length
            Length = Length + AppendLength

            WriteLengthToMemory(Length)
            'Console.WriteLine("Write Length to Memory: " & Length)

            'Flush to File 
            If UpdateHeaderToFileTimer Is Nothing Then
                If DatabaseConfig.SupportPageHeaderBuffer Then
                    Dim FTimerCallback As TimerCallback = AddressOf FlushHeaderToFile
                    UpdateHeaderToFileTimer = New Timer(FTimerCallback, Nothing, DatabaseConfig.PageHeaderBufferFlushMSeconds, -1)
                Else
                    Dim FTimerCallback As TimerCallback = AddressOf UpdateLengthToFile
                    UpdateHeaderToFileTimer = New Timer(FTimerCallback, Nothing, DatabaseConfig.PageLengthFlushMSeconds, -1)
                End If
            End If

        Catch ex As Exception
            If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
        End Try

        'Release Sign
        HeaderMutex.Release()

        'Return Value
        Return StartPOS
    End Function

    Private Sub UpdateLengthToFile()
        'Clear UpdateHeaderToFileTimer
        If UpdateHeaderToFileTimer IsNot Nothing Then
            Try
                UpdateHeaderToFileTimer.Dispose()
            Catch ex As Exception
            Finally
                UpdateHeaderToFileTimer = Nothing
            End Try
        End If

        'Execute
        WriteLengthToFile(ReadLengthFromMemory())
    End Sub


#Region "Length Basic Functions"
    Private Function ReadLengthFromMemory() As Int64
        Dim FBytes As Byte() = PageHeaderMemory.Read(0, 8)
        Return BitConverter.ToInt64(FBytes, 0)
    End Function

    Private Sub WriteLengthToMemory(ByVal Length As Int64)
        Dim FBytes As Byte() = BitConverter.GetBytes(Length)
        PageHeaderMemory.Write(FBytes, 0, 0, FBytes.Length)
    End Sub

    Private Function ReadLengthFromFile() As Int64
        'Generate FStream
        Dim FStream As FileStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)

        'Get FileLength
        Dim FileLength As Int64 = ReadLengthFromFile(FStream)

        'Close FStream
        FStream.Close()
        FStream.Dispose()

        'Return value
        Return FileLength
    End Function

    Private Function ReadLengthFromFile(ByVal FStream As FileStream) As Int64
        'Seek
        FStream.Position = 0

        'Read
        Dim FBytes(7) As Byte
        FStream.Read(FBytes, 0, 8)

        'Get Length
        Dim Length As Int64 = BitConverter.ToInt64(FBytes, 0)

        'Return value
        Return Length
    End Function

    Private Sub WriteLengthToFile(ByVal Length As Int64)
        'Generate FStream
        Dim FStream As FileStream = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)

        'Write
        WriteLengthToFile(FStream, Length)

        'Close FStream
        FStream.Flush()
        FStream.Close()
        FStream.Dispose()
    End Sub

    Private Sub WriteLengthToFile(ByVal FStream As FileStream, ByVal Length As Int64)
        'Seek
        FStream.Position = 0

        'Write
        Dim FBytes As Byte() = BitConverter.GetBytes(Length)
        FStream.Write(FBytes, 0, FBytes.Count)

        If Settings.IfDebugMode Then
            Console.WriteLine("Write Length to File: " & Length)
        End If
    End Sub

#End Region

#End Region

#Region "Page Header"

    Private UpdateHeaderToFileTimer As Timer = Nothing

    Private Sub FlushHeaderToFile()
        'Clear UpdateHeaderToFileTimer
        If UpdateHeaderToFileTimer IsNot Nothing Then
            Try
                UpdateHeaderToFileTimer.Dispose()
            Catch ex As Exception
            Finally
                UpdateHeaderToFileTimer = Nothing
            End Try
        End If

        'Execute
        WriteToHeaderFile(ReadFromHeaderMemory())
    End Sub

#Region "Page Header Memory"

    ''' <summary>
    ''' First 8 Bytes: Length
    ''' </summary>
    Public PageHeaderMemory As SharedMemory

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="Bytes"></param>
    ''' <param name="Position"></param>
    Private Sub WriteToHeader(ByVal Bytes() As Byte, Optional ByVal Position As Int64 = 0)
        'Check input
        'Do not write the Meta Area
        If Position < DatabaseConfig.DataPageHeaderMetaSize Then Return

        'Write to Memory
        PageHeaderMemory.Write(Bytes, Position)

        'Flush to File 
        If UpdateHeaderToFileTimer Is Nothing Then
            Dim FTimerCallback As TimerCallback = AddressOf FlushHeaderToFile
            UpdateHeaderToFileTimer = New Timer(FTimerCallback, Nothing, DatabaseConfig.PageHeaderBufferFlushMSeconds, -1)
        End If
    End Sub

    Private Function ReadFromHeader(Optional ByVal Position As Int64 = 0, Optional ByVal Length As Int64 = 0) As Byte()
        'Check input
        If Position < 0 OrElse Position >= PageHeaderMemory.Size OrElse Length < 0 Then Return Nothing
        Dim MaxRemainLength As Int64 = PageHeaderMemory.Size - Position
        If Length = 0 Then
            Length = MaxRemainLength
        ElseIf Length > MaxRemainLength Then
            Length = MaxRemainLength
        End If

        'Read Bytes
        Dim Bytes As Byte()
        Bytes = PageHeaderMemory.Read(Position, Length)

        'Return Value
        Return Bytes
    End Function

    Private Sub CreatePageHeaderMemory()
        'Create Memory
        Dim MemorySize As Int64
        If DatabaseConfig.SupportPageHeaderBuffer Then
            MemorySize = DatabaseConfig.DataPageHeaderSize
        Else
            MemorySize = 8
        End If

        Dim IfNewCreate As Boolean = False
        PageHeaderMemory = New SharedMemory("FDB-" & DatabaseConfig.DatabaseKey & "-" & TableName & "-P" & ID, MemorySize, IfNewCreate)

        'Init Header or Length
        If IfNewCreate Then
            LoadPageHeaderMemoryFromFile()
        Else
            Dim Length As Int64 = ReadLengthFromMemory()
            If Length <= 0 Then
                LoadPageHeaderMemoryFromFile()
            End If
        End If
    End Sub

    Private Sub LoadPageHeaderMemoryFromFile()
        'Wait Signal
        HeaderMutex.WaitOne()

        Try
            'Check if already read by other process
            Dim Length As Int64 = ReadLengthFromMemory()
            If Length > 0 Then
                'Release Signal
                HeaderMutex.Release()

                Return
            End If

            'Console.WriteLine("LoadPageHeaderMemoryFromFile")

            'Execute Load
            If File.Exists(FilePath) Then
                If DatabaseConfig.SupportPageHeaderBuffer Then
                    Dim HeaderBytes As Byte() = ReadFromHeaderFile()
                    WriteToHeaderMemory(HeaderBytes)
                Else
                    Length = ReadLengthFromFile()
                    WriteLengthToMemory(Length)
                End If
            End If

        Catch ex As Exception
            If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
        End Try

        'Release Signal
        HeaderMutex.Release()
    End Sub

    Public Sub ClearPageHeaderMemory()
        If PageHeaderMemory Is Nothing OrElse PageHeaderMemory.Size = 0 Then Return
        Dim FBytes(PageHeaderMemory.Size - 1) As Byte
        PageHeaderMemory.Write(FBytes)
    End Sub

#End Region

#Region "PageHeader Basic Functions"
    Private Function ReadFromHeaderMemory(Optional ByVal Position As Int64 = 0, Optional ByVal Length As Int64 = 0) As Byte()
        'Check input
        If Position < 0 OrElse Position >= PageHeaderMemory.Size OrElse Length < 0 Then Return Nothing
        Dim MaxRemainLength As Int64 = PageHeaderMemory.Size - Position
        If Length = 0 Then
            Length = MaxRemainLength
        ElseIf Length > MaxRemainLength Then
            Length = MaxRemainLength
        End If

        'Read Bytes
        Dim Bytes As Byte() = PageHeaderMemory.Read(Position, Length)

        'Return Value
        Return Bytes
    End Function

    Private Sub WriteToHeaderMemory(ByVal Bytes() As Byte, Optional ByVal Position As Int64 = 0)
        PageHeaderMemory.Write(Bytes, Position)
    End Sub

    Private Function ReadFromHeaderFile(Optional ByVal Position As Int64 = 0, Optional ByVal Length As Int64 = 0) As Byte()
        'Generate FStream
        Dim FStream As FileStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)

        'Read HeaderBytes
        Dim HeadBytes() As Byte = ReadFromHeaderFile(FStream, Position, Length)

        'Close FStream
        FStream.Close()
        FStream.Dispose()

        'Return value
        Return HeadBytes
    End Function

    Private Function ReadFromHeaderFile(ByVal FStream As FileStream, Optional ByVal Position As Int64 = 0, Optional ByVal Length As Int64 = 0) As Byte()
        'Check input
        If Position < 0 OrElse Position >= PageHeaderMemory.Size OrElse Length < 0 Then Return Nothing
        Dim MaxRemainLength As Int64 = PageHeaderMemory.Size - Position
        If Length = 0 Then
            Length = MaxRemainLength
        ElseIf Length > MaxRemainLength Then
            Length = MaxRemainLength
        End If

        'Seek
        FStream.Position = Position

        'Read
        Dim Bytes(Length - 1) As Byte
        FStream.Read(Bytes, 0, Length)

        'Return value
        Return Bytes
    End Function

    Private Sub WriteToHeaderFile(ByVal Bytes As Byte(), Optional ByVal Position As Int64 = 0)
        'Generate FStream
        Dim FStream As FileStream = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)

        'Write
        WriteToHeaderFile(FStream, Bytes, Position)

        'Close FStream
        FStream.Flush()
        FStream.Close()
        FStream.Dispose()
    End Sub

    Private Sub WriteToHeaderFile(ByVal FStream As FileStream, ByVal Bytes As Byte(), Optional ByVal Position As Int64 = 0)
        'Seek
        FStream.Position = Position

        'Write
        FStream.Write(Bytes, 0, Bytes.Count)

        If Settings.IfDebugMode Then
            Console.WriteLine("Write Header to File.")
        End If
    End Sub

#End Region

#End Region

#Region "Write Data"

    Public Sub WriteData(ByRef FData As Data)
        'Check Input
        If FData Is Nothing OrElse FData.ID < 0 Then Return

        'Check If File Exists
        If IfFileExists = False Then
            If File.Exists(FilePath) Then
                IfFileExists = True
            Else
                IfFileExists = False
                CreatePageFile()
                If File.Exists(FilePath) Then
                    IfFileExists = True
                Else
                    Throw New Exception("Page file not exists at " & FilePath)
                    Return
                End If
            End If
        End If

        'Format Data
        FData.FormatBeforeAddToPage()

        'Get File Stream
        Dim FStream As FileStream
        Dim IfUseStreamPool As Boolean = True

        If IfUseStreamPool Then
            FStream = GetOneFileStream()
        Else
            FStream = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)
        End If

        'Write Data Index
        WriteDataIndex(FStream, FData)

        'Write Data Block
        'Write based on SupportWriteBuffer settings
        If DatabaseConfig.SupportWriteBuffer = False Then
            'Support nothing write for pre-allocate byte space
            If FData.StartPOS > 0 AndAlso FData.Value IsNot Nothing AndAlso FData.Value.Count > 0 Then
                FStream.Position = FData.StartPOS
                FStream.Write(FData.Value, 0, FData.Value.Count)
            End If
        Else
            If FData.StartPOS > 0 AndAlso FData.Value IsNot Nothing AndAlso FData.Value.Count > 0 Then
                PageFileBufferWriter.Write(FStream, FData.StartPOS, FData.Value)
            End If
        End If

        'Return or Close Stream
        If IfUseStreamPool Then
            ReturnOneFileStream(FStream)
        Else
            FStream.Flush()
            FStream.Close()
            FStream.Dispose()
        End If

    End Sub

    Private Sub WriteDataIndex(ByRef FStream As FileStream, ByRef FData As Data)

        'Wait Signal
        Dim IndexMutex As MutexACL = GetOneIndexMutex(FData.ID)
        IndexMutex.WaitOne()

        'Execute
        Try
            'Get Current Data Index
            Dim CurrentData As New Data
            If DatabaseConfig.SupportPageHeaderBuffer Then
                CurrentData.PageIndexBytes = ReadFromHeader(GetDataIndexPOS(FData.ID), DatabaseConfig.DataPageHeaderDataIndexSize)
            Else
                CurrentData.PageIndexBytes = ReadDataIndex(FStream, FData.ID)
            End If

            'Check if re-use the current section
            Dim IfReUseBlock As Boolean = False
            If CurrentData.ID = 0 AndAlso CurrentData.Length = 0 AndAlso CurrentData.StartPOS = 0 Then
                'not use now => not reuse
                IfReUseBlock = False
            Else
                If CurrentData.BlockLength >= FData.BlockLength Then
                    'Space enough, can reuse
                    IfReUseBlock = True
                    FData.StartPOS = CurrentData.StartPOS
                    FData.BlockLength = CurrentData.BlockLength
                Else
                    'Not enough space, cannot reuse
                    IfReUseBlock = False
                End If
            End If

            'If not reuse block
            '1. Get StartPOS and Update Length. Data block will append data at the end of page file
            '2. Make sure File Length is enough
            '3. Add to PendingRemoveBlocks if has old data block
            If IfReUseBlock = False Then
                '1. Get StartPOS and Update Length
                FData.StartPOS = AddLength(FData.Length)

                '2. Make sure file length enough
                If FData.Length > 0 Then
                    Dim EndLength As Int64 = FData.StartPOS + FData.Length
                    If CachedFileLength < EndLength Then
                        If FileLength < EndLength Then
                            FileMutex.WaitOne()

                            If FileLength < EndLength Then
                                Dim NewFileLength As Int64 = FileLength + DatabaseConfig.DatabasePageFileInitSize

                                Do While NewFileLength < EndLength
                                    NewFileLength = NewFileLength + DatabaseConfig.DatabasePageFileInitSize
                                Loop

                                FStream.SetLength(NewFileLength)  'use to fast expend file
                            End If

                            FileMutex.Release()
                        End If
                    End If
                End If

                '3. Add to PendingRemoveBlocks
                If CurrentData.StartPOS > 0 Then
                    FileRBMutex.WaitOne()

                    Try
                        Dim FStream2 As FileStream
                        FStream2 = File.Open(PendingRemoveBlocksFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)
                        FStream2.Position = FStream2.Length

                        Dim FBytes() As Byte = CurrentData.PageRemoveBlockBytes
                        FStream2.Write(FBytes, 0, FBytes.Count)
                        FStream2.Flush()
                        FStream2.Close()
                        FStream2.Dispose()
                    Catch ex As Exception
                        If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
                    End Try

                    FileRBMutex.Release()
                End If
            End If

            'Write Data Index
            If DatabaseConfig.SupportPageHeaderBuffer Then
                WriteToHeader(FData.PageIndexBytes, GetDataIndexPOS(FData.ID))
            Else
                'Write based on SupportWriteBuffer settings
                If DatabaseConfig.SupportWriteBuffer = False Then
                    'Support Multiple Thread Write
                    FStream.Position = GetDataIndexPOS(FData.ID)
                    Dim PageIndexBytes As Byte() = FData.PageIndexBytes
                    FStream.Write(PageIndexBytes, 0, PageIndexBytes.Count)
                Else
                    PageFileBufferWriter.Write(FStream, GetDataIndexPOS(FData.ID), FData.PageIndexBytesFull(DatabaseConfig.DataPageHeaderDataIndexSize))
                End If
            End If

        Catch ex As Exception
            'Release Signal
            IndexMutex.Release()

            'Throw Ex
            Throw ex
        End Try

        'Release Signal
        IndexMutex.Release()

    End Sub

#End Region

#Region "Read Data"

    Public Function ReadData(ByVal DataID As Int64) As Data
        'Check Input
        If DataID <= 0 Then Return Nothing

        'Check If File Exists
        If IfFileExists = False Then
            If File.Exists(FilePath) Then
                IfFileExists = True
            Else
                Return Nothing
            End If
        End If

        'Init Parameters
        Dim FData As New Data

        'Get File Stream
        Dim FStream As FileStream
        Dim IfUseStreamPool As Boolean = True

        If IfUseStreamPool Then
            FStream = GetOneFileStream()
        Else
            FStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        End If

        'Read Data Index
        Dim DataIndex As Byte()
        If DatabaseConfig.SupportPageHeaderBuffer Then
            DataIndex = ReadFromHeaderMemory(GetDataIndexPOS(DataID), DatabaseConfig.DataPageHeaderDataIndexSize)
        Else
            DataIndex = ReadDataIndex(FStream, DataID)
        End If

        FData.PageIndexBytes = DataIndex

        If FData.ID = 0 AndAlso FData.Length = 0 AndAlso FData.StartPOS = 0 Then
            'No Data Index
            FData = Nothing
        Else
            'Has Data Index
            If FData.ID = DataID Then
                'Read Data
                If FData.StartPOS > 0 AndAlso FData.Length > 0 Then
                    'Make sure file length enough
                    Dim EndLength As Int64 = FData.StartPOS + FData.Length

                    If CachedFileLength < EndLength Then
                        'not enough length, not read bytes
                        If Settings.IfDebugMode Then Console.WriteLine("not enough length, not read bytes")
                    Else
                        'Execute Read
                        ReDim FData.Value(FData.Length - 1)

                        FStream.Position = FData.StartPOS
                        FStream.Read(FData.Value, 0, FData.Length)
                    End If
                End If
            Else
                'Error Data Index
                FData = Nothing
            End If

        End If

        'Close or Return FileStream
        If IfUseStreamPool Then
            ReturnOneFileStream(FStream)
        Else
            FStream.Close()
            FStream.Dispose()
            FStream = Nothing
        End If

        'Return Value
        Return FData
    End Function

#End Region

#Region "Data Index Operate"

    Public Function ReadDataIndex(ByRef FStream As FileStream, ByVal DataID As Int64) As Byte()
        Dim Bytes(DatabaseConfig.DataPageHeaderDataIndexSize - 1) As Byte

        FStream.Position = GetDataIndexPOS(DataID)
        FStream.Read(Bytes, 0, Bytes.Length)

        Return Bytes
    End Function

    Public Sub WriteDataIndex(ByRef FStream As FileStream, ByVal DataID As Int64, ByRef Bytes As Byte())
        FStream.Position = GetDataIndexPOS(DataID)
        FStream.Write(Bytes, 0, Bytes.Length)
    End Sub

    Public Function GetDataIndexPOS(ByVal DataID As Int64) As Int64
        Dim IndexID As Int64 = DataID Mod DatabaseConfig.DataPageSize
        Dim POS As Int64 = DatabaseConfig.DataPageHeaderMetaSize + DatabaseConfig.DataPageHeaderDataIndexSize * IndexID
        Return POS
    End Function

#End Region


#Region "Common Functions"

    Public Function GetPageFilePath(ByVal PageID As Int64) As String
        Return DatabaseConfig.DatabaseFolderPath & TableName & "\" & Int(PageID / DatabaseConfig.DataPageFolderSize) & "dpf\" & PageID & "dp.fdb"
    End Function

    Public Function GetPageRBFilePath(ByVal PageID As Int64) As String
        Return DatabaseConfig.DatabaseFolderPath.TrimEnd("\") & "\" & TableName & "\" & Int(PageID / DatabaseConfig.DataPageFolderSize) & "dpf\" & PageID & "dprb.fdb"
    End Function

#End Region


#Region "Dispose"

    Private FlushLock As New Object

    Public Sub Flush()
        SyncLock FlushLock
            ExecuteFlush()
        End SyncLock
    End Sub

    Public Sub ExecuteFlush()
        'Update Length To File
        If UpdateHeaderToFileTimer IsNot Nothing Then
            Try
                UpdateHeaderToFileTimer.Dispose()
                UpdateHeaderToFileTimer = Nothing
            Catch ex As Exception
                If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
            End Try
            Try
                If DatabaseConfig.SupportPageHeaderBuffer Then
                    FlushHeaderToFile()
                Else
                    UpdateLengthToFile()
                End If
            Catch ex As Exception
                If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
            End Try
        End If

        'Flush PageFileBufferWriter
        If PageFileBufferWriter IsNot Nothing Then
            Try
                PageFileBufferWriter.Flush()
            Catch ex As Exception
                If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
            End Try
        End If

        'Flush FileStreams
        FlushAllFileStreams()

    End Sub

    Private disposedValue As Boolean

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: 释放托管状态(托管对象)
            End If

            ' TODO: 释放未托管的资源(未托管的对象)并重写终结器
            ' TODO: 将大型字段设置为 null

            'Execute Flush
            ExecuteFlush()

            'Clear PageFileBufferWriter
            If PageFileBufferWriter IsNot Nothing Then
                Try
                    PageFileBufferWriter.Dispose()
                    PageFileBufferWriter = Nothing
                Catch ex As Exception
                    If Settings.IfDebugMode Then Console.WriteLine(ex.ToString)
                End Try
            End If

            'Destory FileStream
            DestoryAllFileStreams()

            'Dispose Mutexes
            DisposeAllMutexes()

            'Set disposedValue
            disposedValue = True
        End If
    End Sub


    '' TODO: 仅当“Dispose(disposing As Boolean)”拥有用于释放未托管资源的代码时才替代终结器
    'Protected Overrides Sub Finalize()
    '    ' 不要更改此代码。请将清理代码放入“Dispose(disposing As Boolean)”方法中
    '    Dispose(disposing:=False)
    '    MyBase.Finalize()
    'End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' 不要更改此代码。请将清理代码放入“Dispose(disposing As Boolean)”方法中
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub


#End Region



End Class

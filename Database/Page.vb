Imports System.IO
Imports System.Threading

Public Class Page
    Implements IDisposable

    Public ID As Int64
    Public FilePath As String   'Page File Path
    Public IfFileExists As Boolean

    Public PendingRemoveBlocksFilePath As String    'Page File's Pending Remove Data Blocks. StartPOS(8 Bytes)+BlockLength(6 Bytes)

    Public PageFileBufferWriter As FileBufferWriter

    Public Sub New(ByVal ID As Int64)
        Me.ID = ID

        'Cal FilePath
        FilePath = GetPageFilePath(ID)
        PendingRemoveBlocksFilePath = GetPageRBFilePath(ID)

        'Create LengthMutex
        LengthMutex = New MutexACL("FDBP" & ID & "Length")

        'Create Page SharedMemory
        CreateMemory()

        'Check if need to Create PageFile
        If File.Exists(FilePath) Then
            IfFileExists = True
        Else
            IfFileExists = False
            CreatePageFile()
            If File.Exists(FilePath) Then IfFileExists = True
        End If

        'Check if need to Create PendingRemoveBlocksFile
        RemoveBlocksFileMutex = New MutexACL("FDBPRB" & ID & "Operate")
        If File.Exists(PendingRemoveBlocksFilePath) = False Then
            CreatePendingRemoveBlocksFile()
        End If

        'Create Buffer Writer if SupportWriteBuffer
        If Config.SupportWriteBuffer Then
            PageFileBufferWriter = New FileBufferWriter(FilePath, Config.DataPageWriteBufferSize, Config.WriteBufferFlushMSeconds)
        End If
    End Sub


#Region "FileReated: CreatePageFile, FileLength"

    Public Sub CreatePageFile()
        Dim FMutexACL As New MutexACL("FDBP" & ID & "Operate")
        FMutexACL.WaitOne()

        If File.Exists(FilePath) Then Return

        Dim PageFolderPath As String = New FileInfo(FilePath).DirectoryName
        If Directory.Exists(PageFolderPath) = False Then
            Directory.CreateDirectory(PageFolderPath)
        End If

        Dim FStream As New FileStream(FilePath, FileMode.CreateNew)
        FStream.SetLength(Config.DatabasePageFileInitSize)  'use to fast create file
        FStream.Flush()
        FStream.Close()
        FStream.Dispose()

        WriteLengthToMemory(Config.DataPageHeaderSize)
        WriteLengthToFile(Config.DataPageHeaderSize)

        FMutexACL.Release()
    End Sub


    Private RemoveBlocksFileMutex As MutexACL

    Public Sub CreatePendingRemoveBlocksFile()
        RemoveBlocksFileMutex.WaitOne()

        If File.Exists(PendingRemoveBlocksFilePath) Then Return

        Dim PageFolderPath As String = New FileInfo(PendingRemoveBlocksFilePath).DirectoryName
        If Directory.Exists(PageFolderPath) = False Then
            Directory.CreateDirectory(PageFolderPath)
        End If

        Dim FStream As New FileStream(PendingRemoveBlocksFilePath, FileMode.CreateNew)
        FStream.Flush()
        FStream.Close()
        FStream.Dispose()

        RemoveBlocksFileMutex.Release()
    End Sub


    ''' <summary>
    ''' Page File Size. If -1, means file not exists
    ''' </summary>
    ''' <returns></returns>
    Public ReadOnly Property FileLength As Int64
        Get
            If File.Exists(FilePath) Then
                Return New System.IO.FileInfo(FilePath).Length
            Else
                Return -1
            End If
        End Get
    End Property

#End Region

#Region "Length: Real Data Length"
    ' Real Data Length of this Page file, Store in Memory

    Public LengthMutex As MutexACL

    Public Function ReadLengthFromMemory() As Int64
        Dim FBytes As Byte() = Memory.Read(0, 8)
        Return BitConverter.ToInt64(FBytes)
    End Function

    Public Sub WriteLengthToMemory(ByVal Length As Int64)
        Dim FBytes As Byte() = BitConverter.GetBytes(Length)
        Memory.Write(FBytes, 0, 0, FBytes.Length)
    End Sub

    Public Function ReadLengthFromFile() As Int64
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

    Public Function ReadLengthFromFile(ByVal FStream As FileStream) As Int64
        'Seek
        FStream.Position = 0

        'Read
        Dim FBytes(7) As Byte
        FStream.Read(FBytes, 0, 8)

        'Get Length
        Dim Length As Int64 = BitConverter.ToInt64(FBytes)

        'Return value
        Return Length
    End Function

    Public Sub WriteLengthToFile(ByVal Length As Int64)
        'Generate FStream
        Dim FStream As FileStream = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)

        'Write
        WriteLengthToFile(FStream, Length)

        'Close FStream
        FStream.Flush()
        FStream.Close()
        FStream.Dispose()
    End Sub

    Public Sub WriteLengthToFile(ByVal FStream As FileStream, ByVal Length As Int64)
        'Seek
        FStream.Position = 0

        'Write
        Dim FBytes As Byte() = BitConverter.GetBytes(Length)
        FStream.Write(FBytes)

        Console.WriteLine("Write Length to File: " & Length)
    End Sub


#Region "Add Length"

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
        LengthMutex.WaitOne()

        'Execute
        Dim Length As Int64 = ReadLengthFromMemory()
        'Console.WriteLine("Length from Memory: " & Length)
        StartPOS = Length
        Length = Length + AppendLength

        WriteLengthToMemory(Length)
        'Console.WriteLine("Write Length to Memory: " & Length)

        'Flush to File each 3 seconds
        If UpdateLengthToFileTimer Is Nothing Then
            Dim FTimerCallback As TimerCallback = AddressOf UpdateLengthToFile
            UpdateLengthToFileTimer = New Timer(FTimerCallback, Nothing, 1000, -1)
        End If

        'Release Sign
        LengthMutex.Release()

        'Return Value
        Return StartPOS
    End Function

    Private UpdateLengthToFileTimer As Timer = Nothing

    Private Sub UpdateLengthToFile()
        'Clear UpdateLengthToFileTimer
        If UpdateLengthToFileTimer IsNot Nothing Then
            Try
                UpdateLengthToFileTimer.Dispose()
            Catch ex As Exception
            Finally
                UpdateLengthToFileTimer = Nothing
            End Try
        End If

        'Execute
        WriteLengthToFile(ReadLengthFromMemory())
    End Sub

#End Region

#End Region

#Region "Page Shared Memory"

    ''' <summary>
    ''' First 8 Bytes: Length
    ''' </summary>
    Private Memory As SharedMemory
    Private disposedValue As Boolean

    Private Sub CreateMemory()
        'Create Memory
        Dim MemorySize As Int64 = 8
        Dim IfNewCreate As Boolean = False
        Memory = New SharedMemory("FDBP" & ID, MemorySize, IfNewCreate)

        'Init Length
        If IfNewCreate Then
            LengthMutex.WaitOne()
            If File.Exists(FilePath) Then
                Dim Length As Int64 = ReadLengthFromFile()
                WriteLengthToMemory(Length)
            End If
            LengthMutex.Release()
        End If
    End Sub

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
        FStream = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)

        'Check if re-use the current section
        Dim IfReUseBlock As Boolean = False
        Dim CurrentDataIndex As Byte() = ReadDataIndex(FStream, FData.ID)
        Dim CurrentData As New Data
        CurrentData.PageIndexBytes = CurrentDataIndex

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
        '1. Update Length, data block will append data at the end of page file
        '2. Make sure File Length is enough
        '3. Add to PendingRemoveBlocks if has old data block
        If IfReUseBlock = False Then
            'Update Length
            FData.StartPOS = AddLength(FData.Length)

            'Make sure file length enough
            If FData.Length > 0 Then
                Dim EndLength As Int64 = FData.StartPOS + FData.Length
                If FileLength < EndLength Then
                    FStream.SetLength(FileLength + Config.DatabasePageFileInitSize)  'use to fast create file
                End If
            End If

            'Add to PendingRemoveBlocks
            If CurrentData.StartPOS > 0 Then
                RemoveBlocksFileMutex.WaitOne()

                Dim FStream2 As FileStream
                FStream2 = File.Open(PendingRemoveBlocksFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)
                FStream2.Position = FStream2.Length

                FStream2.Write(CurrentData.PageRemoveBlockBytes)
                FStream2.Flush()
                FStream2.Close()
                FStream2.Dispose()

                RemoveBlocksFileMutex.Release()
            End If
        End If

        'Write based on SupportWriteBuffer settings
        If Config.SupportWriteBuffer = False Then
            'Write Data Index
            'Support Multiple Thread Write
            FStream.Position = GetDataIndexPOS(FData.ID)
            Dim PageIndexBytes As Byte() = FData.PageIndexBytes
            FStream.Write(PageIndexBytes, 0, PageIndexBytes.Count)

            'Write Data Block
            'Support nothing write for pre-allocate byte space
            If FData.StartPOS > 0 AndAlso FData.Value IsNot Nothing AndAlso FData.Value.Count > 0 Then
                FStream.Position = FData.StartPOS
                FStream.Write(FData.Value, 0, FData.Value.Count)
            End If
        Else
            'Write Data Index
            PageFileBufferWriter.Write(FStream, GetDataIndexPOS(FData.ID), FData.PageIndexBytesFull)

            'Write Data Block
            If FData.StartPOS > 0 AndAlso FData.Value IsNot Nothing AndAlso FData.Value.Count > 0 Then
                PageFileBufferWriter.Write(FStream, FData.StartPOS, FData.Value)
            End If
        End If

        'Close Stream
        FStream.Flush()
        FStream.Close()
        FStream.Dispose()

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
        FStream = File.Open(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)

        'Read Data Index
        Dim DataIndex As Byte() = ReadDataIndex(FStream, DataID)
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
                    If FileLength < EndLength Then
                        'not enough length, not read bytes
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

        'Close Stream
        FStream.Flush()
        FStream.Close()
        FStream.Dispose()

        'Return Value
        Return FData
    End Function

#End Region

#Region "Data Index Operate"

    Public Function ReadDataIndex(ByRef FStream As FileStream, ByVal DataID As Int64) As Byte()
        Dim Bytes(Config.DataPageHeaderDataIndexSize - 1) As Byte

        FStream.Position = GetDataIndexPOS(DataID)
        FStream.Read(Bytes, 0, Bytes.Length)

        Return Bytes
    End Function

    Public Sub WriteDataIndex(ByRef FStream As FileStream, ByVal DataID As Int64, ByRef Bytes As Byte())
        FStream.Position = GetDataIndexPOS(DataID)
        FStream.Write(Bytes, 0, Bytes.Length)
    End Sub

    Public Shared Function GetDataIndexPOS(ByVal DataID As Int64) As Int64
        Dim IndexID As Int64 = DataID Mod Config.DataPageSize
        Dim POS As Int64 = Config.DataPageHeaderMetaSize + Config.DataPageHeaderDataIndexSize * IndexID
        Return POS
    End Function

#End Region


#Region "Dispose"

    Public Sub Flush()
        'Update Length To File
        If UpdateLengthToFileTimer IsNot Nothing Then
            Try
                UpdateLengthToFileTimer.Dispose()
                UpdateLengthToFileTimer = Nothing
            Catch ex As Exception
            End Try
            Try
                UpdateLengthToFile()
            Catch ex As Exception
            End Try
        End If

        'Flush PageFileBufferWriter
        If PageFileBufferWriter IsNot Nothing Then
            Try
                PageFileBufferWriter.Flush()
            Catch ex As Exception
            End Try
        End If
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: 释放托管状态(托管对象)
            End If

            ' TODO: 释放未托管的资源(未托管的对象)并重写终结器
            ' TODO: 将大型字段设置为 null

            'Flush
            Flush()

            'Clear PageFileBufferWriter
            If PageFileBufferWriter IsNot Nothing Then
                Try
                    PageFileBufferWriter.Dispose()
                    PageFileBufferWriter = Nothing
                Catch ex As Exception
                End Try
            End If

            disposedValue = True
        End If
    End Sub

    ' ' TODO: 仅当“Dispose(disposing As Boolean)”拥有用于释放未托管资源的代码时才替代终结器
    ' Protected Overrides Sub Finalize()
    '     ' 不要更改此代码。请将清理代码放入“Dispose(disposing As Boolean)”方法中
    '     Dispose(disposing:=False)
    '     MyBase.Finalize()
    ' End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' 不要更改此代码。请将清理代码放入“Dispose(disposing As Boolean)”方法中
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub


#End Region


#Region "Shared Functions"

    Public Shared Pages As New Concurrent.ConcurrentDictionary(Of Int64, Page)
    Private Shared CreatePageLock As New Object

    Public Shared Sub Write(ByVal FData As Data)
        'Get Page
        Dim FPage As Page = GetPage(FData.ID)

        'Execute
        FPage.WriteData(FData)
    End Sub

    Public Shared Function Read(ByVal DataID As Int64) As Data
        'Get Page
        Dim FPage As Page = GetPage(DataID)

        'Execute
        Return FPage.ReadData(DataID)
    End Function

    Public Shared Sub FlushAll()
        For Each PageItem In Pages
            If PageItem.Value IsNot Nothing Then
                Try
                    PageItem.Value.Flush()
                Catch ex As Exception
                End Try
            End If
        Next
    End Sub

    Public Shared Function GetPageID(ByVal DataID As Int64) As Int64
        Return Int(DataID / Config.DataPageSize)
    End Function

    Public Shared Function GetPage(ByVal DataID As Int64) As Page
        'Get Page ID
        Dim PageID As Int64 = GetPageID(DataID)

        'Get Page
        If Pages.ContainsKey(PageID) = False Then
            SyncLock CreatePageLock
                Pages.TryAdd(PageID, New Page(PageID))
            End SyncLock
        End If

        'Return Value
        Return Pages(PageID)
    End Function

    Public Shared Function GetPageFilePath(ByVal PageID As Int64) As String
        If String.IsNullOrWhiteSpace(Config.DatabaseFolderPath) Then
            Return Int(PageID / Config.DataPageFolderSize) & "dpf\" & PageID & "dp.fdb" 'DPF means data page folder, DP means data page.
        Else
            Return Config.DatabaseFolderPath.TrimEnd("\") & "\" & Int(PageID / Config.DataPageFolderSize) & "dpf\" & PageID & "dp.fdb"
        End If
    End Function

    Public Shared Function GetPageRBFilePath(ByVal PageID As Int64) As String
        If String.IsNullOrWhiteSpace(Config.DatabaseFolderPath) Then
            Return Int(PageID / Config.DataPageFolderSize) & "dpf\" & PageID & "dprb.fdb" 'DPF means data page folder, DPRB means data page pending remove data blocks.
        Else
            Return Config.DatabaseFolderPath.TrimEnd("\") & "\" & Int(PageID / Config.DataPageFolderSize) & "dpf\" & PageID & "dprb.fdb"
        End If
    End Function

#End Region

End Class

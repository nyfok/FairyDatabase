Imports FairyDatabase

Public Class WritePerformanceTest

    Private Shared DatabaseName As String = "TestDB"
    Private Shared TableName As String = "TestTable"
    Private Shared Database As FairyDatabase.Database

    Private Shared WriteNumber As Int64 = 9999
    Private Shared ByteSize As Int64 = 100
    Private Shared IfVerifyData As Boolean = False

    Private Shared SampleBytes(ByteSize - 1) As Byte
    Private Shared SampleBytesHash As String

    Public Shared Sub Start()
        'Write Log
        Console.WindowWidth = 150
        Console.WriteLine("Write Performance Test: TestNumber=" & WriteNumber & ", ByteSize=" & ByteSize & ", IfVerifyData=" & IfVerifyData)
        Console.WriteLine()

        'Get Database 
        Dim Config As New DatabaseConfig(DatabaseName)
        Database = New FairyDatabase.Database(Config)

        'Set Debug Mode
        FairyDatabase.Settings.IfDebugMode = False

        'Init RandomIDs, IfVerifyData
        Randomize()
        PrepareRandomIDs()

        'Generate SampleBytes
        For I = 0 To ByteSize - 1
            Dim Number As Single = Rnd()
            SampleBytes(I) = Int(Number * 256)
        Next
        SampleBytesHash = GetBytesHash(SampleBytes)

        'Execute Test
        Do While True
            Console.WriteLine("Start to test write performance.")
            Console.WriteLine()

            For Each ThreadNumber In New Integer() {1, 2, 4, 8, 16, 32, 64, 128, 256}
                Console.WriteLine("-------------------------------------------------------------------------------------")
                Console.WriteLine()

                For Each IfRandomWrite In New Boolean() {False, True}
                    If IfRandomWrite Then
                        Console.WriteLine("Start to test RANDOM write performance via " & ThreadNumber & " threads.")
                    Else
                        Console.WriteLine("Start to test SEQUENCY write performance via " & ThreadNumber & " threads.")
                    End If
                    Console.WriteLine()

                    If ThreadNumber = 1 Then
                        If IfRandomWrite = False Then
                            For J = 1 To 5
                                TestWriteFilesInSingleThread()
                            Next
                            Console.WriteLine()
                        End If

                        For J = 1 To 5
                            TestWriteInSingleThread(IfRandomWrite)
                        Next
                        Console.WriteLine()
                    Else
                        If IfRandomWrite = False Then
                            For J = 1 To 5
                                TestWriteFilesInMultipleThreads(ThreadNumber)
                            Next
                            Console.WriteLine()
                        End If

                        For J = 1 To 5
                            TestWriteInMultipleThreads(ThreadNumber, IfRandomWrite)
                        Next
                        Console.WriteLine()
                    End If

                Next

            Next

            Console.WriteLine("======================================================================================")
            Console.WriteLine()
            Console.WriteLine("Press any key to start a new test. (Press ""q"" for exit)")
            Dim Input As String = Console.ReadLine()
            If Input.Trim.ToLower = "q" Then Exit Do
        Loop

    End Sub

#Region "Test Write to Files"

    Private Shared TestWriteFileFolderPath As String = "temp\writeperformancetest\"
    Private Shared Sub TestWriteFilesInSingleThread()
        'Clear and Init Resources
        ClearAndInitResources()

        'Init Parameters
        Dim SubFolderPath As String
        Dim FilePath As String

        'Clear Resources
        If System.IO.Directory.Exists(TestWriteFileFolderPath) Then
            For Each SubFolderPath In System.IO.Directory.GetDirectories(TestWriteFileFolderPath)
                System.IO.Directory.Delete(SubFolderPath, True)
            Next
        Else
            System.IO.Directory.CreateDirectory(TestWriteFileFolderPath)
        End If

        'Init Parameters
        Dim StartTime As DateTime = Now

        'Exectue
        Do While True
            Dim DataID As Int64 = GetNextDataID(False)
            If DataID <= 0 Then Exit Do

            Dim FData As New Data(DataID, SampleBytes)

            SubFolderPath = TestWriteFileFolderPath & Int(DataID / 1000) & "K"
            If System.IO.Directory.Exists(SubFolderPath) = False Then System.IO.Directory.CreateDirectory(SubFolderPath)
            FilePath = SubFolderPath & "\" & DataID & ".dat"

            System.IO.File.WriteAllBytes(FilePath, FData.Value)

            'Console.WriteLine("Write " & DataID & " ok.")
        Loop

        Database.FlushTable(TableName)

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim WriteSpeed As Decimal = WriteNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = WriteNumber / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Dim LogString = "Write FILES via single thread using " & MSeconds & "ms.                                                         "
        Console.WriteLine(LogString.Substring(0, 60) & "(ByteSize=" & SampleBytes.Length & ", Copies=" & WriteNumber & ", WriteCopySpeed=" & WriteCopySpeed & " Copy/s, WriteSpeed=" & WriteSpeed & " MB/s)")

    End Sub


    Private Shared Sub TestWriteFilesInMultipleThreads(ByVal ThreadNumber As Integer)
        'Clear and Init Resources
        ClearAndInitResources()

        'Init Parameters
        Dim SubFolderPath As String

        'Clear Resources
        If System.IO.Directory.Exists(TestWriteFileFolderPath) Then
            For Each SubFolderPath In System.IO.Directory.GetDirectories(TestWriteFileFolderPath)
                System.IO.Directory.Delete(SubFolderPath, True)
            Next
        Else
            System.IO.Directory.CreateDirectory(TestWriteFileFolderPath)
        End If

        'Init Parameters
        ReDim ManualResetEvents(ThreadNumber - 1)

        Dim StartTime As DateTime = Now

        'Execute
        For ThreadID = 1 To ThreadNumber
            ManualResetEvents(ThreadID - 1) = New Threading.ManualResetEvent(False)

            System.Threading.ThreadPool.QueueUserWorkItem(New System.Threading.WaitCallback(AddressOf TestWriteFilesInMultipleThreadsDO), ThreadID)
        Next

        For ThreadID = 1 To ThreadNumber
            ManualResetEvents(ThreadID - 1).WaitOne()
        Next

        Database.FlushTable(TableName)

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim WriteSpeed As Decimal = WriteNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = WriteNumber / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Dim LogString = "Write FILES via " & ThreadNumber & " threads using " & MSeconds & "ms.                                                         "
        Console.WriteLine(LogString.Substring(0, 60) & "(ByteSize=" & SampleBytes.Length & ", Copies=" & WriteNumber & ", WriteCopySpeed=" & WriteCopySpeed & " Copy/s, WriteSpeed=" & WriteSpeed & " MB/s)")

    End Sub

    Private Shared Sub TestWriteFilesInMultipleThreadsDO(ByVal ThreadID As Integer)
        'Init Parameters
        Dim SubFolderPath As String
        Dim FilePath As String

        'Execute
        Do While True
            Dim DataID As Int64 = GetNextDataID(False)
            If DataID <= 0 Then Exit Do

            Dim FData As New Data(DataID, SampleBytes)

            SubFolderPath = TestWriteFileFolderPath & Int(DataID / 1000) & "K"
            If System.IO.Directory.Exists(SubFolderPath) = False Then System.IO.Directory.CreateDirectory(SubFolderPath)
            FilePath = SubFolderPath & "\" & DataID & ".dat"

            System.IO.File.WriteAllBytes(FilePath, FData.Value)

            'Console.WriteLine("(" & ThreadID & ") Write " & DataID & " ok.")
        Loop

        'Set Signal
        ManualResetEvents(ThreadID - 1).Set()
    End Sub


#End Region

#Region "Test Write to DB"

    Private Shared Sub TestWriteInSingleThread(ByVal IfRandomRead As Boolean)
        'Clear and Init Resources
        ClearAndInitResources()

        'Init Parameters
        Dim StartTime As DateTime = Now

        'Exectue
        Do While True
            Dim DataID As Int64 = GetNextDataID(IfRandomRead)
            If DataID <= 0 Then Exit Do

            Dim FData As New Data(DataID, SampleBytes)
            Database.Write(TableName, FData)

            'Console.WriteLine("Write " & DataID & " ok.")
        Loop

        Database.FlushTable(TableName)

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim WriteSpeed As Decimal = WriteNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = WriteNumber / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Dim WriteWayString As String
        If IfRandomRead Then
            WriteWayString = "Random"
        Else
            WriteWayString = "Sequency"
        End If

        Dim LogString = WriteWayString & " write DB via single thread using " & MSeconds & "ms.                                                         "
        Console.WriteLine(LogString.Substring(0, 60) & "(ByteSize=" & SampleBytes.Length & ", Copies=" & WriteNumber & ", WriteCopySpeed=" & WriteCopySpeed & " Copy/s, WriteSpeed=" & WriteSpeed & " MB/s)")

        If FairyDatabase.Settings.IfDebugMode Then
            'Dim FWriter As FileBufferWriter = Page.GetPage(1).PageFileBufferWriter
            'If FWriter IsNot Nothing Then
            '    Console.WriteLine(Now.ToString & ": CreateNewCount_Index=" & FWriter.CreateNewCount_Index & ", CreateNewCount_Block=" & FWriter.CreateNewCount_Block)
            'End If
        End If

        If IfVerifyData Then
            'PrintFileLength()
            CheckDataCorrectRate()
        End If
    End Sub

    Private Shared ManualResetEvents As Threading.ManualResetEvent()
    Private Shared TestWriteInMultipleThreads_IfRandomRead As Boolean

    Private Shared Sub TestWriteInMultipleThreads(ByVal ThreadNumber As Integer, ByVal IfRandomRead As Boolean)
        'Clear and Init Resources
        ClearAndInitResources()

        'Init Parameters
        ReDim ManualResetEvents(ThreadNumber - 1)
        TestWriteInMultipleThreads_IfRandomRead = IfRandomRead

        Dim StartTime As DateTime = Now

        'Execute
        For ThreadID = 1 To ThreadNumber
            ManualResetEvents(ThreadID - 1) = New Threading.ManualResetEvent(False)

            System.Threading.ThreadPool.QueueUserWorkItem(New System.Threading.WaitCallback(AddressOf TestWriteInMultipleThreadsDO), ThreadID)
        Next

        For ThreadID = 1 To ThreadNumber
            ManualResetEvents(ThreadID - 1).WaitOne()
        Next

        Database.FlushTable(TableName)

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim WriteSpeed As Decimal = WriteNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = WriteNumber / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Dim WriteWayString As String
        If IfRandomRead Then
            WriteWayString = "Random"
        Else
            WriteWayString = "Sequency"
        End If

        Dim LogString = WriteWayString & " write DB via " & ThreadNumber & " threads using " & MSeconds & "ms.                                                         "
        Console.WriteLine(LogString.Substring(0, 60) & "(ByteSize=" & SampleBytes.Length & ", Copies=" & WriteNumber & ", WriteCopySpeed=" & WriteCopySpeed & " Copy/s, WriteSpeed=" & WriteSpeed & " MB/s)")

        If IfVerifyData Then
            'PrintFileLength()
            CheckDataCorrectRate()
        End If
    End Sub

    Private Shared Sub TestWriteInMultipleThreadsDO(ByVal ThreadID As Integer)

        'Execute
        Do While True
            Dim DataID As Int64 = GetNextDataID(TestWriteInMultipleThreads_IfRandomRead)
            If DataID <= 0 Then Exit Do

            Dim FData As New Data(DataID, SampleBytes)
            Database.Write(TableName, FData)

            'Console.WriteLine("(" & ThreadID & ") Write " & DataID & " ok.")
        Loop

        'Set Signal
        ManualResetEvents(ThreadID - 1).Set()
    End Sub

#End Region

#Region "RandomIDs, GetNextDataID"

    Private Shared RandomIDs As List(Of Int64)

    Private Shared Sub PrepareRandomIDs()
        Randomize()

        'Console.WriteLine("Preparing Random IDs...")

        Dim RemainIDs As New List(Of Int64)
        For I = 1 To WriteNumber
            RemainIDs.Add(I)
        Next

        RandomIDs = New List(Of Int64)
        Do While RemainIDs.Count > 0
            Dim Index As Integer = Int(Rnd() * RemainIDs.Count)
            Dim ID As Int64 = RemainIDs.ElementAt(Index)
            RandomIDs.Add(ID)
            RemainIDs.Remove(ID)
        Loop
        RemainIDs = Nothing

        'Console.WriteLine("Random IDs inited.")
        'Console.WriteLine()
    End Sub


    Private Shared CurrentDataID As Int64 = 0
    Private Shared GetNextDataIDLock As New Object
    Private Shared NextDataIDs As Concurrent.ConcurrentQueue(Of Int64)

    Private Shared Function GetNextDataID(ByVal IfRandomRead As Boolean) As Int64
        If IfRandomRead = False Then
            Dim DataID As Int64 = System.Threading.Interlocked.Increment(CurrentDataID)
            If DataID > WriteNumber Then Return 0
            Return DataID
        Else
            Dim DataID As Int64
            If NextDataIDs.TryDequeue(DataID) Then
                Return DataID
            Else
                Return 0
            End If
        End If
    End Function

#End Region

#Region "Tools"
    Private Shared Sub PrintFileLength()
        For Each PageItem In Database.GetTable(TableName).Pages
            If PageItem.Value IsNot Nothing Then
                Try
                    Dim FStream As System.IO.FileStream = System.IO.File.Open(PageItem.Value.FilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite)
                    Dim MD5String As String = GetStreamHash(FStream)
                    FStream.Close()
                    FStream.Dispose()

                    Console.WriteLine("Page file " & PageItem.Value.ID & " Length=" & PageItem.Value.FileLength & ", MD5=" & MD5String)
                Catch ex As Exception
                    Console.WriteLine(ex.ToString)
                End Try
            End If
        Next
    End Sub

    Private Shared Sub CheckDataCorrectRate()
        Dim ErrorCount As Integer = 0

        For DataID = 1 To WriteNumber
            Dim FData As Data = Database.Read(TableName, DataID)

            If FData Is Nothing OrElse FData.Value Is Nothing Then
                ErrorCount = ErrorCount + 1
                Continue For
            End If

            Dim BytesHash As String = GetBytesHash(FData.Value)
            If SampleBytesHash <> BytesHash Then
                ErrorCount = ErrorCount + 1
            End If
        Next

        If ErrorCount = 0 Then
            Console.WriteLine("All data correct! (Number=" & WriteNumber & ", CorrectRate=" & (WriteNumber - ErrorCount) * 100 / WriteNumber & "%)")
        Else
            Console.WriteLine(ErrorCount & " datas are error! (Number=" & WriteNumber & ", CorrectRate=" & (WriteNumber - ErrorCount) * 100 / WriteNumber & "%)")
        End If
    End Sub

    Private Shared Sub ClearAndInitResources()
        'Clear Page Files and Memory
        If Database.GetTable(TableName).Pages IsNot Nothing Then
            For Each PageItem In Database.GetTable(TableName).Pages
                If PageItem.Value IsNot Nothing Then
                    Try
                        PageItem.Value.ClearPageHeaderMemory()
                        PageItem.Value.Dispose()

                        If System.IO.File.Exists(PageItem.Value.FilePath) Then System.IO.File.Delete(PageItem.Value.FilePath)
                        If System.IO.File.Exists(PageItem.Value.PendingRemoveBlocksFilePath) Then System.IO.File.Delete(PageItem.Value.PendingRemoveBlocksFilePath)
                    Catch ex As Exception
                        Console.WriteLine(ex.ToString)
                    End Try
                End If
            Next
        End If

        If System.IO.Directory.Exists(Database.DatabaseConfig.DatabaseFolderPath) Then
            For Each FilePath In System.IO.Directory.GetFiles(Database.DatabaseConfig.DatabaseFolderPath, "*.fdb", System.IO.SearchOption.AllDirectories)
                Try
                    System.IO.File.Delete(FilePath)
                Catch ex As Exception
                    Console.WriteLine(ex.ToString)
                End Try
            Next
        End If

        Database.GetTable(TableName).Pages = New Concurrent.ConcurrentDictionary(Of Int64, Page)

        'Init CurrentDataID, NextDataIDs
        CurrentDataID = 0

        NextDataIDs = New Concurrent.ConcurrentQueue(Of Int64)
        For Each ID In RandomIDs
            NextDataIDs.Enqueue(ID)
        Next

        'Console.WriteLine("Related resource clear and inited.")
    End Sub

#End Region


#Region "MD5"

    Private Shared MD5 As Security.Cryptography.MD5 = Security.Cryptography.MD5.Create
    Public Shared Function GetBytesHash(ByRef FBytes() As Byte) As String
        Dim FStream As New System.IO.MemoryStream(FBytes)
        Return GetStreamHash(FStream)
    End Function
    Public Shared Function GetStreamHash(ByRef FStream As System.IO.Stream) As String
        Dim MD5Bytes() As Byte = MD5.ComputeHash(FStream)

        Dim MD5String As New System.Text.StringBuilder
        For Each FByte In MD5Bytes
            MD5String.Append(String.Format("{0:X2}", FByte))
        Next

        Return MD5String.ToString
    End Function

#End Region

End Class

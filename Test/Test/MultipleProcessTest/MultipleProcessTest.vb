Imports FairyDatabase

''' <summary>
''' Please launch two processes manually to test multiple process write and read.
''' </summary>
Public Class MultipleProcessTest

    Private Shared TestNumber As Int64 = 49999
    Private Shared ByteSize As Int64 = 1000
    Private Shared IfVerifyData As Boolean = True

    Private Shared SampleBytes(ByteSize - 1) As Byte
    Private Shared SampleBytesHash As String

    Public Shared Sub Start()
        'Write Log
        Console.WindowWidth = 150
        Console.WriteLine("Write Performance Test: TestNumber=" & TestNumber & ", ByteSize=" & ByteSize & ", IfVerifyData=" & IfVerifyData)
        Console.WriteLine()

        'Init FairyDatabase Config
        FairyDatabase.Config.Init()
        FairyDatabase.Config.IfDebugMode = False

        'Init RandomIDs, IfVerifyData
        Randomize()
        PrepareRandomIDs()

        'Generate SampleBytes
        For I = 0 To ByteSize - 1
            Dim Number As Single = Rnd()
            SampleBytes(I) = Int(I Mod 256)
        Next
        SampleBytesHash = GetBytesHash(SampleBytes)

        'Execute Test
        Do While True
            Dim ThreadNumber As Integer = 128

            If False Then
                Console.WriteLine("Press any key to start check correct rate.")
                Console.ReadLine()
                ClearAndInitResources()
                CheckDataCorrectRate()
                Continue Do
            End If

            For Each IfRandomWrite In New Boolean() {False, True}
                DeleteFiles()

                Console.WriteLine("Press any key to start write.")
                Console.ReadLine()

                If IfRandomWrite Then
                    Console.WriteLine("Start to test RANDOM write performance via " & ThreadNumber & " threads.")
                Else
                    Console.WriteLine("Start to test SEQUENCY write performance via " & ThreadNumber & " threads.")
                End If
                Console.WriteLine()

                TestWriteInMultipleThreads(ThreadNumber, IfRandomWrite)

                Console.WriteLine("Press any key to dispose db.")
                Console.ReadLine()
                DBDispose()
            Next

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

        Page.FlushAll()

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim WriteSpeed As Decimal = TestNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = TestNumber / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Dim LogString = "Write FILES via single thread using " & MSeconds & "ms.                                                         "
        Console.WriteLine(LogString.Substring(0, 60) & "(ByteSize=" & SampleBytes.Length & ", Copies=" & TestNumber & ", WriteCopySpeed=" & WriteCopySpeed & " Copy/s, WriteSpeed=" & WriteSpeed & " MB/s)")

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

        Page.FlushAll()

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim WriteSpeed As Decimal = TestNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = TestNumber / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Dim LogString = "Write FILES via " & ThreadNumber & " threads using " & MSeconds & "ms.                                                         "
        Console.WriteLine(LogString.Substring(0, 60) & "(ByteSize=" & SampleBytes.Length & ", Copies=" & TestNumber & ", WriteCopySpeed=" & WriteCopySpeed & " Copy/s, WriteSpeed=" & WriteSpeed & " MB/s)")

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
            Page.Write(FData)

            'Console.WriteLine("Write " & DataID & " ok.")
        Loop

        Page.FlushAll()

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim WriteSpeed As Decimal = TestNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = TestNumber / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Dim WriteWayString As String
        If IfRandomRead Then
            WriteWayString = "Random"
        Else
            WriteWayString = "Sequency"
        End If

        Dim LogString = WriteWayString & " write DB via single thread using " & MSeconds & "ms.                                                         "
        Console.WriteLine(LogString.Substring(0, 60) & "(ByteSize=" & SampleBytes.Length & ", Copies=" & TestNumber & ", WriteCopySpeed=" & WriteCopySpeed & " Copy/s, WriteSpeed=" & WriteSpeed & " MB/s)")

        If FairyDatabase.Config.IfDebugMode Then
            Dim FWriter As FileBufferWriter = Page.GetPage(1).PageFileBufferWriter
            If FWriter IsNot Nothing Then
                Console.WriteLine(Now.ToString & ": CreateNewCount_Index=" & FWriter.CreateNewCount_Index & ", CreateNewCount_Block=" & FWriter.CreateNewCount_Block)
            End If
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

        Page.FlushAll()

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim WriteSpeed As Decimal = TestNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = TestNumber / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Dim WriteWayString As String
        If IfRandomRead Then
            WriteWayString = "Random"
        Else
            WriteWayString = "Sequency"
        End If

        Dim LogString = WriteWayString & " write DB via " & ThreadNumber & " threads using " & MSeconds & "ms.                                                         "
        Console.WriteLine(LogString.Substring(0, 60) & "(ByteSize=" & SampleBytes.Length & ", Copies=" & TestNumber & ", WriteCopySpeed=" & WriteCopySpeed & " Copy/s, WriteSpeed=" & WriteSpeed & " MB/s)")

        For I = 1 To 2
            Console.WriteLine("Press any key to check data correct rate.")
            Console.ReadLine()

            If IfVerifyData Then
                'PrintFileLength()
                CheckDataCorrectRate()
            End If

        Next
    End Sub

    Private Shared Sub TestWriteInMultipleThreadsDO(ByVal ThreadID As Integer)

        'Execute
        Do While True
            Dim DataID As Int64 = GetNextDataID(TestWriteInMultipleThreads_IfRandomRead)
            If DataID <= 0 Then Exit Do

            Dim FData As New Data(DataID, SampleBytes)
            Page.Write(FData)

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
        For I = 1 To TestNumber
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
            If DataID > TestNumber Then Return 0
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
        For Each PageItem In Page.Pages
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

        For DataID = 1 To TestNumber
            Dim FData As Data = Page.Read(DataID)

            If FData Is Nothing OrElse FData.Value Is Nothing Then
                If FData IsNot Nothing Then
                    Console.WriteLine("Error Data: DataID=" & DataID & ", ID=" & FData.ID & ", StartPOS=" & FData.StartPOS)
                    If FData.StartPOS > 0 Then
                        Console.WriteLine("--")
                    End If
                End If
                ErrorCount = ErrorCount + 1
                Continue For
            End If

            Dim BytesHash As String = GetBytesHash(FData.Value)
            If SampleBytesHash <> BytesHash Then
                ErrorCount = ErrorCount + 1
                Console.WriteLine("Error Data: Wrong Start Position, FData.FirstValue=" & FData.Value.First & ", DataID=" & DataID & ", ID=" & FData.ID & ", StartPOS=" & FData.StartPOS)
            End If
        Next

        If ErrorCount = 0 Then
            Console.WriteLine("All data correct! (Number=" & TestNumber & ", CorrectRate=" & (TestNumber - ErrorCount) * 100 / TestNumber & "%)")
        Else
            Console.WriteLine(ErrorCount & " datas are error! (Number=" & TestNumber & ", CorrectRate=" & (TestNumber - ErrorCount) * 100 / TestNumber & "%)")
        End If
    End Sub

    Private Shared Sub ClearAndInitResources()
        'Init CurrentDataID, NextDataIDs
        CurrentDataID = 0

        NextDataIDs = New Concurrent.ConcurrentQueue(Of Int64)
        For Each ID In RandomIDs
            NextDataIDs.Enqueue(ID)
        Next

        'Console.WriteLine("Related resource clear and inited.")
    End Sub

    Private Shared Sub DBDispose()
        If Page.Pages IsNot Nothing Then
            For Each PageItem In Page.Pages
                If PageItem.Value IsNot Nothing Then
                    Try
                        PageItem.Value.ClearPageHeaderMemory()
                        PageItem.Value.Dispose()
                    Catch ex As Exception
                        Console.WriteLine(ex.ToString)
                    End Try
                End If
            Next
        End If
    End Sub

    Private Shared Sub DeleteFiles()
        Console.WriteLine("Press any key to delete test db files.")
        Console.ReadLine()

        'Clear Page Files and Memory
        If Page.Pages IsNot Nothing Then
            For Each PageItem In Page.Pages
                If PageItem.Value IsNot Nothing Then
                    Try
                        If System.IO.File.Exists(PageItem.Value.FilePath) Then System.IO.File.Delete(PageItem.Value.FilePath)
                        If System.IO.File.Exists(PageItem.Value.PendingRemoveBlocksFilePath) Then System.IO.File.Delete(PageItem.Value.PendingRemoveBlocksFilePath)
                    Catch ex As Exception
                        Console.WriteLine(ex.ToString)
                    End Try
                End If
            Next
        End If

        If System.IO.Directory.Exists(Config.DatabaseFolderPath) Then
            For Each FilePath In System.IO.Directory.GetFiles(Config.DatabaseFolderPath, "*.fdb", System.IO.SearchOption.AllDirectories)
                Try
                    System.IO.File.Delete(FilePath)
                Catch ex As Exception
                    Console.WriteLine(ex.ToString)
                End Try
            Next
        End If

        Page.Pages = New Concurrent.ConcurrentDictionary(Of Int64, Page)

        Console.WriteLine("Test db files are all deleted.")

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

Imports FairyDatabase

Public Class ReadPerformanceTest

    Private Shared ReadNumber As Int64 = 49999
    Private Shared ByteSize As Int64 = 100
    Private Shared IfVerifyData As Boolean = False

    Private Shared SampleBytes(ByteSize - 1) As Byte
    Private Shared SampleBytesHash As String

    Public Shared Sub Start()
        'Write Log
        Console.WindowWidth = 150
        Console.WriteLine("Read Performance Test: TestNumber=" & ReadNumber & ", ByteSize=" & ByteSize & ", IfVerifyData=" & IfVerifyData)
        Console.WriteLine()

        'Init FairyDatabase Config
        FairyDatabase.Config.Init()
        FairyDatabase.Config.IfDebugMode = False

        'Init RandomIDs
        Randomize()
        PrepareRandomIDs()

        'Prepare Test Data
        PrepareTestData()

        'Execute Test
        Do While True
            Console.WriteLine("Start to test read performance.")
            Console.WriteLine()

            For Each ThreadNumber In New Integer() {1, 2, 4, 8, 16, 32, 64, 128, 256}
                Console.WriteLine("-------------------------------------------------------------------------------------")
                Console.WriteLine()

                For Each IfRandomRead In New Boolean() {False, True}
                    If IfRandomRead Then
                        Console.WriteLine("Start to test RANDOM read performance via " & ThreadNumber & " threads.")
                    Else
                        Console.WriteLine("Start to test SEQUENCY read performance via " & ThreadNumber & " threads.")
                    End If
                    Console.WriteLine()

                    If ThreadNumber = 1 Then
                        For J = 1 To 5
                            TestReadFilesInSingleThread(IfRandomRead)
                        Next
                        Console.WriteLine()

                        For J = 1 To 5
                            TestReadInSingleThread(IfRandomRead)
                        Next
                        Console.WriteLine()
                    Else
                        For J = 1 To 5
                            TestReadFilesInMultipleThreads(ThreadNumber, IfRandomRead)
                        Next
                        Console.WriteLine()

                        For J = 1 To 5
                            TestReadInMultipleThreads(ThreadNumber, IfRandomRead)
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

#Region "Test Read From File"

    Private Shared TestReadFileFolderPath As String = "temp\readperformancetest\"

    Private Shared Sub TestReadFilesInSingleThread(ByVal IfRandomRead As Boolean)
        'Clear and Init Resources
        ClearAndInitResources(IfRandomRead)

        'Init Parameters
        Dim FilePath As String
        Dim StartTime As DateTime = Now

        'Exectue
        Do While True
            Dim DataID As Int64 = GetNextDataID(IfRandomRead)
            If DataID <= 0 Then Exit Do

            FilePath = TestReadFileFolderPath & Int(DataID / 1000) & "K\" & DataID & ".dat"
            Dim FData As New Data(DataID)
            FData.Value = System.IO.File.ReadAllBytes(FilePath)

            'Console.WriteLine("Read " & DataID & " ok. (Value.Length=" & FData.Value.Length & ")")
        Loop

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim ReadSpeed As Decimal = ReadNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        ReadSpeed = Int(ReadSpeed * 1000) / 1000
        Dim ReadCopySpeed As Decimal = ReadNumber / MSeconds * 1000
        ReadCopySpeed = Int(ReadCopySpeed)

        Dim ReadWayString As String
        If IfRandomRead Then
            ReadWayString = "Random"
        Else
            ReadWayString = "Sequency"
        End If

        Dim LogString = ReadWayString & " read FILES via single thread using " & MSeconds & "ms.                                                        "
        Console.WriteLine(LogString.Substring(0, 60) & "(ByteSize=" & SampleBytes.Length & ", Copies=" & ReadNumber & ", ReadCopySpeed=" & ReadCopySpeed & " Copy/s, ReadSpeed=" & ReadSpeed & " MB/s)")
    End Sub

    Private Shared Sub TestReadFilesInMultipleThreads(ByVal ThreadNumber As Integer, ByVal IfRandomRead As Boolean)
        'Clear and Init Resources
        ClearAndInitResources(IfRandomRead)

        'Init Parameters
        ReDim ManualResetEvents(ThreadNumber - 1)
        TestReadInMultipleThreads_IfRandomRead = IfRandomRead

        Dim StartTime As DateTime = Now

        'Execute
        For ThreadID = 1 To ThreadNumber
            ManualResetEvents(ThreadID - 1) = New Threading.ManualResetEvent(False)

            System.Threading.ThreadPool.QueueUserWorkItem(New System.Threading.WaitCallback(AddressOf TestReadFilesInMultipleThreadsDO), ThreadID)
        Next

        For ThreadID = 1 To ThreadNumber
            ManualResetEvents(ThreadID - 1).WaitOne()
        Next

        Page.FlushAll()

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim ReadSpeed As Decimal = ReadNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        ReadSpeed = Int(ReadSpeed * 1000) / 1000
        Dim ReadCopySpeed As Decimal = ReadNumber / MSeconds * 1000
        ReadCopySpeed = Int(ReadCopySpeed)

        Dim ReadWayString As String
        If IfRandomRead Then
            ReadWayString = "Random"
        Else
            ReadWayString = "Sequency"
        End If

        Dim LogString As String = ReadWayString & " read FILES via " & ThreadNumber & " Threads using " & MSeconds & "ms.                                 "
        Console.WriteLine(LogString.Substring(0, 60) & "(ByteSize=" & SampleBytes.Length & ", Copies=" & ReadNumber & ", ReadCopySpeed=" & ReadCopySpeed & " Copy/s, ReadSpeed=" & ReadSpeed & " MB/s)")
    End Sub

    Private Shared Sub TestReadFilesInMultipleThreadsDO(ByVal ThreadID As Integer)
        'Init Parameters
        Dim FilePath As String

        'Execute
        Do While True
            Dim DataID As Int64 = GetNextDataID(TestReadInMultipleThreads_IfRandomRead)
            If DataID <= 0 Then Exit Do

            FilePath = TestReadFileFolderPath & Int(DataID / 1000) & "K\" & DataID & ".dat"
            Dim FData As New Data(DataID)
            FData.Value = System.IO.File.ReadAllBytes(FilePath)

            'Console.WriteLine("(" & ThreadID & ") Read " & DataID & " ok. (Value.Length=" & FData.Value.Length & ")")
        Loop

        'Set Signal
        ManualResetEvents(ThreadID - 1).Set()
    End Sub


#End Region


#Region "Test Read From DB"

    Private Shared Sub TestReadInSingleThread(ByVal IfRandomRead As Boolean)
        'Clear and Init Resources
        ClearAndInitResources(IfRandomRead)

        'Init Parameters
        Dim StartTime As DateTime = Now

        'Exectue
        Do While True
            Dim DataID As Int64 = GetNextDataID(IfRandomRead)
            If DataID <= 0 Then Exit Do

            Dim FData As Data = Page.Read(DataID)
            If IfVerifyData Then ReadSamples.Enqueue(FData)

            'Console.WriteLine("Read " & DataID & " ok. (Value.Length=" & FData.Value.Length & ")")
        Loop

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim ReadSpeed As Decimal = ReadNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        ReadSpeed = Int(ReadSpeed * 1000) / 1000
        Dim ReadCopySpeed As Decimal = ReadNumber / MSeconds * 1000
        ReadCopySpeed = Int(ReadCopySpeed)

        Dim ReadWayString As String
        If IfRandomRead Then
            ReadWayString = "Random"
        Else
            ReadWayString = "Sequency"
        End If

        Dim LogString As String = ReadWayString & " read DB via single thread using " & MSeconds & "ms.                                                "
        Console.WriteLine(LogString.Substring(0, 60) & "(ByteSize=" & SampleBytes.Length & ", Copies=" & ReadNumber & ", ReadCopySpeed=" & ReadCopySpeed & " Copy/s, ReadSpeed=" & ReadSpeed & " MB/s)")

        If IfVerifyData Then
            CheckDataCorrectRate()
        End If

    End Sub

    Private Shared ManualResetEvents As Threading.ManualResetEvent()
    Private Shared TestReadInMultipleThreads_IfRandomRead As Boolean
    Private Shared Sub TestReadInMultipleThreads(ByVal ThreadNumber As Integer, ByVal IfRandomRead As Boolean)
        'Clear and Init Resources
        ClearAndInitResources(IfRandomRead)

        'Init Parameters
        ReDim ManualResetEvents(ThreadNumber - 1)
        TestReadInMultipleThreads_IfRandomRead = IfRandomRead

        Dim StartTime As DateTime = Now

        'Execute
        For ThreadID = 1 To ThreadNumber
            ManualResetEvents(ThreadID - 1) = New Threading.ManualResetEvent(False)

            System.Threading.ThreadPool.QueueUserWorkItem(New System.Threading.WaitCallback(AddressOf TestReadInMultipleThreadsDO), ThreadID)
        Next

        For ThreadID = 1 To ThreadNumber
            ManualResetEvents(ThreadID - 1).WaitOne()
        Next

        Page.FlushAll()

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim ReadSpeed As Decimal = ReadNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        ReadSpeed = Int(ReadSpeed * 1000) / 1000
        Dim ReadCopySpeed As Decimal = ReadNumber / MSeconds * 1000
        ReadCopySpeed = Int(ReadCopySpeed)

        Dim ReadWayString As String
        If IfRandomRead Then
            ReadWayString = "Random"
        Else
            ReadWayString = "Sequency"
        End If

        Dim LogString As String = ReadWayString & " read DB via " & ThreadNumber & " Threads using " & MSeconds & "ms.                               "
        Console.WriteLine(LogString.Substring(0, 60) & "(ByteSize=" & SampleBytes.Length & ", Copies=" & ReadNumber & ", ReadCopySpeed=" & ReadCopySpeed & " Copy/s, ReadSpeed=" & ReadSpeed & " MB/s)")

        If IfVerifyData Then
            CheckDataCorrectRate()
        End If
    End Sub

    Private Shared Sub TestReadInMultipleThreadsDO(ByVal ThreadID As Integer)
        'Execute
        Do While True
            Dim DataID As Int64 = GetNextDataID(TestReadInMultipleThreads_IfRandomRead)
            If DataID <= 0 Then Exit Do

            Dim FData As Data = Page.Read(DataID)
            If IfVerifyData Then ReadSamples.Enqueue(FData)

            'Console.WriteLine("(" & ThreadID & ") Read " & DataID & " ok. (Value.Length=" & FData.Value.Length & ")")
        Loop

        'Set Signal
        ManualResetEvents(ThreadID - 1).Set()
    End Sub

#End Region


#Region "PrepareTestData, RandomIDs, GetNextDataID"
    Private Shared Sub PrepareTestData()
        Console.WriteLine("Preparing Test Data...")

        'Clear and Init Resources
        ClearAndInitResources()

        'Generate SampleBytes
        Randomize()
        For I = 0 To ByteSize - 1
            Dim Number As Single = Rnd()
            SampleBytes(I) = Int(Number * 256)
        Next
        SampleBytesHash = WritePerformanceTest.GetBytesHash(SampleBytes)

        'Exectue
        For Count = 1 To ReadNumber
            Dim FData As New Data(Count, SampleBytes)
            Page.Write(FData)

            'Console.WriteLine("Write " & Count & " ok.")
        Next

        Page.FlushAll()

        'Prepare Test Files
        PrepareTestFiles()

        'Output Result
        Console.WriteLine("Test Data and Files are all get prepared.")
        Console.WriteLine()
    End Sub

    Private Shared Sub PrepareTestFiles()
        Console.WriteLine("Preparing Test Files...")

        'Init Parameters
        Dim SubFolderPath As String
        Dim FilePath As String

        'Clear Resources
        If System.IO.Directory.Exists(TestReadFileFolderPath) Then
            For Each SubFolderPath In System.IO.Directory.GetDirectories(TestReadFileFolderPath)
                System.IO.Directory.Delete(SubFolderPath, True)
            Next
        Else
            System.IO.Directory.CreateDirectory(TestReadFileFolderPath)
        End If

        'Init Parameters
        CurrentDataID = 0

        Dim StartTime As DateTime = Now

        'Exectue
        Do While True
            Dim DataID As Int64 = GetNextDataID(False)
            If DataID <= 0 Then Exit Do

            Dim FData As New Data(DataID, SampleBytes)

            SubFolderPath = TestReadFileFolderPath & Int(DataID / 1000) & "K"
            If System.IO.Directory.Exists(SubFolderPath) = False Then System.IO.Directory.CreateDirectory(SubFolderPath)
            FilePath = SubFolderPath & "\" & DataID & ".dat"

            System.IO.File.WriteAllBytes(FilePath, FData.Value)

            'Console.WriteLine("Write " & DataID & " ok.")
        Loop

        Page.FlushAll()

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim WriteSpeed As Decimal = ReadNumber * SampleBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = ReadNumber / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        'Console.WriteLine("Write files via single thread using " & MSeconds & "ms. (ByteSize=" & SampleBytes.Length & ", Copies=" & ReadNumber & ", WriteCopySpeed=" & WriteCopySpeed & "Copy/s, WriteSpeed=" & WriteSpeed & "MB/s)")

    End Sub

    Private Shared RandomIDs As List(Of Int64)

    Private Shared Sub PrepareRandomIDs()
        Randomize()

        'Console.WriteLine("Preparing Random IDs...")

        Dim RemainIDs As New List(Of Int64)
        For I = 1 To ReadNumber
            RemainIDs.Add(I)
        Next

        RandomIDs = New List(Of Int64)
        Do While RandomIDs.Count < ReadNumber
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

        Dim DataID As Int64
        If NextDataIDs.TryDequeue(DataID) Then
            Return DataID
        Else
            Return 0
        End If

    End Function

#End Region


#Region "Tools"

    Private Shared Sub PrintFileLength()
        For Each PageItem In Page.Pages
            If PageItem.Value IsNot Nothing Then
                Try
                    Console.WriteLine("Page " & PageItem.Value.ID & " Length = " & PageItem.Value.FileLength)
                Catch ex As Exception
                    Console.WriteLine(ex.ToString)
                End Try
            End If
        Next
    End Sub

    Private Shared ReadSamples As Concurrent.ConcurrentQueue(Of Data)

    Private Shared Sub CheckDataCorrectRate()
        'Check Data Number
        Dim FDict As New SortedDictionary(Of Int64, Int64)
        For Each item In ReadSamples
            If FDict.ContainsKey(item.ID) = False Then
                FDict.Add(item.ID, 0)
            End If
        Next
        Console.WriteLine("ReadSamples.count=" & ReadSamples.Count & ", RealSamplesCount=" & FDict.Count & " (" & FDict.First.Key & " - " & FDict.Last.Key & ")")

        'Check Data Correct Rate
        Dim ErrorCount As Integer = 0

        Dim FData As Data
        Dim ReadCount As Int64 = ReadSamples.Count

        Do While ReadSamples.Count > 0
            ReadSamples.TryDequeue(FData)

            If FData Is Nothing OrElse FData.Value Is Nothing Then
                ErrorCount = ErrorCount + 1
                Continue Do
            End If

            Dim BytesHash As String = WritePerformanceTest.GetBytesHash(FData.Value)
            If SampleBytesHash <> BytesHash Then
                ErrorCount = ErrorCount + 1
            End If
        Loop

        If ErrorCount = 0 Then
            Console.WriteLine("All data correct! (Number=" & ReadCount & ", CorrectRate=" & (ReadCount - ErrorCount) * 100 / ReadCount & "%)")
        Else
            Console.WriteLine(ErrorCount & " datas are error! (Number=" & ReadSamples.Count & ", CorrectRate=" & (ReadSamples.Count - ErrorCount) * 100 / ReadSamples.Count & "%)")
        End If

        ReadSamples = Nothing
    End Sub


    Private Shared Sub ClearAndInitResources(Optional ByVal IfRandomRead As Boolean = True)
        'Clear Page Files and Memory
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

        'Init CurrentDataID, NextDataIDs
        CurrentDataID = 0

        If IfRandomRead Then
            NextDataIDs = New Concurrent.ConcurrentQueue(Of Int64)
            For Each ID In RandomIDs
                NextDataIDs.Enqueue(ID)
            Next
        Else
            NextDataIDs = New Concurrent.ConcurrentQueue(Of Int64)
            For ID = 1 To ReadNumber
                NextDataIDs.Enqueue(ID)
            Next
        End If


        'Init ReadSamples
        ReadSamples = New Concurrent.ConcurrentQueue(Of Data)

        'Console.WriteLine("Related resource clear and inited.")
    End Sub


#End Region

End Class

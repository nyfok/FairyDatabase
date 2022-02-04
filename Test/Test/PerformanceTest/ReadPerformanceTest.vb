Imports FairyDatabase
Public Class ReadPerformanceTest

    Private Shared ReadNumber As Int64 = 9999
    Private Shared ByteSize As Int64 = 100

    Private Shared SampleBytes(ByteSize - 1) As Byte

    Public Shared Sub Start()
        'Write Log
        Console.WindowWidth = 150
        Console.WriteLine("Test: ByteSize=" & ByteSize & ", TestNumber=" & ReadNumber)
        Console.WriteLine()

        'Init FairyDatabase Config
        FairyDatabase.Config.Init(,, False)
        FairyDatabase.Config.IfDebugMode = False

        'Init RandomIDs
        Randomize()
        PrepareRandomIDs()

        'Prepare Test Data
        PrepareTestData()
        Console.WriteLine()

        'Test Single Thread
        For TestNumber = 1 To 2
            Console.WriteLine("Processing Single Thread Test...")

            For Each IfRandomRead In New Boolean() {False, True}
                TestReadFilesInSingleThread(IfRandomRead)
                TestReadInSingleThread(IfRandomRead)
            Next

            Console.ReadLine()
        Next

        'Test Multiple Threads
        For Each ThreadNumber In New Integer() {2, 4, 8, 16}
            Console.WriteLine("Processing Multiple Threads Test...")

            For Each IfRandomRead In New Boolean() {False, True}
                TestReadFilesInMultipleThreads(ThreadNumber, False)
                TestReadInMultipleThreads(ThreadNumber, False)
            Next

            Console.ReadLine()
        Next

    End Sub

#Region "Test Read From File"

    Private Shared TestReadFileFolderPath As String = "temp\readperformancetest\"

    Private Shared Sub TestReadFilesInSingleThread(ByVal IfRandomRead As Boolean)

        'Init Parameters
        CurrentDataID = 0
        NextDataIDs = New HashSet(Of Int64)
        NextDataIDs.UnionWith(RandomIDs)

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
        Console.WriteLine(ReadWayString & " read files via single thread using " & MSeconds & "ms.        " & vbTab & "(ByteSize=" & SampleBytes.Length & ", Copies=" & ReadNumber & ", ReadCopySpeed=" & ReadCopySpeed & " Copy/s, ReadSpeed=" & ReadSpeed & " MB/s)")
    End Sub

    Private Shared Sub TestReadFilesInMultipleThreads(ByVal ThreadNumber As Integer, ByVal IfRandomRead As Boolean)
        'Init Parameters
        ReDim ManualResetEvents(ThreadNumber - 1)
        TestReadInMultipleThreads_IfRandomRead = IfRandomRead

        CurrentDataID = 0
        NextDataIDs = New HashSet(Of Int64)
        NextDataIDs.UnionWith(RandomIDs)

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
        Console.WriteLine(ReadWayString & " read files via " & ThreadNumber & " Threads using " & MSeconds & "ms.     " & vbTab & "(ByteSize=" & SampleBytes.Length & ", Copies=" & ReadNumber & ", ReadCopySpeed=" & ReadCopySpeed & " Copy/s, ReadSpeed=" & ReadSpeed & " MB/s)")
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
        'Clear Resources
        Clear()

        'Init Parameters
        CurrentDataID = 0
        NextDataIDs = New HashSet(Of Int64)
        NextDataIDs.UnionWith(RandomIDs)

        Dim StartTime As DateTime = Now

        'Exectue
        Do While True
            Dim DataID As Int64 = GetNextDataID(IfRandomRead)
            If DataID <= 0 Then Exit Do

            Dim FData As Data = Page.Read(DataID)

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
        Console.WriteLine(ReadWayString & " read db via single thread using " & MSeconds & "ms.         " & vbTab & "(ByteSize=" & SampleBytes.Length & ", Copies=" & ReadNumber & ", ReadCopySpeed=" & ReadCopySpeed & " Copy/s, ReadSpeed=" & ReadSpeed & " MB/s)")
    End Sub

    Private Shared ManualResetEvents As Threading.ManualResetEvent()
    Private Shared TestReadInMultipleThreads_IfRandomRead As Boolean
    Private Shared Sub TestReadInMultipleThreads(ByVal ThreadNumber As Integer, ByVal IfRandomRead As Boolean)
        'Clear Resources
        Clear()

        'Init Parameters
        ReDim ManualResetEvents(ThreadNumber - 1)
        TestReadInMultipleThreads_IfRandomRead = IfRandomRead

        CurrentDataID = 0
        NextDataIDs = New HashSet(Of Int64)
        NextDataIDs.UnionWith(RandomIDs)

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
        Console.WriteLine(ReadWayString & " read db via " & ThreadNumber & " Threads using " & MSeconds & "ms.         " & vbTab & "(ByteSize=" & SampleBytes.Length & ", Copies=" & ReadNumber & ", ReadCopySpeed=" & ReadCopySpeed & " Copy/s, ReadSpeed=" & ReadSpeed & " MB/s)")
    End Sub

    Private Shared Sub TestReadInMultipleThreadsDO(ByVal ThreadID As Integer)

        'Execute
        Do While True
            Dim DataID As Int64 = GetNextDataID(TestReadInMultipleThreads_IfRandomRead)
            If DataID <= 0 Then Exit Do

            Dim FData As Data = Page.Read(DataID)

            'Console.WriteLine("(" & ThreadID & ") Read " & DataID & " ok. (Value.Length=" & FData.Value.Length & ")")
        Loop

        'Set Signal
        ManualResetEvents(ThreadID - 1).Set()
    End Sub

#End Region


#Region "PrepareTestData, RandomIDs, GetNextDataID"
    Private Shared Sub PrepareTestData()
        Console.WriteLine("Preparing Test Data...")

        'Clear
        Clear()

        'Generate SampleBytes
        Randomize()
        For I = 0 To ByteSize - 1
            Dim Number As Single = Rnd()
            SampleBytes(I) = Int(Number * 256)
        Next

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
        NextDataIDs = New HashSet(Of Int64)
        NextDataIDs.UnionWith(RandomIDs)

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

    Private Shared RandomIDs As HashSet(Of Int64)

    Private Shared Sub PrepareRandomIDs()
        Randomize()

        Dim RemainIDs As New HashSet(Of Int64)
        For I = 1 To ReadNumber
            RemainIDs.Add(I)
        Next

        RandomIDs = New HashSet(Of Int64)
        Do While RandomIDs.Count < ReadNumber
            Dim Index As Integer = Int(Rnd() * RemainIDs.Count)
            Dim ID As Int64 = RemainIDs.ElementAt(Index)
            RandomIDs.Add(ID)
            RemainIDs.Remove(ID)
        Loop
        RemainIDs = Nothing
    End Sub

    Private Shared CurrentDataID As Int64 = 0
    Private Shared GetNextDataIDLock As New Object
    Private Shared NextDataIDs As HashSet(Of Int64)

    Private Shared Function GetNextDataID(ByVal IfRandomRead As Boolean) As Int64
        SyncLock GetNextDataIDLock
            If IfRandomRead = False Then
                CurrentDataID = CurrentDataID + 1
                If CurrentDataID <= ReadNumber Then
                    Return CurrentDataID
                Else
                    Return 0
                End If
            Else
                If NextDataIDs.Count = 0 Then Return 0

                CurrentDataID = NextDataIDs.First
                NextDataIDs.Remove(CurrentDataID)
                Return CurrentDataID
            End If
        End SyncLock
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


    Private Shared Sub Clear()

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

        Page.Pages = New Concurrent.ConcurrentDictionary(Of Int64, Page)

        'Console.WriteLine("Related resource cleared.")
    End Sub


#End Region

End Class

Public Class TestRead

    Private Shared ReadNumber As Int64 = 100
    Private Shared SpaceSize As Int64 = 500
    Private Shared ByteSize As Int64 = 1024

    Private Shared TestReadBytes(ByteSize - 1) As Byte
    Private Shared RandomReadIDs As HashSet(Of Int64)

    Public Shared Sub Start()
        'Prepare Read Data
        Clear()    'force clear first
        PrepareReadData()

        'Init RandomReadIDs
        Randomize()
        RandomReadIDs = New HashSet(Of Int64)
        Do While RandomReadIDs.Count < ReadNumber
            Dim ID As Int64 = Int(Rnd() * SpaceSize) + 1
            If RandomReadIDs.Contains(ID) = False Then RandomReadIDs.Add(ID)
        Loop

        'Test Single Thread
        TestReadInSingleThread(False)
        Console.ReadLine()

        TestReadInSingleThread(True)
        Console.ReadLine()

        'Test Multiple Threads
        For ThreadNumber = 1 To 16
            TestReadInMultipleThreads(ThreadNumber, False)
            Console.ReadLine()

            TestReadInMultipleThreads(ThreadNumber, True)
            Console.ReadLine()
        Next
    End Sub

    Private Shared Sub PrepareReadData()
        'Check if exists related Resources
        Dim IfAllPageExists As Boolean = True
        Dim MaxPageID As Integer = Page.GetPageID(SpaceSize)
        For PageID = 0 To MaxPageID
            Dim FilePath As String = Page.GetPageFilePath(PageID)
            If System.IO.File.Exists(FilePath) = False Then
                IfAllPageExists = False
                Exit For
            End If
        Next
        If IfAllPageExists Then
            Console.WriteLine("Related resource prepared.")
            Return
        End If

        'Clear
        Clear()

        'Exectue
        For Count = 1 To SpaceSize

            Dim FData As New Data(Count, TestReadBytes)
            Page.Write(FData)

            Console.WriteLine("Write " & Count & " ok.")
        Next

        Page.FlushAll()

        'Output Result
        Console.WriteLine("Related resource prepared.")

    End Sub

    Private Shared Sub TestReadInSingleThread(ByVal IfRandomRead As Boolean)

        'Init Parameters
        CurrentDataID = 0
        NextDataIDs = New HashSet(Of Int64)
        NextDataIDs.UnionWith(RandomReadIDs)

        Dim StartTime As DateTime = Now

        'Exectue
        Do While True
            Dim DataID As Int64 = GetNextDataID(IfRandomRead)
            If DataID <= 0 Then Exit Do

            Dim FData As Data = Page.Read(DataID)

            Console.WriteLine("Read " & DataID & " ok. (Value.Length=" & FData.Value.Length & ")")
        Loop

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim ReadSpeed As Decimal = ReadNumber * TestReadBytes.Length / 1024 / 1024 / MSeconds * 1000
        ReadSpeed = Int(ReadSpeed * 1000) / 1000
        Dim ReadCopySpeed As Decimal = ReadNumber / MSeconds * 1000
        ReadCopySpeed = Int(ReadCopySpeed)

        Dim ReadWayString As String
        If IfRandomRead Then
            ReadWayString = "Random"
        Else
            ReadWayString = "Sequency"
        End If
        Console.WriteLine(ReadWayString & " read via single thread using " & MSeconds & "ms. (ByteSize=" & TestReadBytes.Length & ", Copies=" & ReadNumber & ", ReadCopySpeed=" & ReadCopySpeed & "Copy/s, ReadSpeed=" & ReadSpeed & "MB/s)")
    End Sub

    Private Shared ManualResetEvents As Threading.ManualResetEvent()
    Private Shared TestReadInMultipleThreads_IfRandomRead As Boolean
    Private Shared Sub TestReadInMultipleThreads(ByVal ThreadNumber As Integer, ByVal IfRandomRead As Boolean)
        'Init Parameters
        ReDim ManualResetEvents(ThreadNumber - 1)
        TestReadInMultipleThreads_IfRandomRead = IfRandomRead

        CurrentDataID = 0
        NextDataIDs = New HashSet(Of Int64)
        NextDataIDs.UnionWith(RandomReadIDs)

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
        Dim ReadSpeed As Decimal = ReadNumber * TestReadBytes.Length / 1024 / 1024 / MSeconds * 1000
        ReadSpeed = Int(ReadSpeed * 1000) / 1000
        Dim ReadCopySpeed As Decimal = ReadNumber / MSeconds * 1000
        ReadCopySpeed = Int(ReadCopySpeed)

        Dim ReadWayString As String
        If IfRandomRead Then
            ReadWayString = "Random"
        Else
            ReadWayString = "Sequency"
        End If
        Console.WriteLine(ReadWayString & " read via " & ThreadNumber & " Threads using " & MSeconds & "ms. (ByteSize=" & TestReadBytes.Length & ", Copies=" & ReadNumber & ", ReadCopySpeed=" & ReadCopySpeed & "Copy/s, ReadSpeed=" & ReadSpeed & "MB/s)")
    End Sub

    Private Shared Sub TestReadInMultipleThreadsDO(ByVal ThreadID As Integer)

        'Execute
        Do While True
            Dim DataID As Int64 = GetNextDataID(TestReadInMultipleThreads_IfRandomRead)
            If DataID <= 0 Then Exit Do

            Dim FData As Data = Page.Read(DataID)

            Console.WriteLine("(" & ThreadID & ") Read " & DataID & " ok. (Value.Length=" & FData.Value.Length & ")")
        Loop

        'Set Signal
        ManualResetEvents(ThreadID - 1).Set()
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
                        PageItem.Value.WriteLengthToMemory(0)
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
                End Try
            Next
        End If

        Page.Pages = New Concurrent.ConcurrentDictionary(Of Int64, Page)
    End Sub

End Class

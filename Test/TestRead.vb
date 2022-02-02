Public Class TestRead

    Private Shared TestReadCount As Int64 = 100
    Private Shared TestSpaceSize As Int64 = 500
    Private Shared ByteSize As Int64 = 1024

    Private Shared TestReadBytes(ByteSize - 1) As Byte

    Public Shared Sub Start()

        'Prepare Read Data
        'Clear()    'force clear first
        PrepareReadData()

        'Test Single Thread
        TestReadInSingleThread()
        Console.ReadLine()

        'Test Multiple Threads
        For TestThreadNumber = 1 To 16
            ThreadNumber = TestThreadNumber
            TestReadInMultipleThreads()
            Console.ReadLine()
        Next
    End Sub

    Private Shared Sub PrepareReadData()
        'Check if exists related Resources
        Dim IfAllPageExists As Boolean = True
        Dim MaxPageID As Integer = Page.GetPageID(TestSpaceSize)
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
        For Count = 1 To TestSpaceSize

            Dim FData As New Data(Count, TestReadBytes)
            Page.Write(FData)

            Console.WriteLine("Write " & Count & " ok.")
        Next

        Page.FlushAll()

        'Output Result
        Console.WriteLine("Related resource prepared.")

    End Sub

    Private Shared Sub TestReadInSingleThread()

        'Init Parameters
        Dim StartTime As DateTime = Now

        'Exectue
        For Count = 1 To TestReadCount

            Dim FData As Data = Page.Read(Count)

            Console.WriteLine("Read " & Count & " ok. (Value.Length=" & FData.Value.Length & ")")
        Next

        Page.FlushAll()

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim ReadSpeed As Decimal = TestReadCount * TestReadBytes.Length / 1024 / 1024 / MSeconds * 1000
        ReadSpeed = Int(ReadSpeed * 1000) / 1000
        Dim ReadCopySpeed As Decimal = TestReadCount / MSeconds * 1000
        ReadCopySpeed = Int(ReadCopySpeed)

        Console.WriteLine("Single Thread using " & MSeconds & "ms. (ByteSize=" & TestReadBytes.Length & ", Copies=" & TestReadCount & ", ReadCopySpeed=" & ReadCopySpeed & "Copy/s, ReadSpeed=" & ReadSpeed & "MB/s)")

    End Sub

    Private Shared ManualResetEvents As Threading.ManualResetEvent()
    Private Shared ThreadNumber As Integer

    Private Shared Sub TestReadInMultipleThreads()
        'Init Parameters
        Dim StartTime As DateTime = Now
        ReDim ManualResetEvents(ThreadNumber - 1)

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
        Dim ReadSpeed As Decimal = TestReadCount * TestReadBytes.Length / 1024 / 1024 / MSeconds * 1000
        ReadSpeed = Int(ReadSpeed * 1000) / 1000
        Dim ReadCopySpeed As Decimal = TestReadCount / MSeconds * 1000
        ReadCopySpeed = Int(ReadCopySpeed)

        Console.WriteLine(ThreadNumber & " Multiple Thread using " & MSeconds & "ms. (ByteSize=" & TestReadBytes.Length & ", Copies=" & TestReadCount & ", ReadCopySpeed=" & ReadCopySpeed & "Copy/s, ReadSpeed=" & ReadSpeed & "MB/s)")

    End Sub

    Private Shared Sub TestReadInMultipleThreadsDO(ByVal ThreadID As Integer)

        'Execute
        For Count = 1 To TestReadCount / ThreadNumber

            Dim DataID As Int64 = (ThreadID - 1) * TestReadCount / ThreadNumber + Count

            Dim FData As Data = Page.Read(DataID)

            Console.WriteLine("(" & ThreadID & ") Read " & DataID & " ok. (Value.Length=" & FData.Value.Length & ")")
        Next

        'Set Signal
        ManualResetEvents(ThreadID - 1).Set()
    End Sub

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

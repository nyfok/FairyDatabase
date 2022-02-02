Public Class TestWrite

    Private Shared ByteSize As Int64 = 1024
    Private Shared TestWriteCount As Int64 = 100
    Private Shared TestWriteBytes(ByteSize - 1) As Byte

    Public Shared Sub Start()
        'Test Single Thread
        TestWriteInSingleThread()
        Console.ReadLine()

        'Test Multiple Threads
        For TestThreadNumber = 1 To 16
            ThreadNumber = TestThreadNumber
            TestWriteInMultipleThreads()
            Console.ReadLine()
        Next
    End Sub


    Private Shared Sub TestWriteInSingleThread()
        'Clear Resources
        Clear()
        Console.WriteLine("Related resource cleared.")

        'Init Parameters
        Dim StartTime As DateTime = Now

        'Exectue
        For Count = 1 To TestWriteCount

            Dim FData As New Data(Count, TestWriteBytes)
            Page.Write(FData)

            Console.WriteLine("Write " & Count & " ok.")
        Next

        Page.FlushAll()

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim WriteSpeed As Decimal = TestWriteCount * TestWriteBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = TestWriteCount / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Console.WriteLine("Single Thread using " & MSeconds & "ms. (ByteSize=" & TestWriteBytes.Length & ", Copies=" & TestWriteCount & ", WriteCopySpeed=" & WriteCopySpeed & "Copy/s, WriteSpeed=" & WriteSpeed & "MB/s)")

        PrintFileLength()

    End Sub

    Private Shared ManualResetEvents As Threading.ManualResetEvent()
    Private Shared ThreadNumber As Integer

    Private Shared Sub TestWriteInMultipleThreads()
        'Clear Resources
        Clear()
        Console.WriteLine("Related resource cleared.")

        'Init Parameters
        Dim StartTime As DateTime = Now
        ReDim ManualResetEvents(ThreadNumber - 1)

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
        Dim WriteSpeed As Decimal = TestWriteCount * TestWriteBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = TestWriteCount / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Console.WriteLine(ThreadNumber & " Multiple Thread using " & MSeconds & "ms. (ByteSize=" & TestWriteBytes.Length & ", Copies=" & TestWriteCount & ", WriteCopySpeed=" & WriteCopySpeed & "Copy/s, WriteSpeed=" & WriteSpeed & "MB/s)")

        PrintFileLength()
    End Sub

    Private Shared Sub TestWriteInMultipleThreadsDO(ByVal ThreadID As Integer)

        'Execute
        For Count = 1 To TestWriteCount / ThreadNumber

            Dim DataID As Int64 = (ThreadID - 1) * TestWriteCount / ThreadNumber + Count

            Dim FData As New Data(Count, TestWriteBytes)
            Page.Write(FData)

            Console.WriteLine("(" & ThreadID & ") Write " & DataID & " ok.")
        Next

        'Set Signal
        ManualResetEvents(ThreadID-1).Set()
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

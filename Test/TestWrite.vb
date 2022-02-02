Public Class TestWrite

    Private Shared WriteNumber As Int64 = 1000
    Private Shared ByteSize As Int64 = 1000

    Private Shared WriteBytes(ByteSize - 1) As Byte

    Public Shared Sub Start()
        'Test Single Thread
        For TestNumber = 1 To 5
            TestWriteInSingleThread()
            Console.ReadLine()
        Next

        'Test Multiple Threads
        For ThreadNumber = 1 To 16
            TestWriteInMultipleThreads(ThreadNumber)
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
        For Count = 1 To WriteNumber
            Dim FData As New Data(Count, WriteBytes)
            Page.Write(FData)

            'Console.WriteLine("Write " & Count & " ok.")
        Next

        Page.FlushAll()

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim WriteSpeed As Decimal = WriteNumber * WriteBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = WriteNumber / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Console.WriteLine("Sequency write via single thread using " & MSeconds & "ms. (ByteSize=" & WriteBytes.Length & ", Copies=" & WriteNumber & ", WriteCopySpeed=" & WriteCopySpeed & "Copy/s, WriteSpeed=" & WriteSpeed & "MB/s)")

        PrintFileLength()

    End Sub

    Private Shared ManualResetEvents As Threading.ManualResetEvent()

    Private Shared Sub TestWriteInMultipleThreads(ByVal ThreadNumber As Integer)
        'Clear Resources
        Clear()
        Console.WriteLine("Related resource cleared.")

        'Init Parameters
        ReDim ManualResetEvents(ThreadNumber - 1)
        CurrentDataID = 0

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
        Dim WriteSpeed As Decimal = WriteNumber * WriteBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = WriteNumber / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Console.WriteLine("Sequency write via " & ThreadNumber & " threads using " & MSeconds & "ms. (ByteSize=" & WriteBytes.Length & ", Copies=" & WriteNumber & ", WriteCopySpeed=" & WriteCopySpeed & "Copy/s, WriteSpeed=" & WriteSpeed & "MB/s)")

        PrintFileLength()
    End Sub

    Private Shared Sub TestWriteInMultipleThreadsDO(ByVal ThreadID As Integer)

        'Execute
        Do While True
            Dim DataID As Int64 = GetNextDataID()
            If DataID > WriteNumber Then Exit Do

            Dim FData As New Data(DataID, WriteBytes)
            Page.Write(FData)

            'Console.WriteLine("(" & ThreadID & ") Write " & DataID & " ok.")
        Loop

        'Set Signal
        ManualResetEvents(ThreadID - 1).Set()
    End Sub

    Private Shared CurrentDataID As Int64 = 0
    Private Shared GetNextDataIDLock As New Object
    Private Shared Function GetNextDataID() As Int64
        SyncLock GetNextDataIDLock
            CurrentDataID = CurrentDataID + 1
            Return CurrentDataID
        End SyncLock
    End Function


    Private Shared Sub PrintFileLength()
        For Each PageItem In Page.Pages
            If PageItem.Value IsNot Nothing Then
                Try
                    Console.WriteLine("Page file " & PageItem.Value.ID & " Length = " & PageItem.Value.FileLength)
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

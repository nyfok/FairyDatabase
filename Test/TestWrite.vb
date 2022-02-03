Public Class TestWrite

    Private Shared WriteNumber As Int64 = 9999
    Private Shared ByteSize As Int64 = 10

    Private Shared WriteBytes(ByteSize - 1) As Byte
    Private Shared WriteBytesHash As String

    Public Shared Sub Start()
        'Generate WriteBytes
        Randomize()
        For I = 0 To ByteSize - 1
            Dim Number As Single = Rnd()
            WriteBytes(I) = Int(Number * 256)
        Next
        WriteBytesHash = GetBytesHash(WriteBytes)

        'Init RandomIDs
        PrepareRandomIDs()

        'Test Single Thread
        For TestNumber = 1 To 50
            'TestWriteInSingleThread(False)
            'Console.ReadLine()
            TestWriteInSingleThread(True)
            Console.ReadLine()
        Next

        'Test Multiple Threads
        For ThreadNumber = 2 To 16
            TestWriteInMultipleThreads(ThreadNumber, False)
            Console.ReadLine()
            'TestWriteInMultipleThreads(ThreadNumber, True)
            'Console.ReadLine()
        Next
    End Sub


#Region "Test Write"
    Private Shared Sub TestWriteInSingleThread(ByVal IfRandomRead As Boolean)
        'Clear Resources
        Clear()
        Console.WriteLine("Related resource cleared.")

        'Init Parameters
        CurrentDataID = 0
        NextDataIDs = New HashSet(Of Int64)
        NextDataIDs.UnionWith(RandomIDs)

        Dim StartTime As DateTime = Now

        'Exectue
        Do While True
            Dim DataID As Int64 = GetNextDataID(IfRandomRead)
            If DataID <= 0 Then Exit Do

            Dim FData As New Data(DataID, WriteBytes)
            Page.Write(FData)

            'Console.WriteLine("Write " & DataID & " ok.")
        Loop

        Page.FlushAll()

        'Output Result
        Dim MSeconds As Decimal = Now.Subtract(StartTime).TotalMilliseconds
        Dim WriteSpeed As Decimal = WriteNumber * WriteBytes.Length / 1024 / 1024 / MSeconds * 1000
        WriteSpeed = Int(WriteSpeed * 1000) / 1000
        Dim WriteCopySpeed As Decimal = WriteNumber / MSeconds * 1000
        WriteCopySpeed = Int(WriteCopySpeed)

        Dim WriteWayString As String
        If IfRandomRead Then
            WriteWayString = "Random"
        Else
            WriteWayString = "Sequency"
        End If
        Console.WriteLine(WriteWayString & " write via single thread using " & MSeconds & "ms. (ByteSize=" & WriteBytes.Length & ", Copies=" & WriteNumber & ", WriteCopySpeed=" & WriteCopySpeed & "Copy/s, WriteSpeed=" & WriteSpeed & "MB/s)")

        PrintFileLength()
        CheckDataCorrectRate()
    End Sub

    Private Shared ManualResetEvents As Threading.ManualResetEvent()
    Private Shared TestWriteInMultipleThreads_IfRandomRead As Boolean

    Private Shared Sub TestWriteInMultipleThreads(ByVal ThreadNumber As Integer, ByVal IfRandomRead As Boolean)
        'Clear Resources
        Clear()
        Console.WriteLine("Related resource cleared.")

        'Init Parameters
        ReDim ManualResetEvents(ThreadNumber - 1)
        TestWriteInMultipleThreads_IfRandomRead = IfRandomRead

        CurrentDataID = 0
        NextDataIDs = New HashSet(Of Int64)
        NextDataIDs.UnionWith(RandomIDs)

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

        Dim WriteWayString As String
        If IfRandomRead Then
            WriteWayString = "Random"
        Else
            WriteWayString = "Sequency"
        End If
        Console.WriteLine(WriteWayString & " write via " & ThreadNumber & " threads using " & MSeconds & "ms. (ByteSize=" & WriteBytes.Length & ", Copies=" & WriteNumber & ", WriteCopySpeed=" & WriteCopySpeed & "Copy/s, WriteSpeed=" & WriteSpeed & "MB/s)")

        PrintFileLength()
        CheckDataCorrectRate()
    End Sub

    Private Shared Sub TestWriteInMultipleThreadsDO(ByVal ThreadID As Integer)

        'Execute
        Do While True
            Dim DataID As Int64 = GetNextDataID(TestWriteInMultipleThreads_IfRandomRead)
            If DataID <= 0 Then Exit Do

            Dim FData As New Data(DataID, WriteBytes)
            Page.Write(FData)

            'Console.WriteLine("(" & ThreadID & ") Write " & DataID & " ok.")
        Loop

        'Set Signal
        ManualResetEvents(ThreadID - 1).Set()
    End Sub

#End Region

#Region "RandomIDs, GetNextDataID"
    Private Shared RandomIDs As HashSet(Of Int64)

    Private Shared Sub PrepareRandomIDs()
        Randomize()

        Dim RemainIDs As New HashSet(Of Int64)
        For I = 1 To WriteNumber
            RemainIDs.Add(I)
        Next

        RandomIDs = New HashSet(Of Int64)
        Do While RemainIDs.Count > 0
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
                If CurrentDataID <= WriteNumber Then
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
            Dim FData As Data = Page.Read(DataID)

            If FData Is Nothing OrElse FData.Value Is Nothing Then
                ErrorCount = ErrorCount + 1
                Continue For
            End If

            Dim BytesHash As String = GetBytesHash(FData.Value)
            If WriteBytesHash <> BytesHash Then
                ErrorCount = ErrorCount + 1
            End If
        Next

        If ErrorCount = 0 Then
            Console.WriteLine("All data correct! (Number=" & WriteNumber & ", CorrectRate=" & (WriteNumber - ErrorCount) * 100 / WriteNumber & "%)")
        Else
            Console.WriteLine(ErrorCount & " datas are error! (Number=" & WriteNumber & ", CorrectRate=" & (WriteNumber - ErrorCount) * 100 / WriteNumber & "%)")
        End If
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

#End Region


#Region "MD5"

    Private Shared MD5 As Security.Cryptography.MD5 = Security.Cryptography.MD5.Create
    Private Shared Function GetBytesHash(ByRef FBytes() As Byte) As String
        Dim FStream As New System.IO.MemoryStream(FBytes)
        Return GetStreamHash(FStream)
    End Function
    Private Shared Function GetStreamHash(ByRef FStream As System.IO.Stream) As String
        Dim MD5Bytes() As Byte = MD5.ComputeHash(FStream)

        Dim MD5String As New System.Text.StringBuilder
        For Each FByte In MD5Bytes
            MD5String.Append(String.Format("{0:X2}", FByte))
        Next

        Return MD5String.ToString
    End Function

#End Region

End Class

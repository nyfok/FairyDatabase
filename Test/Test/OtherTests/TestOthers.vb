Imports FairyDatabase

Public Class TestOthers

    Public Shared Sub Start()

        Select Case 8
            Case 1
                TestSharedMemory()
            Case 2
                TestArrayObjectPerformance()
            Case 3
                Dim FDictionary As New Dictionary(Of Integer, Integer)
                FDictionary.Add(3001, 1)
                FDictionary.Add(3002, 5)
                FDictionary.Add(3003, 3)
                FDictionary.Add(4003, 2)
                FDictionary.Add(5003, 3)

                Dim SortedItems = From KeyValuePair In FDictionary Select ID = KeyValuePair.Key, Score = KeyValuePair.Value Where ID > 4000 And Score < 1
                Console.WriteLine(SortedItems Is Nothing)
                Console.WriteLine(SortedItems.Count)
                For Each Item In SortedItems
                    Console.WriteLine("ID: " & Item.ID)
                Next


                Dim FHashset As New HashSet(Of Integer)
                FHashset.Add(3001)
                FHashset.Add(3002)
                FHashset.Add(3003)
                FHashset.Add(4003)
                FHashset.Add(5003)

                Dim SortedItems2 = From Item In FHashset Where Item > 4000
                Console.WriteLine(SortedItems2 Is Nothing)
                Console.WriteLine(SortedItems2.Count)
                For Each Item In SortedItems2
                    Console.WriteLine("ID: " & Item)
                Next

            Case 4
                Randomize()

                For I = 1 To 100
                    Dim IfNewCreate As Boolean = False
                    Dim FSM As New SharedMemory("Test", 4, IfNewCreate)
                    Console.WriteLine(Now.ToString & ": IfNewCreate=" & IfNewCreate)

                    If IfNewCreate Then
                        Dim FBytes As Byte() = FSM.Read()
                        Console.WriteLine(Now.ToString & ": Read from memroy: " & FBytes(0) & "," & FBytes(1) & "," & FBytes(2) & "," & FBytes(3))

                        ReDim FBytes(3)
                        For J = 0 To FBytes.Length - 1
                            FBytes(J) = Rnd() * 255
                        Next

                        FSM.Write(FBytes)
                        Console.WriteLine(Now.ToString & ": Write to memory")
                    Else
                        Dim FBytes As Byte() = FSM.Read()
                        Console.WriteLine(Now.ToString & ": Read from memroy: " & FBytes(0) & "," & FBytes(1) & "," & FBytes(2) & "," & FBytes(3))

                        If I = 10 Then
                            FSM.Dispose()
                            Console.WriteLine(Now.ToString & ": Sharedmemory disposed.")
                        End If
                    End If

                    Console.ReadLine()
                Next

            Case 5
                'Test MutexACL Dispose
                Dim FMutex As New MutexACL("Global\TestMutex")

                Dim Max As Integer = Now.Second Mod 10
                For I = 1 To Max
                    Console.WriteLine("Waiting " & I & " of " & Max)
                    FMutex.WaitOne()
                    Console.WriteLine("Got " & I & ". Press any key to release.")
                    Console.ReadLine()
                    FMutex.Release()
                Next

                Console.WriteLine("Pending Dispose. Press any key to continue.")
                Console.ReadLine()
                FMutex.Dispose()

            Case 6
                For i = 1 To 1000000
                    Dim FMutex As New MutexACL("Global\TestMutex" & i)
                Next
                Console.WriteLine("created 100000 mutex.")

            Case 7
                Console.WriteLine(CType(3.6, Integer))
                Dim num(7) As Integer

                ReDim Preserve num(10)

                'FariyDatabaseCSharp.Test.Start()

            Case 8
                FlushAllTest()

        End Select
    End Sub


    Private Shared Sub TestSharedMemory()
        For ThreadID = 1 To 10
            System.Threading.ThreadPool.QueueUserWorkItem(New System.Threading.WaitCallback(AddressOf TestSharedMemoryDo), ThreadID)
        Next
    End Sub

    Private Shared Sub TestSharedMemoryDo(ByVal ThreadID As Integer)
        Dim FBytes(3) As Byte
        For I = 0 To FBytes.Length - 1
            FBytes(I) = I
        Next

        Dim FSM As New SharedMemory("Test", 100)
        For i = 1 To 100
            FSM.Write(FBytes)
            Dim FBytes2 As Byte() = FSM.Read(0)
            Console.WriteLine(FBytes2(2))
        Next
    End Sub

    Private Sub TestRead(ByVal ThreadID As Integer)
        'Add Database Connection
        Dim DatabaseName As String = "TestDB"
        Dim Config As New FairyDatabase.DatabaseConfig(DatabaseName)
        Dim Database As New FairyDatabase.Database(Config)

        Dim TableName As String = "TestTable"

        For i = 1 To 10
            Console.WriteLine(ThreadID & ": " & i)

            Dim DataID As Int64 = i
            Dim fdata As Data = Database.Read(TableName, DataID)

            If fdata IsNot Nothing And fdata.Value IsNot Nothing Then
                System.IO.File.WriteAllBytes("temp/" & ThreadID & "-" & i & ".zip", fdata.Value)
            End If

        Next
    End Sub



    Private Shared Sub TestArrayObjectPerformance()
        'Test Insert Speed via Multiple ways
        'Init Parameters
        Dim StartTime As DateTime
        Dim MSeconds As Decimal

        Const TestNumber As Integer = 100000

        For LoopTimes = 1 To 5
            'List
            StartTime = Now
            Dim FList As New List(Of Byte)
            For I = 1 To TestNumber
                FList.Add(0)
            Next
            MSeconds = Now.Subtract(StartTime).TotalMilliseconds
            Console.WriteLine("List using " & MSeconds & "ms.             " & vbTab & "(Process: " & Int(TestNumber / MSeconds) & " Number/ms)")

            'ArrayList
            StartTime = Now
            Dim FArrayList As New ArrayList
            For I = 1 To TestNumber : FArrayList.Add(0) : Next
            MSeconds = Now.Subtract(StartTime).TotalMilliseconds
            Console.WriteLine("ArrayList using " & MSeconds & "ms.        " & vbTab & "(Process: " & Int(TestNumber / MSeconds) & " /ms)")

            'Redim
            StartTime = Now
            Dim FRedim() As Byte
            For I = 1 To TestNumber : ReDim FRedim(I - 1) : FRedim(I - 1) = 0 : Next
            MSeconds = Now.Subtract(StartTime).TotalMilliseconds
            Console.WriteLine("ReDim using " & MSeconds & "ms.            " & vbTab & "(Process: " & Int(TestNumber / MSeconds) & " /ms)")

            'Redim Preserve
            StartTime = Now
            Dim FRedimPerserve() As Byte
            For I = 1 To TestNumber : ReDim Preserve FRedimPerserve(I - 1) : FRedimPerserve(I - 1) = 0 : Next
            MSeconds = Now.Subtract(StartTime).TotalMilliseconds
            Console.WriteLine("ReDim Preserve using " & MSeconds & "ms.   " & vbTab & "(Process: " & Int(TestNumber / MSeconds) & " /ms)")

            'Query
            StartTime = Now
            Dim FQuery As New Queue(Of Byte)
            For I = 1 To TestNumber : FQuery.Enqueue(0) : Next
            MSeconds = Now.Subtract(StartTime).TotalMilliseconds
            Console.WriteLine("Queue using " & MSeconds & "ms.            " & vbTab & "(Process: " & Int(TestNumber / MSeconds) & " /ms)")

            'Convert arrary to list
            StartTime = Now
            Dim FList2 As New List(Of Byte)
            FList2.AddRange(FRedim)
            MSeconds = Now.Subtract(StartTime).TotalMilliseconds
            Console.WriteLine("Convert arrary to list using " & MSeconds & "ms.            " & vbTab & "(Process: " & Int(TestNumber / MSeconds) & " /ms, ListLength=" & FList2.Count & ")")

            'Convert list to array
            StartTime = Now
            Dim FRedim2 As Byte()
            FRedim2 = FList2.ToArray
            MSeconds = Now.Subtract(StartTime).TotalMilliseconds
            Console.WriteLine("Convert list to array using " & MSeconds & "ms.            " & vbTab & "(Process: " & Int(TestNumber / MSeconds) & " /ms, ArrayLength=" & FRedim2.Count & ")")

            ''Convert arrary to list
            'ReDim FRedim2(1024)
            'StartTime = Now
            'FList2 = New List(Of Byte)
            'For I = 1 To TestNumber
            '    FList2.AddRange(FRedim)
            'Next
            'MSeconds = Now.Subtract(StartTime).TotalMilliseconds
            'Console.WriteLine("Convert arrary to list using " & MSeconds & "ms.            " & vbTab & "(Process: " & Int(TestNumber / MSeconds) & " /ms, ListLength=" & FList2.Count & ")")

            ''Convert list to array
            'StartTime = Now
            'FRedim2 = FList2.ToArray
            'MSeconds = Now.Subtract(StartTime).TotalMilliseconds
            'Console.WriteLine("Convert list to array using " & MSeconds & "ms.            " & vbTab & "(Process: " & Int(TestNumber / MSeconds) & " /ms, ArrayLength=" & FRedim2.Count & ")")

            ''List.Count
            'StartTime = Now
            'For I = 1 To TestNumber
            '    Dim a As Integer = FList2.Count
            'Next
            'MSeconds = Now.Subtract(StartTime).TotalMilliseconds
            'Console.WriteLine("List.Count using " & MSeconds & "ms.            " & vbTab & "(Process: " & Int(TestNumber / MSeconds) & " /ms, ListLength=" & FList2.Count & ")")

            'Write line
            Console.WriteLine()
        Next
    End Sub


#Region "Hotfix - Flush all test"

    Public Shared Sub FlushAllTest()
        FairyDatabase.Config.Init()
        FairyDatabase.Config.IfDebugMode = False
        Console.WriteLine("Start")
        'Dim temp As New Page(100000000)
        For i = 0 To 5
            Dim newThread As System.Threading.Thread = New System.Threading.Thread(New System.Threading.ThreadStart(AddressOf FlushAllTestThread))
            newThread.Name = i
            newThread.Start()
        Next
    End Sub

    Private Shared Sub FlushAllTestThread()
        Dim random As Random = New Random()

        For DataID As Int64 = (Int64.Parse(System.Threading.Thread.CurrentThread.Name) * 1000) + 1 To (Int64.Parse(System.Threading.Thread.CurrentThread.Name) * 1000) + 100
            'long randnumber = (random.NextDouble() * 100);
            'Console.WriteLine("Thread")
            Dim identifier As String = "thread " & System.Threading.Thread.CurrentThread.Name & " at " & Date.Now.ToFileTimeUtc()
            Console.WriteLine("DataID=" & DataID)

            Dim FData As New Data(DataID)
            Console.WriteLine(identifier & " going to write """ & identifier & """ at " & DataID)
            'Console.WriteLine("bytes are " & System.Text.Encoding.UTF8.GetBytes(identifier).Length)
            FData.Value = System.Text.Encoding.UTF8.GetBytes(identifier)

            Page.Write(FData)
            Page.FlushAll()
            Console.WriteLine(identifier & " is about to read " & DataID)
            Dim bytes As Byte() = Page.Read(DataID).Value()
            Console.Write("Length is " & bytes.Length)
            Console.WriteLine(identifier & " found """ & Text.Encoding.UTF8.GetString(bytes) & """")
        Next
    End Sub

#End Region
End Class

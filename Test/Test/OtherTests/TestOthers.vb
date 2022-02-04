Imports FairyDatabase

Public Class TestOthers

    Public Shared Sub Start()

        Select Case 3
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
        End Select
    End Sub


    Private Sub TestRead(ByVal ThreadID As Integer)

        For i = 1 To 10
            Console.WriteLine(ThreadID & ": " & i)

            Dim DataID As Int64 = i
            Dim fdata As Data = Page.Read(DataID)

            If fdata IsNot Nothing And fdata.Value IsNot Nothing Then
                System.IO.File.WriteAllBytes("temp/" & ThreadID & "-" & i & ".zip", fdata.Value)
            End If

        Next
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

End Class

Public Class TestOthers

    Public Shared Sub Start()

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


    Private Sub TestSharedMemory(ByVal ThreadID As Integer)
        Dim FBytes(3) As Byte
        For I = 0 To FBytes.Length - 1
            FBytes(I) = I
        Next

        Dim FSM As New SharedMemory("Test", 100)
        For i = 1 To 100
            FSM.Write(FBytes)
            Dim FBytes2 As Byte() = FSM.Read(97)
            Console.WriteLine(FBytes2(2))
        Next
    End Sub


End Class

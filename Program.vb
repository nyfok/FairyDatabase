Imports System
Imports System.IO
Imports System.Security.AccessControl
Imports System.Threading

Module Program
    Sub Main(args As String())
        'Dim fdata As New Data
        'fdata.ID = 281474976710655
        'fdata.StartPOS = 92233720368547758
        'fdata.Length = 281474976710655

        'Dim fbytes As Byte() = fdata.IndexByte
        'Console.WriteLine("fbytes length: " & fbytes.Length)
        'For Each fbyte In fbytes
        '    Console.WriteLine(fbyte)
        'Next

        'Dim fdata2 As New Data
        'fdata2.IndexByte = fdata.IndexByte

        'Console.WriteLine(fdata2.ID)
        'Console.WriteLine(fdata2.StartPOS)
        'Console.WriteLine(fdata2.Length)

        'Console.WriteLine(DataPage.GetDataIndexPOS(33))

        TestMultipleThread()
        'MutexACL.Test2()


        Console.WriteLine("finished")
        Console.ReadLine()
    End Sub

    Private Sub Test()
        'code here will be executed when the time elapse

        Console.WriteLine("TEST!!!")
    End Sub

    Private Sub TestMultipleThread()

        For ThreadID = 1 To 10
            System.Threading.ThreadPool.QueueUserWorkItem(New System.Threading.WaitCallback(AddressOf TesWrite), ThreadID)

        Next

    End Sub

    Private Sub TesWrite(ByVal ThreadID As Integer)

        Dim fbytes As Byte() = System.IO.File.ReadAllBytes("temp/pictures.zip")

        For i = 1 To 50
            Console.WriteLine(ThreadID & ": " & i)

            Dim fdata As New Data(i, fbytes)
            Page.Write(fdata)

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




End Module

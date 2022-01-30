Imports System

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

        TestMultipleThreadWrite()

        Console.WriteLine("finished")
        Console.ReadLine()
    End Sub

    Private Sub TestMultipleThreadWrite()

        For ThreadID = 1 To 10
            System.Threading.ThreadPool.QueueUserWorkItem(New System.Threading.WaitCallback(AddressOf TestMultipleThreadWriteDO), ThreadID)
        Next

    End Sub

    Private Sub TestMultipleThreadWriteDO(ByVal ThreadID As Integer)

        Dim fbytes As Byte() = System.IO.File.ReadAllBytes("temp/pictures.zip")

        For i = 1 To 30
            Console.WriteLine(ThreadID & ": " & i)

            Dim fdata As New Data(i, fbytes)
            DataPage.WriteData(fdata)

        Next

    End Sub


End Module

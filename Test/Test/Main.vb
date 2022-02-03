Imports System
Imports System.IO
Imports System.Security.AccessControl
Imports System.Threading

Module Main

    Sub Main(args As String())

        'Execute Test
        Select Case 1
            Case 1
                WritePerformanceTest.Start()
            Case 2
                ReadPerformanceTest.Start()
            Case 3
                TestOthers.Start()

        End Select

        'Press Any Key to exit
        Console.WriteLine("Press any key to exit.")
        Console.ReadLine()
    End Sub

End Module

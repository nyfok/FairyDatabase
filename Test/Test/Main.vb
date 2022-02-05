Imports System
Imports System.IO
Imports System.Security.AccessControl
Imports System.Threading

Module Main

    Sub Main(args As String())
        'Add Log
        Console.WriteLine("Program start.")

        'Execute Test
        Select Case 1
            Case 1
                Demo.Start()
            Case 2
                WritePerformanceTest.Start()
            Case 3
                ReadPerformanceTest.Start()
            Case 4
                TestOthers.Start()

        End Select

        'Press Any Key to exit
        Console.WriteLine("Program end. Press any key to exit.")
        Console.ReadLine()
    End Sub

End Module

Imports System
Imports System.IO
Imports System.Security.AccessControl
Imports System.Threading

Module Program

    Sub Main(args As String())

        'Execute Test
        Select Case 2
            Case 1
                TestWrite.Start()
            Case 2
                TestRead.Start()
            Case 3
                TestOthers.Start()
        End Select

        'Press Any Key to exit
        Console.WriteLine("Press any key to exit.")
        Console.ReadLine()
    End Sub








End Module

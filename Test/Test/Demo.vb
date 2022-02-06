Imports FairyDatabase

Public Class Demo

    Public Shared Sub Start()
        'Init FairyDatabase 
        FairyDatabase.Config.Init()
        FairyDatabase.Config.IfDebugMode = False

        'Write Data
        For DataID As Int64 = 1 To 3
            Dim FData As New Data(DataID)
            FData.Value = System.Text.Encoding.UTF8.GetBytes("Hello World " & DataID & ". (" & Now.ToString & ")")
            Page.Write(FData)
        Next

        'Flush All Data
        Page.FlushAll()

        'Read Data
        For DataID As Int64 = 1 To 3
            Dim FData As Data = Page.Read(DataID)
            If FData IsNot Nothing Then
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(FData.Value))
            End If
        Next
    End Sub

End Class

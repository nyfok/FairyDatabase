Imports FairyDatabase

Public Class Demo

    Public Shared Sub Start()
        'Set Debug Mode for Test
        FairyDatabase.Settings.IfDebugMode = False

        'Get Database
        Dim DatabaseName As String = "TestDB"
        Dim Config As New DatabaseConfig(DatabaseName)
        Dim Database As New FairyDatabase.Database(Config)

        'Write Data to Table
        Dim TableName As String = "TestTable"

        For DataID As Int64 = 1 To 3
            Dim FData As New Data(DataID)
            FData.Value = System.Text.Encoding.UTF8.GetBytes("Hello World " & DataID & ". (" & Now.ToString & ")")

            Database.Write(TableName, FData)
        Next

        'Flush All Data
        Database.FlushTable(TableName)

        'Read Data
        For DataID As Int64 = 1 To 3
            Dim FData As Data = Database.Read(TableName, DataID)
            If FData IsNot Nothing Then
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(FData.Value))
            End If
        Next
    End Sub

End Class

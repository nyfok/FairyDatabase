Public Class DatabaseOperation

    Public DatabaseConfig As DatabaseConfig

    Public Sub New(ByRef DatabaseConfig As DatabaseConfig)
        'Set Parameters
        Me.DatabaseConfig = DatabaseConfig

        'Create Table Folder if not exists
        Dim FolderPath As String = DatabaseConfig.DatabaseFolderPath
        If System.IO.Directory.Exists(FolderPath) = False Then
            System.IO.Directory.CreateDirectory(FolderPath)
        End If
    End Sub

    Public Tables As New Concurrent.ConcurrentDictionary(Of String, Table)
    Private CreateTableLock As New Object

    Public Sub Write(ByVal TableName As String, ByVal FData As Data)
        'Format Input
        If String.IsNullOrWhiteSpace(TableName) Then Return
        TableName = TableName.Trim.ToLower

        'Get Table
        Dim FTable As Table = GetTable(TableName)

        'Execute
        FTable.Write(FData)
    End Sub

    Public Function Read(ByVal TableName As String, ByVal DataID As Int64) As Data
        'Format Input
        If String.IsNullOrWhiteSpace(TableName) Then Return Nothing
        TableName = TableName.Trim.ToLower

        'Get Table
        Dim FTable As Table = GetTable(TableName)

        'Execute
        Return FTable.Read(DataID)
    End Function

    Public Function GetTable(ByVal TableName As String) As Table
        'Format Input
        If String.IsNullOrWhiteSpace(TableName) Then Return Nothing
        TableName = TableName.Trim.ToLower

        'Get Table
        If Tables.ContainsKey(TableName) Then Return Tables(TableName)

        SyncLock CreateTableLock
            If Tables.ContainsKey(TableName) Then Return Tables(TableName)

            Dim FTable As New Table(TableName, DatabaseConfig)
            Tables.TryAdd(TableName, FTable)

            Return FTable
        End SyncLock

    End Function

    Public Sub FlushTable(ByVal TableName As String)
        'Get Table
        Dim FTable As Table = GetTable(TableName)
        If FTable Is Nothing Then Return

        'Execute
        FTable.Flush()
    End Sub

End Class

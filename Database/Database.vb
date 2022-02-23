Public Class Database

    Public DatabaseConfig As DatabaseConfig
    Private DatabaseOperation As DatabaseOperation

    Public Sub New(ByVal DatabaseConfig As DatabaseConfig)
        'Check input
        If DatabaseConfig Is Nothing Then
            Throw New Exception("DatabaseConfig is nothing.")
            Return
        End If

        'Set DatabaseConfig
        Me.DatabaseConfig = DatabaseConfig

        'Get DatabaseOperation
        DatabaseOperation = GetDatabaseOperation(DatabaseConfig)
    End Sub

#Region "Database Operations"

    Public Sub Write(ByVal TableName As String, ByVal FData As Data)
        If DatabaseOperation Is Nothing Then Return
        DatabaseOperation.Write(TableName, FData)
    End Sub

    Public Function Read(ByVal TableName As String, ByVal DataID As Int64) As Data
        If DatabaseOperation Is Nothing Then Return Nothing
        Return DatabaseOperation.Read(TableName, DataID)
    End Function

    Public Function GetTable(ByVal TableName As String) As Table
        If DatabaseOperation Is Nothing Then Return Nothing
        Return DatabaseOperation.GetTable(TableName)
    End Function

    Public Sub FlushTable(ByVal TableName As String)
        If DatabaseOperation Is Nothing Then Return
        DatabaseOperation.FlushTable(TableName)
    End Sub

#End Region

#Region "Shared Functions"

    Private Shared DatabaseOperations As New Concurrent.ConcurrentDictionary(Of String, DatabaseOperation)   'Key=DatabaseKey
    Private Shared AddDatabaseOperationLock As New Object

    Private Shared Function GetDatabaseOperation(ByRef DatabaseConfig As DatabaseConfig) As DatabaseOperation
        'Check Input
        If DatabaseConfig Is Nothing Then Return Nothing

        'Get Key
        Dim Key As String = DatabaseConfig.DatabaseKey

        'Execute
        If DatabaseOperations.ContainsKey(Key) Then Return DatabaseOperations(Key)

        SyncLock AddDatabaseOperationLock
            If DatabaseOperations.ContainsKey(Key) Then Return DatabaseOperations(Key)

            Dim FDatabaseOperation As New DatabaseOperation(DatabaseConfig)
            DatabaseOperations.TryAdd(Key, FDatabaseOperation)

            Return FDatabaseOperation
        End SyncLock
    End Function

#End Region

End Class

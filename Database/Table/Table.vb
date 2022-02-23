Public Class Table
    Implements IDisposable

    Public Name As String
    Public DatabaseConfig As DatabaseConfig

    Public Sub New(ByVal Name As String, ByRef DatabaseConfig As DatabaseConfig)
        Me.Name = Name
        Me.DatabaseConfig = DatabaseConfig

        'Create Table Folder if not exists
        Dim FolderPath As String = Me.TableFolderPath
        If System.IO.Directory.Exists(FolderPath) = False Then
            System.IO.Directory.CreateDirectory(FolderPath)
        End If
    End Sub


    Public ReadOnly Property TableFolderPath() As String
        Get
            Return DatabaseConfig.DatabaseFolderPath & Name
        End Get
    End Property


#Region "Page Operate Functions"

    Public Pages As New Concurrent.ConcurrentDictionary(Of Int64, Page)
    Private CreatePageLock As New Object

    Public Sub Write(ByVal FData As Data)
        'Get Page
        Dim FPage As Page = GetPage(FData.ID)

        'Execute
        FPage.WriteData(FData)
    End Sub

    Public Function Read(ByVal DataID As Int64) As Data
        'Get Page
        Dim FPage As Page = GetPage(DataID)

        'Execute
        Return FPage.ReadData(DataID)
    End Function

    Public Sub Flush()
        For Each PageItem In Pages
            If PageItem.Value IsNot Nothing Then
                Try
                    PageItem.Value.Flush()
                Catch ex As Exception
                End Try
            End If
        Next
    End Sub

    Public Function GetPageID(ByVal DataID As Int64) As Int64
        Return Int(DataID / DatabaseConfig.DataPageSize)
    End Function

    Public Function GetPage(ByVal DataID As Int64) As Page
        'Get Page ID
        Dim PageID As Int64 = GetPageID(DataID)

        'Get Page
        If Pages.ContainsKey(PageID) Then Return Pages(PageID)

        SyncLock CreatePageLock
            If Pages.ContainsKey(PageID) Then Return Pages(PageID)

            Dim FPage As New Page(Name, PageID, DatabaseConfig)
            Pages.TryAdd(PageID, FPage)

            Return FPage
        End SyncLock

    End Function


#End Region

#Region "Dispose"

    Private disposedValue As Boolean

    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                ' TODO: 释放托管状态(托管对象)
            End If

            ' TODO: 释放未托管的资源(未托管的对象)并重写终结器
            ' TODO: 将大型字段设置为 null

            'Flush
            Flush()

            'Clear Pages
            Pages = New Concurrent.ConcurrentDictionary(Of Int64, Page)

            'Set disposedValue
            disposedValue = True
        End If
    End Sub

    ' ' TODO: 仅当“Dispose(disposing As Boolean)”拥有用于释放未托管资源的代码时才替代终结器
    ' Protected Overrides Sub Finalize()
    '     ' 不要更改此代码。请将清理代码放入“Dispose(disposing As Boolean)”方法中
    '     Dispose(disposing:=False)
    '     MyBase.Finalize()
    ' End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' 不要更改此代码。请将清理代码放入“Dispose(disposing As Boolean)”方法中
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub

#End Region

End Class

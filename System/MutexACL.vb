Imports System.Security.AccessControl
Imports System.Threading

Public Class MutexACL
    Implements IDisposable

    Public Name As String
    Public FMutex As Mutex

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="Name">
    ''' 如果其名称以前缀 "Global\" 开头，则 mutex 在所有终端服务器会话中可见。如：Global\Test
    ''' 如果其名称以前缀 "Local\" 开头，则 mutex 仅在创建它的终端服务器会话中可见。 在这种情况下，可以在服务器上的其他每个终端服务器会话中存在具有相同名称的单独 mutex。如：Local\Test
    ''' 系统默认采用前缀 "Local\
    ''' </param>
    Public Sub New(ByVal Name As String)
        'Set Parameter
        Me.Name = Name
    End Sub

    Public Sub WaitOne()
        'Get Mutex
        Dim IfCreateNewMutex As Boolean = False

        If FMutex Is Nothing Then
            Try
                'Open Existing
                Dim FMutexRights As System.Security.AccessControl.MutexRights = MutexRights.FullControl
                FMutex = Threading.MutexAcl.OpenExisting(Name, FMutexRights)
            Catch ex As Threading.WaitHandleCannotBeOpenedException
                'Console.WriteLine("Mutex does not exist.")

                Dim FMutexSecurity As New System.Security.AccessControl.MutexSecurity
                Dim FMutexAccessRule As MutexAccessRule
                Dim User As String = "everyone"
                FMutexAccessRule = New MutexAccessRule(User, MutexRights.FullControl, AccessControlType.Allow)
                FMutexSecurity.AddAccessRule(FMutexAccessRule)

                'Create Mutex
                FMutex = Threading.MutexAcl.Create(True, Name, IfCreateNewMutex, FMutexSecurity)
                'Console.WriteLine("IfCreateNewMutex: " & IfCreateNewMutex)

            Catch ex As UnauthorizedAccessException
                'Console.WriteLine("Unauthorized access: {0}", ex.Message)
                Throw ex
                Return
            End Try
        End If

        'Check IfCreateNewMutex
        If IfCreateNewMutex Then Return

        'Wait Signal
        Try
            FMutex.WaitOne()
        Catch ex As System.Threading.AbandonedMutexException
            'Console.WriteLine("Mutex wait error: {0}", ex.Message)
        Catch ex As Exception
            'Console.WriteLine("Mutex wait error: {0}", ex.Message)
            Throw ex
            Return
        End Try

    End Sub

    Public Sub Release()
        If FMutex Is Nothing Then Return
        FMutex.ReleaseMutex()
    End Sub


#Region "Test"

    Public Shared Sub Test()
        For ThreadID = 1 To 200
            System.Threading.ThreadPool.QueueUserWorkItem(New System.Threading.WaitCallback(AddressOf TestMutexACL), ThreadID)
        Next
    End Sub

    Private Shared Sub TestMutexACL(ByVal ThreadID As Integer)

        'Create FMutex
        Dim FMutex As New MutexACL("Global\test")
        'Dim FMutex As New MutexACL("Local\test")

        'Wait Mutex
        FMutex.WaitOne()

        'Execute
        Console.WriteLine(Now.ToString & ": Thread " & ThreadID & " Start.")
        Threading.Thread.Sleep(2000)
        Console.WriteLine(Now.ToString & ": Thread " & ThreadID & " End.")

        'Release
        FMutex.Release()

    End Sub


#Region "Test2"

    Private Shared TestMutex As MutexACL

    Public Shared Sub Test2()
        TestMutex = New MutexACL("Global\test2")

        For ThreadID = 1 To 200
            System.Threading.ThreadPool.QueueUserWorkItem(New System.Threading.WaitCallback(AddressOf TestMutexACL2), ThreadID)
        Next
    End Sub

    Private Shared Sub TestMutexACL2(ByVal ThreadID As Integer)

        'Wait Mutex
        TestMutex.WaitOne()

        'Execute
        Console.WriteLine(Now.ToString & ": Thread " & ThreadID & " Start.")
        Threading.Thread.Sleep(2000)
        Console.WriteLine(Now.ToString & ": Thread " & ThreadID & " End.")

        'Release
        TestMutex.Release()

    End Sub

#End Region



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
            If FMutex IsNot Nothing Then
                FMutex.Close()
                FMutex.Dispose()
                FMutex = Nothing
            End If

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

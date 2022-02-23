Imports System.IO
Imports System.Threading
Public Class FileBuffer
    Implements IDisposable

    Private FilePath As String
    Private FileBuffer As FileBufferWriter

    Public StartPosition As Int64
    Public ByteList As New List(Of Byte)
    Public CreateTime As DateTime  'First time when data add to buffer

    Private FlushTimer As Timer = Nothing
    Private FlushMSeconds As Integer

    Private IfStartFlush As Boolean
    Private IfEnd As Boolean


    Public Sub New(ByVal FilePath As String, ByRef FileBuffer As FileBufferWriter, ByVal FlushMSeconds As Integer, ByVal StartPosition As Int64, Optional ByVal Bytes As Byte() = Nothing)
        'Init Parameters
        Me.FilePath = FilePath
        Me.FileBuffer = FileBuffer
        Me.FlushMSeconds = FlushMSeconds
        Me.StartPosition = StartPosition
        If Bytes IsNot Nothing Then Me.ByteList.AddRange(Bytes)
        Me.CreateTime = Now
        Me.IfStartFlush = False
        Me.IfEnd = False

        'Create timer
        Dim FTimerCallback As TimerCallback = AddressOf FlushWithLock
        FlushTimer = New Timer(FTimerCallback, Nothing, FlushMSeconds, -1)
    End Sub

#Region "Properties - Length, EndPosition"
    Public ReadOnly Property Length As Int64
        Get
            Return ByteList.Count
        End Get
    End Property
    Public ReadOnly Property EndPosition As Int64
        Get
            Return StartPosition + ByteList.Count - 1
        End Get
    End Property

#End Region

#Region "Write"

    Private WriteLock As New Object

    Public Sub Write(ByRef FStream As FileStream, ByVal Position As Int64, ByVal Bytes As Byte())
        'Check input
        If Bytes Is Nothing OrElse Bytes.Count = 0 Then Return

        'Check Position
        If Position < StartPosition OrElse Position > (EndPosition + 1) Then
            'Direct write data
            FStream.Position = Position
            FStream.Write(Bytes, 0, Bytes.Count)

            'Console.WriteLine(Now.ToString & ": Write Position B: " & Position & ", Write Length: " & Bytes.Count)
            Return
        End If

        'Sync Execute
        SyncLock WriteLock
            'Check IfStartFlush
            If IfStartFlush Then
                'Already in flush process, this Buffer not exists any more in Buffers. 
                'Direct write data
                FStream.Position = Position
                FStream.Write(Bytes, 0, Bytes.Count)

                'Console.WriteLine(Now.ToString & ": Write Position C: " & Position & ", Write Length: " & Bytes.Count)
                Return
            Else
                'Not in flush process, still waiting to be flush
                'Close the FlushTimer first
                FlushTimer.Dispose()
                FlushTimer = Nothing
            End If

            'Union Data
            If Position = EndPosition + 1 Then
                ByteList.AddRange(Bytes)
            Else
                'Remove Range First
                Dim RemoveCount As Integer = Bytes.Count
                Dim MaxRemoveCount As Integer = EndPosition - Position + 1
                If RemoveCount > MaxRemoveCount Then RemoveCount = MaxRemoveCount
                ByteList.RemoveRange(Position, RemoveCount)

                'Insert Range
                ByteList.InsertRange(Position, Bytes)
            End If

            'Flush right now or Continue Timer
            Dim RemainMSeconds As Integer = FlushMSeconds - Now.Subtract(CreateTime).TotalMilliseconds
            If RemainMSeconds < 0 Then
                'Flush right now
                FlushExecute(FStream)
            Else
                'Continue Timer
                Dim FTimerCallback As TimerCallback = AddressOf FlushWithLock
                FlushTimer = New Timer(FTimerCallback, Nothing, RemainMSeconds, -1)
            End If

            'Return
            Return
        End SyncLock

    End Sub

#End Region

#Region "Flush"

    Private Sub FlushWithLock()
        'Check and Set IfStartFlush
        If IfStartFlush Then Return
        IfStartFlush = True

        'Close FlushTimer
        If FlushTimer IsNot Nothing Then
            FlushTimer.Dispose()
            FlushTimer = Nothing
        End If

        'Wait Lock to execute
        SyncLock WriteLock
            'Execute Flush
            FlushExecute()
        End SyncLock
    End Sub

    Public Sub Flush()
        'Check and Set IfStartFlush
        If IfStartFlush Then Return
        IfStartFlush = True

        'Execute Flush
        FlushExecute()
    End Sub

    Private Sub FlushExecute(Optional ByVal FStream As FileStream = Nothing)
        'Set IfStartFlush
        IfStartFlush = True
        FileBuffer.RemoveBuffer(Me)

        'Write Buffer
        If ByteList IsNot Nothing AndAlso ByteList.Count > 0 Then
            'Check FStream
            Dim IfNewCreateStream As Boolean = False
            If FStream Is Nothing Then
                IfNewCreateStream = True
                FStream = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)
            End If

            'Write
            FStream.Position = StartPosition
            FStream.Write(ByteList.ToArray, 0, ByteList.Count)
            FStream.Flush()

            If False Then
                'Console.WriteLine(Now.ToString & ": Flush Position: " & StartPosition & ", Flush Length: " & ByteList.Count)
            End If

            'Close Stream if NewCreate
            If IfNewCreateStream Then
                FStream.Close()
                FStream.Dispose()
            End If
        End If

        'Set Signals
        IfEnd = True
    End Sub


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
            disposedValue = True

            'Flush
            Flush()
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

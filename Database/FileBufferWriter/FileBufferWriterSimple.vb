Imports System.IO
Imports System.Threading
Public Class FileBufferWriterSimple
    Implements IDisposable

    Private FilePath As String
    Private MaxBufferSize As Int64
    Private FlushMSeconds As Integer

    Public Sub New(ByVal FilePath As String, ByVal MaxBufferSize As Int64, ByVal FlushMSeconds As Integer)
        Me.FilePath = FilePath
        Me.MaxBufferSize = MaxBufferSize
        Me.FlushMSeconds = FlushMSeconds
    End Sub


    Private WriteLock As New Object
    Private FlushTimer As Timer = Nothing

    Private StartPosition As Int64 = 0
    Private Buffer As Byte()
    Private BufferLength As Int64 = 0
    Private NewBufferTime As DateTime  'First time when data add to buffer

    Public Sub Write(ByRef FStream As FileStream, ByVal Position As Int64, ByVal Bytes As Byte())
        'Check input
        If Bytes Is Nothing OrElse Bytes.Count = 0 Then Return

        'Check size, if exceed or equal then direct write and return
        If Bytes.Length >= MaxBufferSize Then
            FStream.Position = Position
            FStream.Write(Bytes, 0, Bytes.Count)
            Return
        End If

        'Sync Execute
        SyncLock WriteLock
            'Check if has buffer
            If BufferLength = 0 Then
                'Set to buffer
                StartPosition = Position
                ReDim Buffer(MaxBufferSize - 1)
                System.Buffer.BlockCopy(Bytes, 0, Buffer, 0, Bytes.Length)
                BufferLength = Bytes.Length

                'Create timer
                Dim FTimerCallback As TimerCallback = AddressOf FlushWithLock
                FlushTimer = New Timer(FTimerCallback, Nothing, FlushMSeconds, -1)
                NewBufferTime = Now

                'Return
                Return
            End If

            'Close FlushTimer
            If FlushTimer IsNot Nothing Then
                FlushTimer.Dispose()
                FlushTimer = Nothing
            End If

            'Check if continuous
            If Position = StartPosition + BufferLength Then
                'Continuous, append to buffer
                Dim NewLength As Int64 = BufferLength + Bytes.Length
                If NewLength > Buffer.Length Then
                    'Resize and Append to buffer
                    ReDim Preserve Buffer(NewLength - 1)
                    System.Buffer.BlockCopy(Bytes, 0, Buffer, BufferLength, Bytes.Length)
                    BufferLength = NewLength

                    'Flush right now
                    FlushExecute(FStream)
                    'Return
                    Return
                Else
                    'Append to buffer
                    System.Buffer.BlockCopy(Bytes, 0, Buffer, BufferLength, Bytes.Length)
                    BufferLength = NewLength

                    'Flush right now or Continue Timer
                    Dim RemainMSeconds As Integer = FlushMSeconds - Now.Subtract(NewBufferTime).TotalMilliseconds
                    If RemainMSeconds < 0 Then
                        'Flush right now
                        FlushExecute(FStream)
                        'Return
                        Return
                    Else
                        'Continue Timer
                        Dim FTimerCallback As TimerCallback = AddressOf FlushWithLock
                        FlushTimer = New Timer(FTimerCallback, Nothing, RemainMSeconds, -1)
                        'Return
                        Return
                    End If

                End If

            Else
                'Not continous, write the current buffer and set new buffer

                'Flush right now
                FlushExecute(FStream)

                'Set to buffer
                StartPosition = Position
                ReDim Buffer(MaxBufferSize - 1)
                System.Buffer.BlockCopy(Bytes, 0, Buffer, 0, Bytes.Length)
                BufferLength = Bytes.Length

                'Create timer
                Dim FTimerCallback As TimerCallback = AddressOf FlushWithLock
                FlushTimer = New Timer(FTimerCallback, Nothing, FlushMSeconds, -1)
                NewBufferTime = Now

                'Return
                Return

            End If


        End SyncLock
    End Sub

#Region "Flush"
    Private Sub FlushWithLock()
        'Close FlushTimer
        If FlushTimer IsNot Nothing Then
            FlushTimer.Dispose()
            FlushTimer = Nothing
        End If

        'Wait Lock to execute
        SyncLock WriteLock
            Flush()
        End SyncLock
    End Sub

    Private Sub Flush()
        'Check BufferLength
        If Buffer Is Nothing OrElse BufferLength <= 0 Then Return

        'Generate FStream
        Dim FStream As FileStream = File.Open(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)

        'Write
        FlushExecute(FStream)

        'Close FStream
        FStream.Flush()
        FStream.Close()
        FStream.Dispose()
    End Sub

    Private Sub FlushExecute(ByVal FStream As FileStream)
        'Write
        FStream.Position = StartPosition
        If Buffer IsNot Nothing AndAlso Buffer.Length >= BufferLength AndAlso BufferLength > 0 Then
            FStream.Write(Buffer, 0, BufferLength)
            'Console.WriteLine("Flush Position: " & StartPosition & ", Flush Length: " & BufferLength)
        End If

        'Init Parameters
        StartPosition = 0
        Buffer = Nothing
        BufferLength = 0
        NewBufferTime = Nothing
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

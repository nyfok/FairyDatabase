Imports System.IO
Imports System.Threading
Public Class FileBufferWriter
    Implements IDisposable

    Private FilePath As String
    Private MaxBufferSize As Int64
    Private FlushMSeconds As Integer

    Private Const MaxBuffersNumber As Integer = 16000

    Public Sub New(ByVal FilePath As String, ByVal MaxBufferSize As Int64, ByVal FlushMSeconds As Integer)
        Me.FilePath = FilePath
        Me.MaxBufferSize = MaxBufferSize
        Me.FlushMSeconds = FlushMSeconds
    End Sub

#Region "Buffers Pool"

    Public Buffers As New HashSet(Of FileBuffer)
    Private BuffersLock As New Object

    Public ReadOnly Property AllBufferLength As Int64
        Get
            Dim Length As Int64 = 0

            For Index = 0 To Buffers.Count - 1
                SyncLock BuffersLock
                    If Index >= Buffers.Count Then Exit For

                    Dim Buffer As FileBuffer = Buffers.ElementAt(Index)
                    If Buffer Is Nothing Then Exit For
                    Length = Length + Buffer.Length
                End SyncLock
            Next

            Return Length
        End Get
    End Property

    Private Function GetExistsBuffer(ByVal Position As Int64) As FileBuffer
        For Index As Integer = 0 To Buffers.Count - 1
            SyncLock BuffersLock
                If Index >= Buffers.Count Then Return Nothing

                Dim FBuffer As FileBuffer = Buffers.ElementAt(Index)
                If FBuffer IsNot Nothing Then
                    If Position >= FBuffer.StartPosition AndAlso Position <= (FBuffer.EndPosition + 1) Then
                        Return FBuffer
                    End If
                End If
            End SyncLock
        Next

        Return Nothing
    End Function

    Public Sub RemoveBuffer(ByRef Buffer As FileBuffer)
        If Buffers.Contains(Buffer) = False Then Return

        SyncLock BuffersLock
            If Buffers.Contains(Buffer) Then Buffers.Remove(Buffer)
        End SyncLock
    End Sub

#End Region

#Region "Write"

    Private CreateNewBufferLock As New Object

    Public Sub Write(ByRef FStream As FileStream, ByVal Position As Int64, ByVal Bytes As Byte())
        'Console.WriteLine(Now.ToString & ": Write Start: " & Position & ", End: " & Position + Bytes.Count - 1)
        Dim StartTime As DateTime = Now
        Dim SpanMSeconds As Decimal
        Dim ShowLogMSeconds As Integer = 10

        'Check input
        If Position < 0 OrElse Bytes Is Nothing OrElse Bytes.Count = 0 Then Return

        'Check input length and AllBufferLength
        'Console.WriteLine("AllBufferLength=" & AllBufferLength & ", MaxBufferSize=" & MaxBufferSize)
        If Buffers.Count >= MaxBuffersNumber OrElse Bytes.Length >= MaxBufferSize OrElse AllBufferLength >= MaxBufferSize Then
            FStream.Position = Position
            FStream.Write(Bytes, 0, Bytes.Count)

            'Console.WriteLine("Write Position A: " & Position & ", Write Length: " & Bytes.Count)
            Return
        End If
        SpanMSeconds = Now.Subtract(StartTime).TotalMilliseconds
        If SpanMSeconds > ShowLogMSeconds Then Console.WriteLine(Now.ToString & ": Check AllBufferLength. (StartPosition=" & Position & ", WaitTime=" & SpanMSeconds & "ms")
        StartTime = Now

        'Check If Exists Buffer
        Dim ExistsBuffer As FileBuffer = GetExistsBuffer(Position)
        SpanMSeconds = Now.Subtract(StartTime).TotalMilliseconds
        If SpanMSeconds > ShowLogMSeconds Then Console.WriteLine(Now.ToString & ": Check Exists Buffer. (StartPosition=" & Position & ", WaitTime=" & SpanMSeconds & "ms")
        StartTime = Now

        If ExistsBuffer IsNot Nothing Then
            ExistsBuffer.Write(FStream, Position, Bytes)
            Return
        End If
        SpanMSeconds = Now.Subtract(StartTime).TotalMilliseconds
        If SpanMSeconds > ShowLogMSeconds Then Console.WriteLine(Now.ToString & ": ExistsBuffer Write Bytes. (StartPosition=" & Position & ", WaitTime=" & SpanMSeconds & "ms")
        StartTime = Now

        'No Exists Buffer, create a new one
        SyncLock CreateNewBufferLock
            SpanMSeconds = Now.Subtract(StartTime).TotalMilliseconds
            If SpanMSeconds > ShowLogMSeconds Then Console.WriteLine(Now.ToString & ": Wait CreateNewBufferLock. (StartPosition=" & Position & ", WaitTime=" & SpanMSeconds & "ms")
            StartTime = Now

            'Check If Exists Buffer
            ExistsBuffer = GetExistsBuffer(Position)

            If ExistsBuffer IsNot Nothing Then
                ExistsBuffer.Write(FStream, Position, Bytes)
                Return
            End If
            SpanMSeconds = Now.Subtract(StartTime).TotalMilliseconds
            If SpanMSeconds > ShowLogMSeconds Then Console.WriteLine(Now.ToString & ": Check Exists Buffer. (StartPosition=" & Position & ", WaitTime=" & SpanMSeconds & "ms")
            StartTime = Now

            'Create New Buffer
            Dim FBuffer As New FileBuffer(FilePath, Me, FlushMSeconds, Position, Bytes)
            Buffers.Add(FBuffer)
            SpanMSeconds = Now.Subtract(StartTime).TotalMilliseconds
            If SpanMSeconds > ShowLogMSeconds Then Console.WriteLine(Now.ToString & ": Create new Buffer. (StartPosition=" & Position & ", WaitTime=" & SpanMSeconds & "ms, TotalBuffers.Length=" & Buffers.Count)
        End SyncLock

    End Sub


#End Region

#Region "Flush"

    Private Sub Flush()
        Do While Buffers.Count > 0
            Try
                Dim Buffer As FileBuffer = Buffers.First
                If Buffer Is Nothing Then Continue Do
                If Buffers.Contains(Buffer) Then Buffers.Remove(Buffer)

                Buffer.Flush()
            Catch ex As Exception
            End Try
        Loop
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

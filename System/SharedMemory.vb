Imports System
Imports System.IO
Imports System.Collections
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Runtime.Serialization
Imports System.Runtime.InteropServices
Imports System.Security.Permissions

Public Class SharedMemory
    Implements IDisposable

    'Parameters
    Public Name As String   'MapName, used to unique indicate the memory address
    Public Size As Int64    'MapSize

    Private FileMappingHandle As IntPtr 'FileMapping Handle
    Private MapViewAddress As IntPtr 'MapViewOfFile Address


#Region "New"

    Public Sub New(ByVal Name As String, ByVal Size As Int64, Optional ByRef IfNewCreate As Boolean = False)

        'Set Value
        Me.Name = Name
        Me.Size = Size

        'Init Parameters
        IfNewCreate = False

        '----------- Get FileMappingHandle -------------
        'Try th get exists handle by name
        FileMappingHandle = Win32.OpenFileMapping(Win32.FILE_MAP_WRITE, False, Name)

        If FileMappingHandle = IntPtr.Zero Then
            'Not Exists, Create a new one
            FileMappingHandle = Win32.CreateFileMapping(Win32.InvalidHandleValue, IntPtr.Zero, Win32.PAGE_READWRITE, 0, Size, Name)

            If FileMappingHandle = IntPtr.Zero Then
                Throw New Exception("Failed to create new FileMapping. (LastError: " & Hex(Err.LastDllError) & ")")
                Return
            End If

            IfNewCreate = True
        End If

        '----------- Get MapViewAddress -------------
        MapViewAddress = Win32.MapViewOfFile(FileMappingHandle, Win32.FILE_MAP_WRITE, 0, 0, IntPtr.Zero)
        If MapViewAddress = IntPtr.Zero Then
            Throw New Exception("Failed to get MapViewOfFile. (LastError: " & Hex(Err.LastDllError) & ")")
            Return
        End If
    End Sub

#End Region

#Region "Read"

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="Offset">FileMapping Offset Position</param>
    ''' <param name="Length">Read Length. If Zero, means read to end</param>
    ''' <returns></returns>

    Public Function Read(Optional ByVal Offset As Int64 = 0, Optional ByVal Length As Int64 = 0) As Byte()
        'Check input
        If Offset >= Size Then
            Throw New Exception("Exceed the max offset. (Request=" & Offset & ", MaxOffset=" & Size - 1 & ")")
            Return Nothing
        End If
        Dim MaxRemainLength As Int64 = Size - Offset
        If Length > MaxRemainLength Then
            Throw New Exception("Exceed the max length. (Request=" & Length & ", MaxLength=" & MaxRemainLength & ")")
            Return Nothing
        End If
        If Length = 0 Then Length = MaxRemainLength

        'Read
        Dim Bytes(Length - 1) As Byte
        Marshal.Copy(MapViewAddress + Offset, Bytes, 0, Length)

        'Return Value
        Return Bytes
    End Function

#End Region

#Region "Write"

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="Bytes">Pending write Bytes</param>
    ''' <param name="TargetOffset">Target Offset Position</param>
    Public Sub Write(ByVal Bytes As Byte(), Optional ByVal TargetOffset As Int64 = 0)
        If Bytes Is Nothing OrElse Bytes.Length = 0 Then Return
        Write(Bytes, TargetOffset, 0, Bytes.Length)
    End Sub

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="Bytes">Pending write Bytes</param>
    ''' <param name="TargetOffset">Target Offset Position</param>
    ''' <param name="SourceOffset">Source Offset Position</param>
    ''' <param name="SourceLength">Source Length</param>
    Public Sub Write(ByVal Bytes As Byte(), ByVal TargetOffset As Int64, ByVal SourceOffset As Int64, ByVal SourceLength As Int64)
        'Check source input
        If Bytes Is Nothing OrElse Bytes.Length = 0 Then Return
        If SourceOffset >= Bytes.Length Then Return
        Dim MaxSourceLength As Int64 = Bytes.Length - SourceOffset
        If SourceLength > MaxSourceLength Then SourceLength = MaxSourceLength

        'Check target size
        Dim TargetMaxRemainSize As Int64 = Size - TargetOffset
        If SourceLength > TargetMaxRemainSize Then
            Throw New Exception("Not enough space to write the bytes. (Request=" & SourceLength & ", MaxLength=" & TargetMaxRemainSize & ")")
            Return
        End If

        'Execute Write
        Marshal.Copy(Bytes, SourceOffset, MapViewAddress + TargetOffset, SourceLength)
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
            Win32.UnmapViewOfFile(MapViewAddress)
            Win32.CloseHandle(FileMappingHandle)
            Name = Nothing
            Size = 0
            MapViewAddress = IntPtr.Zero
            FileMappingHandle = IntPtr.Zero

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

    Protected Overrides Sub Finalize()
        Dispose(False)
    End Sub

#End Region


#Region "Win32"

    Public Class Win32

        Public Shared ReadOnly InvalidHandleValue As New IntPtr(-1)
        Public Const FILE_MAP_WRITE As Int32 = 2
        Public Const PAGE_READWRITE As Int32 = &H4


        Declare Function CreateFileMapping Lib "kernel32" Alias "CreateFileMappingA" _
    (ByVal hFile As IntPtr,
     ByVal pAttributes As IntPtr,
     ByVal flProtect As Int32,
     ByVal dwMaximumSizeHigh As Int32,
     ByVal dwMaximumSizeLow As Int32,
     ByVal pName As String) As IntPtr


        Declare Function OpenFileMapping Lib "kernel32" Alias "OpenFileMappingA" _
    (ByVal dwDesiredAccess As Int32,
    ByVal bInheritHandle As Boolean,
    ByVal name As String) As IntPtr


        Declare Function CloseHandle Lib "kernel32" _
    (ByVal handle As IntPtr) As Boolean


        Declare Function MapViewOfFile Lib "kernel32" _
    (ByVal hFileMappingObject As IntPtr,
     ByVal dwDesiredAccess As Int32,
     ByVal dwFileOffsetHigh As Int32,
     ByVal dwFileOffsetLow As Int32,
     ByVal dwNumberOfBytesToMap As IntPtr) _
     As IntPtr


        Declare Function UnmapViewOfFile Lib "kernel32" _
    (ByVal address As IntPtr) As Boolean


        Declare Function DuplicateHandle Lib "kernel32" _
    (ByVal hSourceProcessHandle As IntPtr,
    ByVal hSourceHandle As IntPtr,
    ByVal hTargetProcessHandle As IntPtr,
    ByRef lpTargetHandle As IntPtr,
    ByVal dwDesiredAccess As Int32,
    ByVal bInheritHandle As Boolean,
    ByVal dwOptions As Int32) As Boolean


        Public Const DUPLICATE_CLOSE_SOURCE As Int32 = &H1
        Public Const DUPLICATE_SAME_ACCESS As Int32 = &H2


        Declare Function GetCurrentProcess Lib "kernel32" _
    () As IntPtr


    End Class

#End Region


End Class

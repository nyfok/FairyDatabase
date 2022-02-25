Public Class Data

#Region "Data Section"

    ''' <summary>
    ''' Max 281万亿
    ''' </summary>
    Public ID As Int64 = 0


    ''' <summary>
    ''' Store the data content
    ''' </summary>
    Private theValue As Byte() = Nothing


    ''' <summary>
    ''' Store the data content
    ''' </summary>
    Public Property Value As Byte()
        Get
            Return theValue
        End Get
        Set(value As Byte())
            theValue = value
            If theValue Is Nothing Then
                Length = 0
            Else
                Length = theValue.Length
            End If
        End Set
    End Property


    Public Sub New()
    End Sub

    Public Sub New(ByVal ID As Int64, Optional ByVal Value As Byte() = Nothing)
        Me.ID = ID
        Me.Value = Value
    End Sub

#End Region

#Region "Data for DB Section"

    ''' <summary>
    ''' 8 Bytes, MaxValue: 9223372036854775807 => 8192PB => If 10000 File, Each File Supports 838TB
    ''' </summary>
    Public StartPOS As Int64 = 0

    ''' <summary>
    ''' Data Bytes' Length: 6 Bytes, MaxValue: 281474976710655  => 256TB
    ''' </summary>
    Public Length As Int64 = 0

    ''' <summary>
    ''' Data Block Length: 6 Bytes, MaxValue: 281474976710655  => 256TB
    ''' </summary>
    Public BlockLength As Int64 = 0

    Public Sub Clear()
        ID = 0
        Value = Nothing
        StartPOS = 0
        Length = 0
        BlockLength = 0
    End Sub

    Public Sub FormatBeforeAddToPage()
        'Can enlarge blocklength if need. If enlarge, future can directly updata data in same block at most time.
        BlockLength = Length
    End Sub

#Region "Bytes Convert Parameters"

    ''' <summary>
    ''' 6 Bytes, MaxValue: 281474976710655 => 281万亿
    ''' </summary>
    Public Property ID_Bytes As Byte()
        Get
            Dim FBytes As Byte() = BitConverter.GetBytes(ID)
            ReDim Preserve FBytes(5)
            Return FBytes
        End Get
        Set(Bytes As Byte())
            If Bytes Is Nothing OrElse Bytes.Length < 6 Then
                ID = -1
                Return
            End If

            Dim FBytes As Byte() = Bytes.Clone
            ReDim Preserve FBytes(7)
            ID = BitConverter.ToInt64(FBytes, 0)
        End Set
    End Property

    ''' <summary>
    ''' 8 Bytes, MaxValue: 9223372036854775807 => 8192PB => If 10000 File, Each File Supports 838TB
    ''' </summary>
    Public Property StartPOS_Bytes As Byte()
        Get
            Dim FBytes As Byte() = BitConverter.GetBytes(StartPOS)
            Return FBytes
        End Get
        Set(Bytes As Byte())
            If Bytes Is Nothing OrElse Bytes.Length <> 8 Then
                ID = -1
                Return
            End If

            Dim FBytes As Byte() = Bytes.Clone
            StartPOS = BitConverter.ToInt64(FBytes, 0)
        End Set
    End Property

    ''' <summary>
    ''' Data Bytes' Length: 6 Bytes, MaxValue: 281474976710655  => 256TB
    ''' </summary>
    Public Property Length_Bytes As Byte()
        Get
            Dim FBytes As Byte() = BitConverter.GetBytes(Length)
            ReDim Preserve FBytes(5)
            Return FBytes
        End Get
        Set(Bytes As Byte())
            If Bytes Is Nothing OrElse Bytes.Length < 6 Then
                ID = -1
                Return
            End If

            Dim FBytes As Byte() = Bytes.Clone
            ReDim Preserve FBytes(7)
            Length = BitConverter.ToInt64(FBytes, 0)
        End Set
    End Property

    ''' <summary>
    ''' Data Block Length: 6 Bytes, MaxValue: 281474976710655  => 256TB
    ''' </summary>
    Public Property BlockLength_Bytes As Byte()
        Get
            Dim FBytes As Byte() = BitConverter.GetBytes(BlockLength)
            ReDim Preserve FBytes(5)
            Return FBytes
        End Get
        Set(Bytes As Byte())
            If Bytes Is Nothing OrElse Bytes.Length < 6 Then
                ID = -1
                Return
            End If

            Dim FBytes As Byte() = Bytes.Clone
            ReDim Preserve FBytes(7)
            BlockLength = BitConverter.ToInt64(FBytes, 0)
        End Set
    End Property


#End Region

#Region "Combined Bytes - PageIndexBytes, PageRemoveBlockBytes"

    Public Property PageIndexBytes As Byte()
        Get
            Dim FBytes(25) As Byte
            Buffer.BlockCopy(ID_Bytes, 0, FBytes, 0, 6)
            Buffer.BlockCopy(StartPOS_Bytes, 0, FBytes, 6, 8)
            Buffer.BlockCopy(Length_Bytes, 0, FBytes, 14, 6)
            Buffer.BlockCopy(BlockLength_Bytes, 0, FBytes, 20, 6)

            Return FBytes
        End Get

        Set(IndexBytes As Byte())
            If IndexBytes Is Nothing OrElse IndexBytes.Length < 26 Then Return

            Dim FBytes(5) As Byte
            Buffer.BlockCopy(IndexBytes, 0, FBytes, 0, FBytes.Length)
            ID_Bytes = FBytes

            ReDim FBytes(7)
            Buffer.BlockCopy(IndexBytes, 6, FBytes, 0, FBytes.Length)
            StartPOS_Bytes = FBytes

            ReDim FBytes(5)
            Buffer.BlockCopy(IndexBytes, 14, FBytes, 0, FBytes.Length)
            Length_Bytes = FBytes

            ReDim FBytes(5)
            Buffer.BlockCopy(IndexBytes, 20, FBytes, 0, FBytes.Length)
            BlockLength_Bytes = FBytes
        End Set
    End Property

    Public Function PageIndexBytesFull(ByVal DataPageHeaderDataIndexSize As Integer) As Byte()
        Dim FBytes() As Byte = PageIndexBytes
        ReDim Preserve FBytes(DataPageHeaderDataIndexSize - 1)
        Return FBytes
    End Function

    Public Property PageRemoveBlockBytes As Byte()
        Get
            Dim FBytes(13) As Byte
            Buffer.BlockCopy(StartPOS_Bytes, 0, FBytes, 0, 8)
            Buffer.BlockCopy(BlockLength_Bytes, 0, FBytes, 7, 6)

            Return FBytes
        End Get

        Set(BlockBytes As Byte())
            If BlockBytes Is Nothing OrElse BlockBytes.Length < 14 Then Return

            Dim FBytes(7) As Byte
            Buffer.BlockCopy(BlockBytes, 0, FBytes, 0, FBytes.Length)
            StartPOS_Bytes = FBytes

            ReDim FBytes(5)
            Buffer.BlockCopy(BlockBytes, 8, FBytes, 0, FBytes.Length)
            BlockLength_Bytes = FBytes
        End Set
    End Property

#End Region

#End Region


End Class

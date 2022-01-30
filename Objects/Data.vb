Public Class Data

    ''' <summary>
    ''' 6 Bytes, MaxValue: 281474976710655 => 281万亿
    ''' </summary>
    Public ID_Byte As Byte() = Nothing

    ''' <summary>
    ''' 8 Bytes, MaxValue: 9223372036854775807 => 8192PB => If 10000 File, Each File Supports 838TB
    ''' </summary>
    Public StartPOS_Byte As Byte() = Nothing

    ''' <summary>
    ''' 6 Bytes, MaxValue: 281474976710655  => 256TB
    ''' </summary>
    Public Length_Byte As Byte() = Nothing

    ''' <summary>
    ''' Store the data content
    ''' </summary>
    Private theBytes As Byte() = Nothing

    Public Property Bytes As Byte()
        Get
            Return theBytes
        End Get
        Set(value As Byte())
            theBytes = value
            If theBytes Is Nothing Then
                Length = 0
            Else
                Length = theBytes.Length
            End If
        End Set
    End Property


#Region "ID, StartPOS, Length Properties"

    Private theID As Int64 = -1

    Public Property ID As Int64
        Get
            If theID >= 0 Then Return theID
            If ID_Byte Is Nothing Then Return -1
            Dim FBytes As Byte() = ID_Byte.Clone
            ReDim Preserve FBytes(7)
            theID = BitConverter.ToInt64(FBytes)
            Return theID
        End Get
        Set(value As Int64)
            theID = value
            Dim FBytes As Byte() = BitConverter.GetBytes(theID)
            ReDim Preserve FBytes(5)
            ID_Byte = FBytes
        End Set
    End Property


    Private theStartPOS As Int64 = -1

    Public Property StartPOS As Int64
        Get
            If theStartPOS >= 0 Then Return theStartPOS
            If StartPOS_Byte Is Nothing Then Return -1
            theStartPOS = BitConverter.ToInt64(StartPOS_Byte)
            Return theStartPOS
        End Get
        Set(value As Int64)
            theStartPOS = value
            StartPOS_Byte = BitConverter.GetBytes(theStartPOS)
        End Set
    End Property


    Private theLength As Int64 = -1

    Public Property Length As Int64
        Get
            If theLength >= 0 Then Return theLength
            If Length_Byte Is Nothing Then Return -1
            Dim FBytes As Byte() = Length_Byte.Clone
            ReDim Preserve FBytes(7)
            theLength = BitConverter.ToInt64(FBytes)
            Return theLength
        End Get
        Set(value As Int64)
            theLength = value
            Dim FBytes As Byte() = BitConverter.GetBytes(theLength)
            ReDim Preserve FBytes(5)
            Length_Byte = FBytes
        End Set
    End Property

    Public Shared Sub TestByteInt64Convert()

        Dim a As Int64 = 9223372036854775807
        Console.WriteLine(a)
        Dim fdata As New Data
        fdata.StartPOS = a
        Console.WriteLine(fdata.StartPOS)
        Console.WriteLine("byte length=" & fdata.StartPOS_Byte.Length)
        For Each fbyte In fdata.StartPOS_Byte
            Console.WriteLine(fbyte)
        Next

        Dim b As Int64 = 281474976710655
        Console.WriteLine(b)
        Dim fdata2 As New Data
        fdata2.ID = b
        Console.WriteLine(fdata2.ID)
        Console.WriteLine("byte length=" & fdata2.ID_Byte.Length)
        For Each fbyte In fdata2.ID_Byte
            Console.WriteLine(fbyte)
        Next

    End Sub
#End Region


#Region "New"

    Public Sub New()
    End Sub

    Public Sub New(ByVal ID As Int64, ByRef Bytes As Byte())
        Me.ID = ID
        Me.Bytes = Bytes
    End Sub

    Public Sub New(ByRef ID As Byte(), ByRef StartPOS As Byte(), ByRef Length As Byte())
        Me.ID_Byte = ID.Clone
        Me.theID = -1

        Me.StartPOS_Byte = StartPOS.Clone
        Me.theStartPOS = -1

        Me.Length_Byte = Length.Clone
        Me.theLength = -1
    End Sub

    Public Sub Clear()
        ID_Byte = Nothing
        theID = -1

        StartPOS_Byte = Nothing
        theStartPOS = -1

        Length_Byte = Nothing
        theLength = -1

        Bytes = Nothing
    End Sub
#End Region

#Region "IndexByte"

    Public Property IndexByte As Byte()
        Get
            Dim FBytes(19) As Byte
            Buffer.BlockCopy(ID_Byte, 0, FBytes, 0, ID_Byte.Length)
            Buffer.BlockCopy(StartPOS_Byte, 0, FBytes, 6, StartPOS_Byte.Length)
            Buffer.BlockCopy(Length_Byte, 0, FBytes, 14, Length_Byte.Length)

            Return FBytes
        End Get

        Set(IndexBytes As Byte())
            If IndexBytes Is Nothing OrElse IndexBytes.Length < 20 Then Return

            ReDim ID_Byte(5)
            Buffer.BlockCopy(IndexBytes, 0, ID_Byte, 0, ID_Byte.Length)
            theID = -1

            ReDim StartPOS_Byte(7)
            Buffer.BlockCopy(IndexBytes, 6, StartPOS_Byte, 0, StartPOS_Byte.Length)
            theStartPOS = -1

            ReDim Length_Byte(5)
            Buffer.BlockCopy(IndexBytes, 14, Length_Byte, 0, Length_Byte.Length)
            theLength = -1
        End Set
    End Property

#End Region

#Region "Tools"




#End Region


End Class

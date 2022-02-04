Public Class GZip

    Public Shared Function Zip(ByVal Bytes() As Byte) As Byte()
        On Error Resume Next

        'Check Input
        If Bytes Is Nothing Then Return Bytes
        If Bytes.Length = 0 Then Return Bytes

        'Define Parameters
        Dim GZippedBytes() As Byte

        'Read to MemoryStream
        Dim FMemoryStream As New System.IO.MemoryStream()

        'Create GZipStream
        Dim FGZipStream As New System.IO.Compression.GZipStream(FMemoryStream, IO.Compression.CompressionMode.Compress, True)

        'Get GZippedBytes
        FGZipStream.Write(Bytes, 0, Bytes.Length)
        FGZipStream.Close()
        FGZipStream.Dispose()

        FMemoryStream.Position = 0
        ReDim GZippedBytes(FMemoryStream.Length - 1)
        FMemoryStream.Read(GZippedBytes, 0, FMemoryStream.Length)

        'Clear Objects
        Bytes = Nothing
        FMemoryStream.Close()
        FMemoryStream.Dispose()

        'Return Value
        Return GZippedBytes

    End Function


    Public Shared Function UnZip(ByVal Bytes As Byte()) As Byte()
        'Check Input
        If Bytes Is Nothing Then Return Bytes
        If Bytes.Length = 0 Then Return Bytes

        'Unzip
        Using FMemoryStream As New System.IO.MemoryStream()
            Using FGZipStream As New System.IO.Compression.GZipStream(New System.IO.MemoryStream(Bytes), System.IO.Compression.CompressionMode.Decompress)
                Dim Buffer As Byte() = New Byte(4096) {}
                Dim ReadLength As Integer = FGZipStream.Read(Buffer, 0, Buffer.Length)
                While ReadLength <> 0
                    FMemoryStream.Write(Buffer, 0, ReadLength)
                    ReadLength = FGZipStream.Read(Buffer, 0, Buffer.Length)
                End While
                FGZipStream.Close()
            End Using

            Return FMemoryStream.ToArray()
        End Using
    End Function


End Class

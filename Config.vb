Public Class Config

    '================================= DATABASE CONFIG ============================================

    Public Shared DatabaseFolderPath As String = "db\" 'blank or end with \
    'Public Shared DatabasePageFileInitSize As Int64 = 100 * 1024 * 1024   '100M Bytes
    Public Shared DatabasePageFileInitSize As Int64 = 1 * 1024 * 1024   '100M Bytes

    Public Const DataPageSize As Int64 = 10000  'Store how many datas in one data page file
    Public Const DataPageFolderSize As Int64 = 1000   'Store how may data page files in one folder

    Public Const DataPageHeaderMetaSize As Int64 = 100  'Store the PageFile self info. First 8 Bytes=RealDataLength
    Public Const DataPageHeaderDataIndexSize As Integer = 30   'Each Data Index length
    Public Const DataPageHeaderSize As Int64 = DataPageHeaderMetaSize + DataPageHeaderDataIndexSize * DataPageSize  'Data page header size

    Public Const SupportWriteBuffer As Boolean = True   'If True => Write to disk after WriteBufferFlushMSeconds or buffer size >= PageWriteBufferSize. If False => Write immediately.
    Public Const WriteBufferFlushMSeconds As Integer = 6000
    Public Const DataPageWriteBufferSize As Int64 = 100 * 1024 '100K Bytes

End Class

Public Class Config

    ''' <summary>
    ''' Debug mode will output some log.
    ''' </summary>
    Public Shared IfDebugMode As Boolean = False

    Public Shared Sub Init(Optional ByVal DatabaseFolderPath As String = "db\", Optional ByVal DatabasePageFileInitSize As Int64 = 2 * 1024 * 1024)
        Config.DatabaseFolderPath = DatabaseFolderPath
        Config.DatabasePageFileInitSize = DatabasePageFileInitSize
    End Sub


#Region "Database Configuration"

    Private Shared theDatabaseFolderPath As String = "db\"

    ''' <summary>
    ''' Database's Folder Path. Support relative path and absolute path.
    ''' </summary>
    ''' <returns></returns>
    Public Shared Property DatabaseFolderPath As String
        Get
            Return theDatabaseFolderPath
        End Get
        Set(value As String)
            'should blank or end with \
            If String.IsNullOrWhiteSpace(value) Then
                theDatabaseFolderPath = "db\"
            Else
                value = value.Trim
                If value.Last = "\" Then
                    theDatabaseFolderPath = value
                Else
                    theDatabaseFolderPath = value & "\"
                End If
            End If
        End Set
    End Property

    ''' <summary>
    ''' Database's Page File's Init Size. Large size will get better read/write performance because request big space at once will get more continous disk spaces.
    ''' </summary>
    Public Shared DatabasePageFileInitSize As Int64 = 2 * 1024 * 1024   '2M Bytes

    ''' <summary>
    ''' How many datas in one data page file.
    ''' </summary>
    Public Const DataPageSize As Int64 = 10000

    ''' <summary>
    ''' How many data page files in one data page folder.
    ''' </summary>
    Public Const DataPageFolderSize As Int64 = 1000

    ''' <summary>
    ''' Store the PageFile's self info. First 8 Bytes=RealDataLength
    ''' </summary>
    Public Const DataPageHeaderMetaSize As Int64 = 100

    ''' <summary>
    ''' Data Index Size for each data.
    ''' </summary>
    Public Const DataPageHeaderDataIndexSize As Integer = 30

    ''' <summary>
    ''' Data Page's header size.
    ''' </summary>
    Public Const DataPageHeaderSize As Int64 = DataPageHeaderMetaSize + DataPageHeaderDataIndexSize * DataPageSize  'Data page header size

#Region "Write Buffer"

    ''' <summary>
    ''' Set if support write buffer. Still under performance optimization. For continous DataID blocks writing can enable writebuffer, otherwise suggest disable it.
    ''' If True => Write to disk after WriteBufferFlushMSeconds or buffer size >= PageWriteBufferSize.
    ''' If False => Write immediately.
    ''' </summary>
    Public Const SupportWriteBuffer As Boolean = True

    ''' <summary>
    ''' Write Buffer Wait MSeconds before auto flush
    ''' </summary>
    Public Const WriteBufferFlushMSeconds As Integer = 500

    ''' <summary>
    ''' Keep buffersize for each data page.
    ''' </summary>
    Public Const DataPageWriteBufferSize As Int64 = 100 * 1024 '100K Bytes

#End Region

#Region "Page Header Buffer"

    ''' <summary>
    ''' Page's data header buffer
    ''' </summary>
    Public Const SupportPageHeaderBuffer As Boolean = True

    ''' <summary>
    ''' Page Header wait MSeconds before auto flush
    ''' </summary>
    Public Const PageHeaderBufferFlushMSeconds As Integer = 500

    ''' <summary>
    ''' Page Length wait MSeconds before flush
    ''' If SupportPageHeaderBuffer=True, will use PageHeaderBufferFlushMSeconds time to flush all the page header instead flush length only.
    ''' If SupportPageHeaderBuffer=False, will use PageLengthFlushMSeconds to flush length only
    ''' </summary>
    Public Const PageLengthFlushMSeconds As Integer = 1000

    ''' <summary>
    ''' Set each page has how many index mutexes.
    ''' This settings always functional and will ignore SupportPageHeaderBuffer settings. 
    ''' </summary>
    Public Const PageHeaderIndexMutexesSize As Integer = 100

#End Region


#End Region

End Class

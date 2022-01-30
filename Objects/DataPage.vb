Imports System.IO
Public Class DataPage

#Region "File Related"

    Public Shared Sub CreatePage(ByVal PageID As Int64)
        Dim PageFilePath As String = GetPageFilePath(PageID)
        If File.Exists(PageFilePath) Then Return

        CreatePageDO(PageFilePath)
    End Sub

    Private Shared Sub CreatePageDO(ByVal PageFilePath As String)
        Dim PageFolderPath As String = New FileInfo(PageFilePath).DirectoryName
        If Directory.Exists(PageFolderPath) = False Then
            Directory.CreateDirectory(PageFolderPath)
        End If

        Dim FStream As New FileStream(PageFilePath, FileMode.CreateNew)
        FStream.SetLength(Config.DatabasePageInitSize)  'use to fast create file
        FStream.Close()
        FStream.Dispose()

        SetPageFileRealLength(PageFilePath, Config.DataPageHeaderSize)
    End Sub


    Private Shared ExistsPages As New Dictionary(Of Int64, DateTime)  'key=PageID, Value=last check time
    Private Shared ExistsPagesLock As New Object
    Private Shared Function IfPageExists(ByVal PageID As Int64, Optional ByVal PageFilePath As String = Nothing, Optional ByVal IfAllowCache As Boolean = False, Optional ByVal CacheSeconds As Integer = 600) As Boolean
        'Check IfAllowCache
        If IfAllowCache Then
            If ExistsPages.ContainsKey(PageID) AndAlso Now.Subtract(ExistsPages(PageID)).TotalSeconds <= CacheSeconds Then
                Return True
            End If
        End If

        '------------ Check File System ----------
        'Get PageFilePath
        If String.IsNullOrWhiteSpace(PageFilePath) Then
            PageFilePath = GetPageFilePath(PageID)
        End If

        'Check If File Exists
        If File.Exists(PageFilePath) = False Then Return False

        'File Exists
        If ExistsPages.ContainsKey(PageID) Then
            ExistsPages(PageID) = Now
        Else
            SyncLock ExistsPagesLock
                If ExistsPages.ContainsKey(PageID) Then
                    ExistsPages(PageID) = Now
                Else
                    ExistsPages.Add(PageID, Now)
                End If
            End SyncLock
        End If

        'Return True
        Return True
    End Function

#Region "Set/Get Page File Real Length"

    Public Shared Sub SetPageFileRealLength(ByVal PageFilePath As String, ByVal FileLength As Int64)

        'Generate PageFileStream
        Dim PageFileStream As FileStream = File.OpenWrite(PageFilePath)

        'Write
        SetPageFileRealLengthDo(PageFileStream, FileLength)

        'Close Stream
        PageFileStream.Flush()
        PageFileStream.Close()
        PageFileStream.Dispose()
    End Sub

    Public Shared Sub SetPageFileRealLengthDo(ByVal PageFileStream As FileStream, ByVal FileLength As Int64)
        'Seek
        PageFileStream.Position = 0

        'Write
        Dim FBytes As Byte() = BitConverter.GetBytes(FileLength)
        PageFileStream.Write(FBytes)
    End Sub


    Public Shared Function GetPageFileRealLength(ByVal PageFilePath As String) As Int64
        'Generate PageFileStream
        Dim PageFileStream As FileStream = File.Open(PageFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)

        'Get FileLength
        Dim FileLength As Int64 = GetPageFileRealLengthDO(PageFileStream)

        'Close PageFileStream
        PageFileStream.Close()
        PageFileStream.Dispose()

        'Return value
        Return FileLength
    End Function

    Public Shared Function GetPageFileRealLengthDO(ByVal PageFileStream As FileStream) As Int64

        'Seek
        PageFileStream.Position = 0

        'Read
        Dim FBytes(7) As Byte
        PageFileStream.Read(FBytes, 0, 8)

        'Get FileLength
        Dim FileLength As Int64 = BitConverter.ToInt64(FBytes)

        'Return value
        Return FileLength
    End Function

    Private Shared Sub TestPageFileRealLength()
        DataPage.CreatePage(2)
        Dim pagefilepath As String = DataPage.GetPageFilePath(2)
        DataPage.SetPageFileRealLength(pagefilepath, 832)
        Console.WriteLine(DataPage.GetPageFileRealLength(pagefilepath))
    End Sub
#End Region

#End Region

#Region "Write Data"

    Private Shared PageWriteLock As New Concurrent.ConcurrentDictionary(Of Int64, Object)
    Private Shared AddNewLockLock As New Object
    Public Shared Sub WriteData(ByVal FData As Data)
        'Get PageID
        If FData.ID < 0 Then Return
        Dim PageID As Int64 = GetPageID(FData.ID)

        'Get Page File Path
        Dim PageFilePath As String = GetPageFilePath(PageID)

        'Make Sure Page File Exists
        If IfPageExists(PageID, PageFilePath) = False Then
            CreatePageDO(PageFilePath)
        End If

        'Get Lock Object
        Dim LockObject As Object
        If PageWriteLock.ContainsKey(PageID) Then
            LockObject = PageWriteLock(PageID)
        Else
            'add new lock
            SyncLock AddNewLockLock
                If PageWriteLock.ContainsKey(PageID) Then
                    LockObject = PageWriteLock(PageID)
                Else
                    LockObject = New Object
                    PageWriteLock.TryAdd(PageID, LockObject)
                End If
            End SyncLock
        End If

        'Thread safe write. Single page file's current real length, can only be written in single thread
        Dim FStream As FileStream
        'FStream = File.OpenWrite(PageFilePath)
        FStream = File.Open(PageFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)

        SyncLock LockObject
            'Cal FData's StartPOS, Length, EndPOS
            Dim EndPOS As Int64 = 0

            If FData.Length <= 0 Then
                FData.StartPOS = 0
            Else
                Dim PageFileRealLength As Int64 = GetPageFileRealLengthDO(FStream)
                FData.StartPOS = PageFileRealLength
                EndPOS = FData.StartPOS + FData.Length - 1

                'Update PageFileRealLength
                PageFileRealLength = EndPOS + 1
                SetPageFileRealLengthDo(FStream, PageFileRealLength)

                'Check If Page File Length Enough
                Dim CurrentPageFileLength As Int64 = New FileInfo(PageFilePath).Length
                If EndPOS >= CurrentPageFileLength Then
                    FStream.SetLength(CurrentPageFileLength + Config.DatabasePageInitSize)  'use to fast create file
                End If
            End If

        End SyncLock

        'Write Data Index
        FStream.Position = GetDataIndexPOS(FData.ID)
        FStream.Write(FData.IndexByte, 0, FData.IndexByte.Count)

        'Write Data Block
        'Support nothing write for pre-allocate byte space
        If FData.StartPOS > 0 AndAlso FData.Bytes IsNot Nothing AndAlso FData.Bytes.Count > 0 Then
            FStream.Position = FData.StartPOS
            FStream.Write(FData.Bytes, 0, FData.Bytes.Count)
        End If

        'Close Stream
        FStream.Flush()
        FStream.Close()
        FStream.Dispose()

    End Sub

#End Region

#Region "Get Datas"

    Public Shared Function GetAllDatas(ByVal PageID As Int64) As List(Of Data)

    End Function

#End Region


#Region "Common Tools"

    Public Shared Function GetPageID(ByVal DataID As Int64) As Int64
        Return Int(DataID / Config.DataPageSize)
    End Function

    Public Shared Function GetPageFilePath(ByVal PageID As Int64) As String
        Return Config.DatabaseFolderPath & Int(PageID / Config.DataPageFolderSize) & "dpf\" & PageID & "dp.fdb"
    End Function

    Public Shared Function GetDataIndexPOS(ByVal DataID As Int64) As Int64
        Dim IndexID As Int64 = DataID Mod Config.DataPageSize
        Dim POS As Int64 = Config.DataPageHeaderMetaSize + Config.DataPageHeaderSubIndexSize * IndexID
        Return POS
    End Function

#End Region



End Class

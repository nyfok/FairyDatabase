<h1>Fairy Database</h1>
<h3>A lite and fast key-value database</h3>
FairyDatabase is a lite and fast key-value database. It can be used to save PB numbers of small data/files and access by int64 number as key.

<h2>Why use Fairy database</h2>
If you have lots of small data/files, little then 1MB.
If you don't want save those data/files into a typical Relational DB such like mysql/sql server which will cause low performance and big space usage.
If you don't want save those data/files directly to the file system which will take big space usage, harder to backup (due too many small files), low performance to write and read.
If you don't want use hbase, mongo or other none-sql network file system or key-value system program due compilicated to setup and maintain.

If you say YES to any of these question above, you may try FairyDatabase.

<b>Good Performance:</b>
Write Speed: FairyDatabase is 8+ times faster then using OS file system to save files.
Read Speed: FairyDatabase is 10+ times faster then using OS file system to save files.
(Please refer to /Test/Test/PerformanceTest/ to check the performance test resut.)

<b>Space Saving:</b> 
Write 10000 1200Bytes files to file system will take 39.0 MB disk space (up to disk drive's cluster settings).
Write 10000 1200Bytes files to FairyDatabase will take 14.0 MB disk space (disk driver's cluster settings will not influence space a lot.)

<b>Multiple Thread and Processes Support</b> 
Currently, FairyDatabase support multiple thread and processes. Internal, FairyDatabase use ShareMemory/Mutex to synchronize different threads or processes. All these are already supported, what you need to do is just import the FairyDatabase.
If future, FairyDatabase may support network grid version if I have time to continue upgrade this program.

<h2>How to use it</h2>
Use FairyDatabase is very simple. 
First, please compile FairyDatabase project to get the FairyDatabase.dll file.
Second, import the dll file to your project.
Third, use FairyDatabase to Write/Read bytes like below.
Note: FairyDatabase use Int64 number as key to write and read data.

<h3>Write:</h3>
```
    Dim DataID As Int64 = 1
    Dim FData As New Data(DataID)
    FData.Value = System.Text.Encoding.UTF8.GetBytes("Hello World " & DataID & ". (" & Now.ToString & ")")
    Page.Write(FData)

    Page.FlushAll()
```

<h3>Read:</h3>
```
    Dim FData As Data = Page.Read(DataID)
```

<h2>Operating Environment</h2>
Currently, runing FairyDatabase needs:
 - .Net 6 framework
 - Windows Environment. Due FairyDatabase use global Mutex in the program.

 <h2>How FairyDatabase Works</h2>
 1. Based on the input DataID to calculate the PageID. Each page will hold 10000 datas and this will save disk space.
 2. Each Page File has header area and block area. 
 Header area including meta area and data index area. 
 - Meta area used to save page file self information such like file's real length. 
 - Data Index Area used to save data's index information, including dataid, startposition, length, spacelength.
 Block Area used to save data bytes.
 3. Different process/threads need to synchronize page file's real length and share index mutex for safe writing.
 4. Add BufferWriter to optimize writing performance. In future, may develop BufferReader to optimize reading performance.
  
<h2>Hello from Jon</h2>
I wrote this program on Feb.2022 and it took me for around 7 days. I hope this program may help you and your work.
If you find any bug or have any suggestion, please feel free to let me know by leaving a message.
Thank you. Have a nice day.
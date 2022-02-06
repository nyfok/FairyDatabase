<h1>Fairy Database</h1>
<h3>A light and fast key-value database</h3>
FairyDatabase is a light and fast key-value database. It can be used to save petabytes of small data/files and be accessed by int64 keys.

<br>
<br>
<br>
<h2>When to use Fairy Database</h2>
When you have lots of small data/files (smaller then 1MB).<br>
When you don't want save large amounts of data/files into a typical Relational DB such as MySQL/SQL Server, which can cause low performance and inefficent space usage.<br>
When you don't want save those data/files directly to the file system which will take big space usage, harder to backup (due too many small files), low performance to write and read.<br>
When you don't want to use HBase, MongoDB or other non SQL based network file system or key-value system due to compilicated setup or maintenance.<br>

If you have answered <b>Yes</b> to any of the questions above, you should try FairyDatabase.<br>
<br>

<h2>Advantages of using Fairy Database</h2>
<b>Performance benifits:</b><br>
Write Speed: FairyDatabase is 8+ times faster then using the default file system to save files.<br>
Read Speed: FairyDatabase is 10+ times faster then using the default file system to save files.<br>
(Please refer to <a href="https://github.com/nyfok/FairyDatabase/tree/develop/Test/Test/PerformanceTestResultsReference">/Test/Test/PerformanceTest/</a> to check the performance test results.)<br>
<br>

<b>Space Saving:</b> <br>
Writing 10000 1.2KB files to the file system will take 39.0 MB of disk space (up to disk drive's cluster settings).<br>
Writing 10000 1.2KB files to FairyDatabase will take 14.0 MB of disk space (disk driver's cluster settings won't influence space a lot).<br>

<b>Multi Thread and Process support</b> <br>
Currently, FairyDatabase supports multiple thread and processes. Internally, FairyDatabase use ShareMemory/Mutex to synchronize different threads or processes. All these are already supported, what you need to do is just import the FairyDatabase.
If future, FairyDatabase may support network grid version if I have time to continue to upgrade this program.<br>
<br>

<h2>Usage</h2>
<h3>Installation</h3>
Installing FairyDatabase is very simple. <br>
<b>First</b>, compile the FairyDatabase project to get the FairyDatabase.dll file. <br>
<b>Second</b>, import the dll file to your project. <br>

Note: FairyDatabase uses Int64 number as key to write and read data. <br>

<h3>Writing:</h3>
<pre><code>    Dim DataID As Int64 = 1
    Dim FData As New Data(DataID)
    FData.Value = System.Text.Encoding.UTF8.GetBytes("Hello World " & DataID & ". (" & Now.ToString & ")")
    Page.Write(FData)
    Page.FlushAll()
</code></pre>

<h3>Reading:</h3>
<pre><code>    Dim FData As Data = Page.Read(DataID)
</code></pre>
<br>

<h2>Requirements</h2>
Currently, using FairyDatabase requires: <br>
 - .Net 6 framework <br>
 - A Windows Environment. (Due to FairyDatabase using global Mutex in the program.) <br>

<br>
 <h2>How FairyDatabase works</h2>
 1. Uses the input DataID to calculate the PageID. Each page holds 10k data which can save disk space. <br>
 2. Each Page File has a header area and block area.  <br>
 The header area includes a meta area and data index area.  <br>
 - The meta area is used to save page file self information such like file's real length.  <br>
 - The data index area is used to save data's index information, including dataid, startposition, length and spacelength. <br>
 The Block area stores the data. <br>
 3. Different process/threads synchronize the page file's real length and share index mutex for safe writing. <br>
 4. Uses a BufferWriter to optimize writing performance. In the future, I may develop BufferReader to optimize reading performance. <br>
  
<br>
<br>
<h2>Note from Jon</h2>
I spent 30 hours programing FairyDatabase during the February of 2022. I hope this program can help you and your work. <br>
If you find any bugs or have any suggestions, please feel free to let me know by leaving a message or pull request. <br>
Thank you for reading, and have a nice day. <br>
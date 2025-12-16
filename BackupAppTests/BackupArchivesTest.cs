using System.Collections.Concurrent;
using ConsoleBackupApp.Backup;
using BackupAppTests.TestingTools;
using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;

namespace BackupAppTests;
public class BackupArchivesTest
{
    private string _currentPath;
    private char _currentDrive;
    private string _archiveFolder;
    private string _testFilesFolder;
    private ArchiveQueue _archiveQueue;
    private BackupArchives _backupArchives;
    private CancellationTokenSource _cancellationTokenSource;
    private List<string> _testFiles;

    [OneTimeSetUp]
    public void ConfigSetup()
    {
        _currentPath = Path.GetFullPath(Directory.GetCurrentDirectory());
        _currentPath += (_currentPath[^1] != Path.DirectorySeparatorChar) ? Path.DirectorySeparatorChar : "";
        _currentDrive = _currentPath[0];

        _archiveFolder = _currentPath + "ArchiveTest" + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(_archiveFolder);
        _testFilesFolder = _currentPath + "FileTest" + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(_testFilesFolder);

        // Create test files with content
        _testFiles = new();
        _testFiles.Add(FileTools.CreateLargeTestFile(_testFilesFolder, "File1.txt"));
        string context = "Start:";
        for (int i = 0; i < 16; i++)
        {
            context += Encoding.UTF8.GetString(SHA1.HashData(Encoding.UTF8.GetBytes(context)));
            _testFiles.Add(FileTools.CreateTestFile(_testFilesFolder, $"HashFile{i}.hex", context));
        }

    }

    [OneTimeTearDown]
    public void ConfigCleanup()
    {
        GC.Collect();
        Directory.Delete(_archiveFolder,true);
        Directory.Delete(_testFilesFolder,true);
    }
    [SetUp]
    public void Setup()
    {
        BackupStat backupStat = new();
        _archiveQueue = new ArchiveQueue(_currentDrive, backupStat);
        _cancellationTokenSource = new CancellationTokenSource();
        _backupArchives = new BackupArchives(_archiveQueue, _archiveFolder);
    }
    [TearDown]
    public void TearDown()
    {
        try
        {
            File.Delete(_archiveFolder + _archiveQueue.Drive + ".zip");
        }
        catch (Exception)
        {
            throw;
        }
        _cancellationTokenSource.Dispose();
        GC.Collect();
    }
    [Test]
    public void Consumer_Empty_ArchiveQueue()
    {
        // Arrange

        // Act
        _cancellationTokenSource.CancelAfter(10);//exit loop
        _backupArchives.Start(_cancellationTokenSource.Token);

        // Assert
        Assert.Pass("No exceptions thrown for empty queue.");
    }

    [Test]
    public void Consumer_AddOneFileToArchive()
    {
        // Arrange
        _archiveQueue.InsertPath(_testFiles[0]);
        string zipFile = _archiveFolder + _archiveQueue.Drive + ".zip";

        // Act
        _cancellationTokenSource.CancelAfter(10);//exit loop
        _backupArchives.Start(_cancellationTokenSource.Token);
        using ZipArchive zipArchive = ZipFile.Open(zipFile, ZipArchiveMode.Read);

        // Assert
        //Verify Compression was done correctly
        Assert.That(File.Exists(_archiveFolder + _archiveQueue.Drive + ".zip"));
        Assert.That(zipArchive.Entries, Has.Count.EqualTo(1));
        FileTools.TestDoFilesMatch(_testFiles[0], zipFile);
    }

    [Test]
    public void Consumer_AddManyFileToArchive()
    {
        // Arrange
        for (int i = 1; i < 17; i++)
        {
            _archiveQueue.InsertPath(_testFiles[i]);
        }
        string zipFile = _archiveFolder + _archiveQueue.Drive + ".zip";

        // Act
        _cancellationTokenSource.CancelAfter(10);//exit loop
        _backupArchives.Start(_cancellationTokenSource.Token);
        using ZipArchive zipArchive = ZipFile.Open(zipFile, ZipArchiveMode.Read);

        // Assert
        //Verify Compression was done correctly
        Assert.That(File.Exists(zipFile));
        Assert.That(zipArchive.Entries, Has.Count.EqualTo(16));
        for (int i = 1; i < 17; i++)
        {
            FileTools.TestDoFilesMatch(_testFiles[i], zipFile);
        }
    }

    [Test]
    public void Consumer_AddManyFileToArchive_EarlyCancel()
    {
        // Arrange
        for (int i = 1; i < 17; i++)
        {
            _archiveQueue.InsertPath(_testFiles[i]);
        }
        string zipFile = _archiveFolder + _archiveQueue.Drive + ".zip";

        // Act
        _cancellationTokenSource.Cancel();//exit loop
        _backupArchives.Start(_cancellationTokenSource.Token);
        using ZipArchive zipArchive = ZipFile.Open(zipFile, ZipArchiveMode.Read);

        // Assert
        //Verify Compression was done correctly
        Assert.That(File.Exists(zipFile));
        Assert.That(zipArchive.Entries, Has.Count.EqualTo(16));
        for (int i = 1; i < 17; i++)
        {
            FileTools.TestDoFilesMatch(_testFiles[i], zipFile);
        }
    }
}

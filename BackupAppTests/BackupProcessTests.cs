using BackupAppTests.TestingTools;
using ConsoleBackupApp.Backup;
using ConsoleBackupApp.DataPaths;

namespace BackupAppTests;

[TestFixture]
public class BackupProcessTest
{
    private string _currentPath;
    private char _currentDrive;
    private string _archiveFolder;
    private string _testFilesFolder;
    private List<ArchiveQueue> _archiveQueues;
    private CancellationTokenSource _cancellationTokenSource;
    private List<string> _testFiles;
    private List<string> _testDirectories;


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
        _testFiles = FileTools.CreateTestDirectories(_testFilesFolder, out List<string> testDirectories);
        _testDirectories = testDirectories;
    }

    [OneTimeTearDown]
    public void ConfigCleanup()
    {
        GC.Collect();
        Directory.Delete(_archiveFolder, true);
        Directory.Delete(_testFilesFolder, true);
    }
    [SetUp]
    public void Setup()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        BackupStat backupStat = new();
        _archiveQueues = [new(
            _currentDrive, backupStat)];
    }

    [TearDown]
    public void TearDown()
    {
        _cancellationTokenSource.Dispose();
        GC.Collect();
    }

    [Test]
    public void Producer_EmptyDirectory()
    {
        // Arrange
        Queue<DataPath> dataPathsQueue = new Queue<DataPath>();
        dataPathsQueue.Enqueue(new DataPath(PathType.Directory,CopyMode.None, _testDirectories[4]));
        BackupShared backupShared = new(dataPathsQueue,_archiveQueues);
        BackupProcess backupProcess = new(backupShared, []);

        // Act
        backupProcess.Start(_cancellationTokenSource.Token); //with end if there are no DataPaths left
        ArchiveQueue archiveQueue = _archiveQueues.First();

        // Assert
        Assert.That(archiveQueue.PathsToCopy, Is.Empty);
    }

    [Test]
    public void Producer_DataPathWithInvalidPath()
    {
        // Arrange
        Queue<DataPath> dataPathsQueue = new Queue<DataPath>();
        dataPathsQueue.Enqueue(new DataPath(PathType.Directory,CopyMode.None, _testDirectories[1] + "adsjh"));
        BackupShared backupShared = new(dataPathsQueue,_archiveQueues);
        BackupProcess backupProcess = new(backupShared, []);

        // Act
        backupProcess.Start(_cancellationTokenSource.Token); //with end if there are no DataPaths left
        ArchiveQueue archiveQueue = _archiveQueues.First();

        // Assert
        Assert.That(archiveQueue.PathsToCopy, Is.Empty);
    }

    [Test]
    public void Producer_SingleFile()
    {
        // Arrange
        Queue<DataPath> dataPathsQueue = new Queue<DataPath>();

        dataPathsQueue.Enqueue(new DataPath(PathType.File, CopyMode.None, _testFiles[1]));

        BackupShared backupShared = new(dataPathsQueue,_archiveQueues);
        BackupProcess backupProcess = new(backupShared, []);

        // Act
        backupProcess.Start(_cancellationTokenSource.Token); //with end if there are no DataPaths left
        ArchiveQueue archiveQueue = _archiveQueues[0];
        
        // Assert
        Assert.That(archiveQueue.PathsToCopy, Has.Count.EqualTo(1));
        Assert.That(archiveQueue.PathsToCopy.Take(), Is.EqualTo(_testFiles[1]));
    }

    [Test]
    public void Producer_TwoDataPathsBothDirectories()
    {
        // Arrange
        Queue<DataPath> dataPathsQueue = new Queue<DataPath>();

        dataPathsQueue.Enqueue(new DataPath(PathType.Directory, CopyMode.None, _testDirectories[1]));
        dataPathsQueue.Enqueue(new DataPath(PathType.Directory, CopyMode.None, _testDirectories[2]));

        BackupShared backupShared = new(dataPathsQueue,_archiveQueues);
        BackupProcess backupProcess = new(backupShared, []);

        // Act
        backupProcess.Start(_cancellationTokenSource.Token); //with end if there are no DataPaths left
        ArchiveQueue archiveQueue = _archiveQueues[0];
        
        // Assert
        Assert.That(archiveQueue.PathsToCopy, Has.Count.EqualTo(16));


    }
    [Test]
    public void Producer_ThreeDataPaths()
    {
        // Arrange
        Queue<DataPath> dataPathsQueue = new Queue<DataPath>();

        dataPathsQueue.Enqueue(new DataPath(PathType.Directory, CopyMode.None, _testDirectories[1]));
        dataPathsQueue.Enqueue(new DataPath(PathType.Directory, CopyMode.None, _testDirectories[3]));
        dataPathsQueue.Enqueue(new DataPath(PathType.File, CopyMode.None, _testFiles[0]));
        
                    

        BackupShared backupShared = new(dataPathsQueue,_archiveQueues);
        BackupProcess backupProcess = new(backupShared, []);

        // Act
        backupProcess.Start(_cancellationTokenSource.Token); //with end if there are no DataPaths left
        ArchiveQueue archiveQueue = _archiveQueues[0];
        
        // Assert
        Assert.That(archiveQueue.PathsToCopy, Has.Count.EqualTo(20));
    }

    [Test]
    public void Producer_TheWholeTestDirectory()
    {
        // Arrange
        Queue<DataPath> dataPathsQueue = new Queue<DataPath>();

        dataPathsQueue.Enqueue(new DataPath(PathType.Directory, CopyMode.None, _testFilesFolder));

        BackupShared backupShared = new(dataPathsQueue,_archiveQueues);
        BackupProcess backupProcess = new(backupShared, []);

        // Act
        backupProcess.Start(_cancellationTokenSource.Token); //with end if there are no DataPaths left
        ArchiveQueue archiveQueue = _archiveQueues[0];
        
        // Assert
        Assert.That(archiveQueue.PathsToCopy, Has.Count.EqualTo(22));

    }
    [Test]
    public void Producer_TwoThreads()
    {
        // Arrange
        Queue<DataPath> dataPathsQueue = new();
        dataPathsQueue.Enqueue(new DataPath(PathType.Directory, CopyMode.None, _testDirectories[1]));
        dataPathsQueue.Enqueue(new DataPath(PathType.Directory, CopyMode.None, _testDirectories[3]));
        dataPathsQueue.Enqueue(new DataPath(PathType.File, CopyMode.None, _testFiles[0]));
        dataPathsQueue.Enqueue(new DataPath(PathType.File, CopyMode.None, _testFiles[1]));
        dataPathsQueue.Enqueue(new DataPath(PathType.Directory, CopyMode.None, _testDirectories[2]));

        BackupShared backupShared = new(dataPathsQueue,_archiveQueues);
        BackupProcess backupProcess = new(backupShared, []);
        BackupProcess backupProcess2 = new(backupShared, []);

        // Act
        Thread thread1 = new(() =>backupProcess.Start(_cancellationTokenSource.Token)); //with end if there are no DataPaths left
        Thread thread2 = new(() =>backupProcess.Start(_cancellationTokenSource.Token)); //with end if there are no DataPaths left
        thread1.Start();
        thread2.Start();
        thread1.Join();
        thread2.Join();
        ArchiveQueue archiveQueue = _archiveQueues[0];
        
        // Assert
        Assert.That(archiveQueue.PathsToCopy, Has.Count.EqualTo(22));
    }

}
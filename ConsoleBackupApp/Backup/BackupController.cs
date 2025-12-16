using ConsoleBackupApp.DataPaths;
using ConsoleBackupApp.Logging;
using ConsoleBackupApp.PriorBackup;
using ConsoleBackupApp.Utils;

namespace ConsoleBackupApp.Backup;
public class BackupController(string folderPath, BackupShared backupShared, List<BackupArchives> backupArchives, List<BackupProcess> backupProcesses, PriorBackupPath[] priorBackupPaths, BackupStat backupStat)
{
    private readonly PriorBackupPath[] _priorBackups = priorBackupPaths;
    private readonly string _folderPath = folderPath;
    private List<BackupArchives> _backupArchives = backupArchives;
    private List<BackupProcess> _backupProcesses = backupProcesses;
    private BackupShared _backupShared = backupShared;
    private BackupStat _backupStat = backupStat;

    public static BackupController Init(string folderPath, List<DataPath> dataPaths, List<PriorBackupPath> priorBackups)
    {
        priorBackups.Sort();

        //Load datapaths into queue and get all drives
        Queue<DataPath> dataPathsQueue = new Queue<DataPath>();
        HashSet<char> drives = [];
        foreach (var dataPath in dataPaths)
        {
            dataPathsQueue.Enqueue(dataPath);
            drives.Add(dataPath.Drive);
        }

        //Create Backup Consumers (Archives)
        BackupStat stats = new();
        List<ArchiveQueue> archiveQueues = [];
        List<BackupArchives> backupArchives = [];
        foreach(char drive in drives)
        {
            ArchiveQueue archiveQueue = new(drive, stats);
            archiveQueues.Add(archiveQueue);
            backupArchives.Add(new BackupArchives(archiveQueue, folderPath));
        }

        BackupShared backupShared = new(dataPathsQueue, archiveQueues);
        PriorBackupPath[] priorBackupsList = [.. priorBackups];

        //Create Backup Produces (Processes)
        List<BackupProcess> backupProcesses = [];
        for (int i = 0; i < backupArchives.Count * 2; i++)//TODO: Change double backup archives
        {
            BackupProcess backupProcess = new(backupShared, priorBackupsList);
            backupProcesses.Add(backupProcess);
        }
        return new(folderPath, backupShared, backupArchives, backupProcesses, priorBackupsList, stats);
    }
    public Result Start(bool logStats = false)
    {
        if (_backupShared.DataPathsIsEmpty())
        {
            return new(ResultType.Info, "No Paths found to be included in backup.");
        }
        if (!SetupDirectory(_folderPath))
        {
            return new(ResultType.Error, $"Setting up the folder: {_folderPath}");
        }

        //Setup Stats Tracking
        DateTime startTime = DateTime.Now;

        CancellationTokenSource cancellationToken = new();

        //Start the Consumers (Archives)
        List<Thread> threadsArchive = new();
        List<Thread> threadsProcess = new();
        foreach (var archive in _backupArchives)
        {
            Thread t = new Thread(() => archive.Start(cancellationToken.Token));
            threadsArchive.Add(t);
            t.Start();
        }

        //Start the Producers (Processes)
        foreach (var process in _backupProcesses)
        {
            Thread t = new Thread(() => process.Start(cancellationToken.Token));
            threadsProcess.Add(t);
            t.Start();
        }
        
        //Wait for complete
        do
        {
            Thread.Sleep(50);
            //Get values
            if (logStats)
            {
                ProgressCount.DrawProgress(_backupStat.CurrentFiles, _backupStat.TotalFiles);
            }

            //Attempt to join process threads
            foreach (Thread t in threadsProcess.ToList())
            {
                if (!t.IsAlive)
                {
                    t.Join();
                    threadsProcess.Remove(t);
                }
            }

            if (threadsProcess.Count == 0)
            {
                //Request normal finish
                cancellationToken.Cancel();
            }

        } while (threadsProcess.Any(t => t.IsAlive) || _backupStat.CurrentFiles < _backupStat.TotalFiles);
        
        if (logStats)
        {
            // Final draw (completed)
            ProgressCount.DrawProgressComplete(_backupStat.CurrentFiles, _backupStat.TotalFiles);
        }

        //Cleanup archive threads
        foreach (var thread in threadsArchive)
        {
            thread.Join();
        }

        //Log Time Taken And BackupStats
        if (logStats)
        {
            GetBackupStats(startTime);
        }

        return new(ResultType.Success);
    }

    //Log Time Taken And BackupStats
    private BackupStat GetBackupStats(DateTime startTime)
    {
        //Retieve Data
        TimeSpan timeTaken = DateTime.Now - startTime;

        //Log
        Logger.Instance.Log(LogLevel.Info, $"Backup Took: {timeTaken.TotalSeconds} seconds");
        Logger.Instance.Log(LogLevel.Info, $"Backup Copied {_backupStat.TotalFiles}");
        Logger.Instance.Log(LogLevel.Info, $"Backup Compressed Size {_backupStat.GetSizeInMegaBytes()} MB");
        return _backupStat;
    }

    public static bool SetupDirectory(string path)
    {
        try
        {
            DirectoryInfo directoryInfo = Directory.CreateDirectory(path);
            return directoryInfo.Exists;
        }
        catch (Exception e)
        {
            Logger.Instance.Log(LogLevel.Error, e.Message + "\n" + e.StackTrace);
            return false;
        }
    }
}
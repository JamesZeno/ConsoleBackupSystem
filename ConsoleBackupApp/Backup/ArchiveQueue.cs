
using System.Collections.Concurrent;

namespace ConsoleBackupApp.Backup;

public class ArchiveQueue(char drive, BackupStat backupStat)
{
    public readonly char Drive = drive;
    public readonly BlockingCollection<string> PathsToCopy = new();
    public readonly BackupStat BackupStat = backupStat;

    public void InsertPath(string fullPath)
    {
        PathsToCopy.Add(fullPath);
        BackupStat.IncrementTotalFile();
    }
}
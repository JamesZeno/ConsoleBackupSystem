using ConsoleBackupApp.Backup;
using ConsoleBackupApp.DataPaths;

namespace ConsoleBackupApp.Backup;
public class BackupShared
{
    private Queue<DataPath> _dataPaths;
    private Mutex _dataPathMutex;
    private List<ArchiveQueue> _archiveQueues;

    public BackupShared(Queue<DataPath> dataPaths, List<ArchiveQueue> archiveQueues)
    {
        _dataPathMutex = new();

        _dataPaths = dataPaths;
        _archiveQueues = archiveQueues;
    }

    public bool DataPathsIsEmpty()
    {
        _dataPathMutex.WaitOne();
        bool result = _dataPaths.Count == 0;
        _dataPathMutex.ReleaseMutex();
        return result;
    } 

    public bool TryGetDataPath(out DataPath dataPath)
    {
        _dataPathMutex.WaitOne();
        if (_dataPaths.Count == 0)
        {
            _dataPathMutex.ReleaseMutex();
            dataPath = default;
            return false;
        }
        else
        {
            dataPath = _dataPaths.Dequeue();
            _dataPathMutex.ReleaseMutex();
            return true;
        }
    }

    internal bool TryGetArchive(char drive, out ArchiveQueue? archive)
    {
        foreach (var archiveQueue in _archiveQueues)
        {
            if (archiveQueue.Drive == drive)
            {
                archive = archiveQueue;
                return true;
            }
        }
        archive = null;
        return false;
    }
}

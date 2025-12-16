namespace ConsoleBackupApp.Backup;

public class BackupStat
{
    private const long MEGABYTE_TO_BYTE = 1_048_576;

    private long _totalSize;
    private int _currentFiles;
    private int _totalFiles;
    
    public long TotalSize => Interlocked.Read(ref _totalSize);
    public int CurrentFiles => Volatile.Read(ref _currentFiles);
    public int TotalFiles => Volatile.Read(ref _totalFiles);



    public void AddFileSize(long bytes)
    {
        Interlocked.Add(ref _totalSize, bytes);
        Interlocked.Increment(ref _currentFiles);
    }

    public void IncrementTotalFile()
    {
        Interlocked.Increment(ref _totalFiles);
    }

    public long GetSizeInMegaBytes()
    {
        return TotalSize / MEGABYTE_TO_BYTE;
    }

}
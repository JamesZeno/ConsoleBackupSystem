using System.IO.Compression;
using ConsoleBackupApp.Logging;
namespace ConsoleBackupApp.Backup;

/// <summary>
/// Consumer of paths that are to be backed up
/// </summary>
public class BackupArchives
{
    private const CompressionLevel COMPRESSION_LEVEL = CompressionLevel.SmallestSize;
    private ZipArchive? _zipArchive;
    private CancellationToken _cancellationToken;
    private Mutex _writeMutex;
    public readonly ArchiveQueue _archive;
    private readonly string _zipFilePath;

    public BackupArchives(ArchiveQueue archiveQueue, string folderPath)
    {
        _writeMutex = new();
        _archive = archiveQueue;
        _zipFilePath = folderPath + _archive.Drive + ".zip";
    }

    /// <summary>
    /// Create a zip file and accepts more parts to be copied to it through the ArchiveQueue
    /// </summary>
    /// <param name="folderPath"></param>
    /// <param name="cancellationToken">if closed early with get everything from the archiveQueue</param>
    public void Start(CancellationToken cancellationToken)
    {
        _writeMutex.WaitOne();
        _cancellationToken = cancellationToken;

        try
        {
            //Create the zip file
            _zipArchive = ZipFile.Open(_zipFilePath, ZipArchiveMode.Create);
        }
        catch (Exception e)
        {
            Logger.Instance.Log(LogLevel.Error, e.Message + "\n" + e.StackTrace);
            return;
        }
        finally
        {
            _writeMutex.ReleaseMutex();
        }
        Consumer();
    }

    private void Consumer()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            if (!_archive.PathsToCopy.TryTake(out string? fullPath) || fullPath is null)
            {
                continue;
            }
            string zipPath = fullPath[3..];
            AddFile(fullPath, zipPath);
        }

        //empty queue the system is closing
        _archive.PathsToCopy.CompleteAdding();
        foreach (string fullPath in _archive.PathsToCopy.GetConsumingEnumerable())
        {
            string zipPath = fullPath[3..];
            AddFile(fullPath, zipPath);
        }

        Close();
    }
    /// <summary>
    /// Add files to the queue be included in the archive
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="entryName"></param>
    /// <exception cref="NullReferenceException"></exception>
    private void AddFile(string filePath, string entryName)
    {
        _writeMutex.WaitOne();
        try
        {
            if (_zipArchive is null)
            {
                Logger.Instance.Log(LogLevel.Error, "Attempted to start archive process before running start.");
                return;
            }
            long originalSize = new FileInfo(filePath).Length;
            ZipArchiveEntry zipArchiveEntry = _zipArchive.CreateEntryFromFile(filePath, entryName, COMPRESSION_LEVEL);
            _archive.BackupStat.AddFileSize(originalSize);
        }
        catch (Exception e)
        {
            Logger.Instance.Log(LogLevel.Error, e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            _writeMutex.ReleaseMutex();
        }
    }

    private void Close()
    {
        try
        {
            _writeMutex.WaitOne();
            _zipArchive?.Dispose();
        }
        catch (Exception e)
        {
            Logger.Instance.Log(LogLevel.Error, e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            _writeMutex.ReleaseMutex();
        }
        _writeMutex.Close();
    }
}
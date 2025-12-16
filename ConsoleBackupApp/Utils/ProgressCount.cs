
namespace ConsoleBackupApp.Utils;

public class ProgressCount
{
    public static void DrawProgress(int current, int total)
    {
        Console.Write($"\r{current} / {total}");
    }

    public static void DrawProgressComplete(int current, int total)
    {
        Console.WriteLine($"\r{current} / {total}");
    }

}
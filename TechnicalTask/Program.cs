using System;
using System.IO;
using System.Security.Cryptography;

public class TechnicalTask
{
    public static async Task Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.Error.WriteLine("Usage: <source> <replica> <interval_seconds> <log_path>");
            return;
        }
        if (!int.TryParse(args[2], out int interval) || interval <= 0)
        {
            Console.Error.WriteLine("Error: Interval must be a positive integer.");
            return;
        }
        string sourceDirName = args[0];
        string replicaDirName = args[1];
        string logPath = args[3];
        try
        {
            if (!ValidatePaths(sourceDirName, replicaDirName, logPath))
            {
                return;
            }
            Directory.CreateDirectory(replicaDirName);
            Log("Synchronization started.", logPath);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error during initialization: {e.Message}");
            return;
        }


        using var cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        try
        {
            while (!token.IsCancellationRequested)
            {

                var (changesCount, errorsCount) = FileSynchronize(sourceDirName, replicaDirName,logPath);
                if (changesCount == 0 && errorsCount == 0)
                {
                    Log("Synchronization completed. No changes detected.", logPath);
                }
                else if(changesCount > 0 && errorsCount ==0)
                {
                    Log($"Synchronization completed. {changesCount} operations performed.",logPath);

                }
                else
                {
                    Log($"Synchronization completed with errors. Operations: {changesCount}, Errors: {errorsCount}", logPath);
                }
                await Task.Delay(TimeSpan.FromSeconds(interval), token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Log("Synchronization stopped.", logPath);
        }
        catch (IOException e)
        {
            Console.Error.WriteLine($"I/O error: {e.Message}");
        }
        catch (UnauthorizedAccessException e)
        {
            Console.Error.WriteLine($"Access denied: {e.Message}");
        }
    }

    static bool ValidatePaths(string sourceDirName, string replicaDirName, string logPath)
    {
        if (string.IsNullOrWhiteSpace(sourceDirName) || string.IsNullOrWhiteSpace(replicaDirName) || string.IsNullOrWhiteSpace(logPath))
        {
            Console.Error.WriteLine("Error: Paths cannot be empty.");
            return false;
        }
        var source = Path.GetFullPath(sourceDirName);
        var replica = Path.GetFullPath(replicaDirName);
        if (!Directory.Exists(source))
        {
            Console.Error.WriteLine("Error: Source directory does not exist.");
            return false;
        }
        if (Directory.Exists(Path.GetFullPath(logPath)))
        {
            Console.Error.WriteLine("Error: Log path must point to a file, not a directory.");
            return false;
        }
        source = Path.TrimEndingDirectorySeparator(source);
        replica = Path.TrimEndingDirectorySeparator(replica);

        if (string.Equals(source, replica, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Error: Path of the source and replica must be different.");
            return false;
        }
        if (!Path.EndsInDirectorySeparator(source))
            source += Path.DirectorySeparatorChar;

        if (!Path.EndsInDirectorySeparator(replica))
            replica += Path.DirectorySeparatorChar;

        if (replica.StartsWith(source, StringComparison.OrdinalIgnoreCase) || source.StartsWith(replica, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Error: The paths to the source and replica must not be nested within each other.");
            return false;
        }

        var log = Path.GetDirectoryName(Path.GetFullPath(logPath));

        if (log == null)
        {
            Console.Error.WriteLine("Error: Invalid log file path.");
            return false;
        }

        if (!Path.EndsInDirectorySeparator(log))
            log += Path.DirectorySeparatorChar;


        if (log.StartsWith(source, StringComparison.OrdinalIgnoreCase) || log.StartsWith(replica, StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("Error: The log file must be stored separately from the source and replica files.");
            return false;
        }

        return true;
    }
    
    static (int changesCount, int errorsCount) FileSynchronize(string sourceDirName, string replicaDirName, string logPath)
    {
        int changesCount = 0 , errorsCount = 0;
        string[] source;
        string[] sourceFolders;
        try
        {
            source = Directory.GetFiles(sourceDirName, "*.*", SearchOption.AllDirectories);
            sourceFolders = Directory.GetDirectories(sourceDirName, "*", SearchOption.AllDirectories);
        }
        catch(Exception e)
        {
            Log($"ERROR: {e.Message}", logPath);
            errorsCount++;
            return (changesCount, errorsCount);
        }

        foreach (var item in sourceFolders)
        {
            string itemReplica = Path.Combine(replicaDirName, Path.GetRelativePath(sourceDirName, item));
            if (File.Exists(itemReplica))
            {
                try
                {
                    File.Delete(itemReplica);
                    Log($"File removed: {itemReplica}", logPath);
                    changesCount++;
                }
                catch (Exception e)
                {
                    Log($"ERROR: {e.Message}", logPath);
                    errorsCount++;
                }
            }

            if (!Directory.Exists(itemReplica))
            {
                try
                {
                    Directory.CreateDirectory(itemReplica);
                    Log($"Directory created: {itemReplica}", logPath);
                    changesCount++;
                }
                catch (Exception e)
                {
                    Log($"ERROR: {e.Message}", logPath);
                    errorsCount++;
                }
            }
        }

        for (int i = 0; i < source.Length; i++)
        {
            try
            {
                var file = Path.GetRelativePath(sourceDirName, source[i]);
                string fileReplica = Path.Combine(replicaDirName, file);

                if (File.Exists(fileReplica))
                {
                    if (!FilesAreEqual_Hash(new FileInfo(fileReplica), new FileInfo(source[i])))
                    {
                        File.Copy(source[i], fileReplica, true);
                        Log($"File overwritten: {fileReplica}", logPath);
                        changesCount++;
                    }
                }
                else
                {
                    if (Directory.Exists(fileReplica)) 
                    { 
                        Directory.Delete(fileReplica, true);
                        Log($"Directory removed: {fileReplica}", logPath);
                        changesCount++;
                    }
                    File.Copy(source[i], fileReplica, true);
                    Log($"File added: {fileReplica}", logPath);
                    changesCount++;
                }

            }
            catch (Exception e)
            {
                Log($"ERROR: {e.Message}", logPath);
                errorsCount++;
            }
        }

        if (errorsCount > 0)
        {
            Log("WARNING: Cleanup skipped because errors occurred during synchronization.", logPath);
            return (changesCount, errorsCount);
        }
        string[] replica;
        try
        {
            replica = Directory.GetFiles(replicaDirName, "*.*", SearchOption.AllDirectories);
        }
        catch (Exception e)
        {
            Log($"ERROR: {e.Message}", logPath);
            errorsCount++;
            return (changesCount, errorsCount);
        }



        foreach (var replicafile in replica)
        {
            try
            {
                string sourceFile = Path.Combine(sourceDirName, Path.GetRelativePath(replicaDirName, replicafile));
                if (!File.Exists(sourceFile))
                {
                    File.Delete(replicafile);
                    Log($"File removed: {replicafile}", logPath);
                    changesCount++;
                }
            }
            catch (Exception e)
            {
                Log($"ERROR: {e.Message}", logPath);
                errorsCount++;
            }
        }
        if (errorsCount > 0)
        {
            Log("WARNING: Directory cleanup skipped because file operations failed.", logPath);
            return (changesCount, errorsCount);
        }
        string[] replicaFolders;
        try
        {
            replicaFolders = Directory.GetDirectories(replicaDirName, "*", SearchOption.AllDirectories).OrderByDescending(x => x.Length).ToArray();
        }
        catch (Exception e)
        {
            Log($"ERROR: {e.Message}", logPath);
            errorsCount++;
            return (changesCount, errorsCount);
        }

        foreach (var item in replicaFolders)
        {
            string itemsource = Path.Combine(sourceDirName, Path.GetRelativePath(replicaDirName, item));
            if (!Directory.Exists(itemsource))
            {
                try
                {
                    Directory.Delete(item, false);
                    Log($"Directory removed: {item}", logPath);
                    changesCount++;
                }
                catch (Exception e)
                {
                    Log($"ERROR: {e.Message}", logPath);
                    errorsCount++;
                }
            }
        }
        return (changesCount, errorsCount);
    }

    static void Log(string message, string logPath)
    {
        var log = Path.GetFullPath(logPath);
        var logDir = Path.GetDirectoryName(log);
        if (logDir != null)
        {
            Directory.CreateDirectory(logDir);
        }
        string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string logEntry = $"[{date}] {message}";
        File.AppendAllText(log, logEntry + Environment.NewLine);
        Console.WriteLine(logEntry);
    }


    static bool FilesAreEqual_Hash(FileInfo first, FileInfo second)
    {
        if (first.Length != second.Length)
            return false;

        using FileStream firststream = first.OpenRead();
        using FileStream secondstream = second.OpenRead();
        using SHA256 sha256 = SHA256.Create();

        byte[] firstHash = sha256.ComputeHash(firststream);
        byte[] secondHash = sha256.ComputeHash(secondstream);

        return firstHash.SequenceEqual(secondHash);
    }

}
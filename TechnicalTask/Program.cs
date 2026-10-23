using System;
using System.IO;
using System.Collections;
using System.Security.Cryptography;
using System.Net.WebSockets;

public class TechnicalTask
{
    public static string sourceDirName = "\\source"; // путь к папке 
    public static string replicaDirName = "\\replica";

    public static void Main(string[] args)
    {
        var source = Directory.GetFiles(sourceDirName, "*.*", SearchOption.AllDirectories);
        var sourcefoldes = Directory.GetDirectories(sourceDirName, "*", SearchOption.AllDirectories);
        
        foreach (var item in sourcefoldes)
        {
            string itemReplica = Path.Combine(replicaDirName,Path.GetRelativePath(sourceDirName, item));
            if (File.Exists(itemReplica))
                File.Delete(itemReplica);

            Directory.CreateDirectory(itemReplica);
            Console.WriteLine($"The directory was create: {itemReplica}");
        }

        for (int i = 0; i < source.Length; i++)
        {
            try
            {
                var file = Path.GetRelativePath(sourceDirName, source[i]); 
                string fileReplica = Path.Combine(replicaDirName, file);

                if (File.Exists(fileReplica)) {
                    if (!FilesAreEqual_Hash(new FileInfo(fileReplica), new FileInfo(source[i])))
                    {
                        File.Copy(source[i], fileReplica,true);
                        Console.WriteLine($"The file has been overwritten: {fileReplica}");

                    }
                }
                else
                {
                    if (Directory.Exists(fileReplica)) {  Directory.Delete(fileReplica, true); }

                    File.Copy(source[i], fileReplica,true);
                    Console.WriteLine($"The file has been added: {fileReplica}");

                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        var repilca = Directory.GetFiles(replicaDirName, "*.*", SearchOption.AllDirectories);

        foreach (var replicafile in repilca)
        {
           try
           {
               string fileSorce = Path.Combine(sourceDirName, Path.GetRelativePath(replicaDirName, replicafile));
               if (!File.Exists(fileSorce))
               {
                    Console.WriteLine($"The file was removed: {replicafile}");
                    File.Delete(replicafile);
                }
            }
           catch (Exception e)
           {
               Console.WriteLine(e.Message);
           }
        }
        var repilcafoldes = Directory.GetDirectories(replicaDirName, "*", SearchOption.AllDirectories).OrderByDescending(x => x.Length);

        foreach (var item in repilcafoldes)
        {
            string itemsource = Path.Combine(sourceDirName, Path.GetRelativePath(replicaDirName, item));
            if (!Directory.Exists(itemsource))
            {
                Console.WriteLine($"The directory was Delete: {item}");
                Directory.Delete(item, false);
                
            }
        }
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
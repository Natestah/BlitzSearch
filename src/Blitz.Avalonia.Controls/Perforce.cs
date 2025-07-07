using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Blitz.Avalonia.Controls;

/// <summary>
/// Basic Perforce Checkout support for handling Read-Only files.
/// </summary>
public static class Perforce
{
    public static bool IsDetectPerforceCommandLineInstalled()
    {
        string environmentPath = Environment.GetEnvironmentVariable("PATH")!;
        string[] paths = environmentPath!.Split(';');

        foreach (string thisPath in paths)
        {
            try
            {
                if (Directory.Exists(thisPath))
                {
                    string test = Path.Combine(thisPath, "p4.exe");
                    if (File.Exists(test))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing directory {thisPath}: {ex.Message}");
            }
        }

        return false;
    }
    
    public static bool EditBatch(IEnumerable<string> files)
    {
        string fileList = CreateTempP4FileList(files);
    
        var procesStartInfo = new ProcessStartInfo("p4.exe")
        {
            CreateNoWindow = true,
            Arguments = $"-x {fileList} edit",
            UseShellExecute = true,
        };

        var process = Process.Start(procesStartInfo);
        process?.WaitForExit();

        foreach (var file in files)
        {
            if (new FileInfo(file).IsReadOnly)
            {
                return false;
            }
        }
        return true;
    }
    
    private static string CreateTempP4FileList(IEnumerable<string> files)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "blitz_p4");
        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }
        var tempFileName = Path.Combine(tempPath, "blitz_temp.txt");
        using StreamWriter writer = new StreamWriter(tempFileName);
        foreach (var file in files)
        {
            writer.WriteLine(file);    
        }
        return tempFileName;   
    }
}
using System;
using System.Collections.Generic;
using System.IO;

namespace Blitz.Avalonia.Controls;

public class PoorMansIPC
{
    public static PoorMansIPC Instance = new PoorMansIPC();

    private Dictionary<string, Action<string>> _actions = new Dictionary<string, Action<string>>();

    private FileSystemWatcher _fileSystemWatcher;

    public PoorMansIPC()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var specificFolder = Path.Combine(folder, "NathanSilvers", "POORMANS_IPC");
        Directory.CreateDirectory(specificFolder);
         
         var watcher = new FileSystemWatcher(specificFolder, "*");
         watcher.EnableRaisingEvents = true;
         watcher.Created += WatcherOnCreated;
         watcher.Renamed += WatcherOnRenamed;
         watcher.Deleted += WatcherOnDeleted;
         watcher.Changed += WatcherOnChanged;
         _fileSystemWatcher = watcher;
    }

    public bool GetSolutionRecord(SolutionID searchingSolutionId, out string path)
    {
        return GetCommandPathFromSolutionID(searchingSolutionId,"VS_SOLUTION" , out path);
       }

    
    public bool GetCommandPathFromSolutionID(SolutionID searchingSolutionId, string command, out string path)
    {
        foreach (var solutionId in GetSolutionTitles())
        {
            if (solutionId.Equals(searchingSolutionId))
            {
                path = Path.Combine(_fileSystemWatcher.Path, $"{command},{solutionId.Title},{solutionId.Identity}.txt");
                return File.Exists(path);
            }
        }
        path = string.Empty;
        return false;
    }
    public IEnumerable<SolutionID> GetSolutionTitles()
    {
        string solutionCommand = $"VS_SOLUTION";
        foreach (var file in Directory.EnumerateFiles(_fileSystemWatcher.Path, solutionCommand+"*.txt"))
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            var split = fileNameWithoutExtension.Split(',');
            if (split.Length == 3)
            {
                yield return new SolutionID{Title = split[1],Identity = split[2]};
            }
        }
    }

    public void RegisterAction(string name, Action<string> action)
    {
        _actions[name] = action;
    }

    public void ExecuteNamedAction(string name)
    {
        if (!_actions.TryGetValue(name, out var action))
        {
            return;
        }
        string filePath = Path.Combine(_fileSystemWatcher.Path, $"{name}.txt");
        if (!File.Exists(filePath))
        {
            return;
        }
        string text = File.ReadAllText(filePath);
        action.Invoke(text);
    }

    private void DoActionWithFile(string fullFilename)
    {
        try
        {
            string action = Path.GetFileNameWithoutExtension(fullFilename).ToUpper();
            if (!File.Exists(fullFilename))
            {
                return;
            }
            
            if (action.Contains(','))
            {
                var split = action.Split(',');
                if (split.Length != 3)
                {
                    //action,solutionname,solutionIdentityMD5
                    return;
                }
                action = split[0];
            }

            if (!_actions.TryGetValue(action, out var function))
            {
                return;
            }
            
            Exception? lastException = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using var file = new FileStream(fullFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var streamReader = new StreamReader(file);
                    var text = streamReader.ReadToEnd();
                    function.Invoke(text);
                    return;

                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            if (lastException != null)
            {
                Console.WriteLine(lastException);
            }

        }
        catch (Exception e)
        {
            //Need a box for the message,  https://github.com/Natestah/BlitzSearch/issues/85
            Console.WriteLine(e);
        }
    }

    private void WatcherOnChanged(object sender, FileSystemEventArgs e)
    {
        DoActionWithFile(e.FullPath);
    }

    private void WatcherOnDeleted(object sender, FileSystemEventArgs e)
    {
        DoActionWithFile(e.FullPath);
    }

    private void WatcherOnRenamed(object sender, RenamedEventArgs e)
    {
        DoActionWithFile(e.FullPath);
    }

    private void WatcherOnCreated(object sender, FileSystemEventArgs e)
    {
        DoActionWithFile(e.FullPath);
    }


    public void ExecuteWithin(DateTime utcNow, TimeSpan withinTime)
    {
        foreach (var file in Directory.EnumerateFiles(_fileSystemWatcher.Path))
        {
            var lastModified = File.GetLastWriteTimeUtc(file);
            if (utcNow - lastModified < withinTime)
            {
                DoActionWithFile(file);
            }
        }
    }
}
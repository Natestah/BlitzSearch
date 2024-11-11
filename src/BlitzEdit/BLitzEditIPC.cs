using System;
using System.Collections.Generic;
using System.IO;

namespace BlitzEdit;


/// <summary>
/// Basic IPC, Watching a filesystem for changes
/// </summary>
public class BLitzEditIPC
{
    public static BLitzEditIPC Instance = new BLitzEditIPC();

    private Dictionary<string, Action<string>> _actions = new Dictionary<string, Action<string>>();

    private FileSystemWatcher _fileSystemWatcher;

    /// <summary>
    /// Returns folder for Blitz Edit.
    /// </summary>
    /// <returns></returns>
    public static string GetFolder()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var specificFolder = Path.Combine(folder, "NathanSilvers", "POORMANS_IPC");
        Directory.CreateDirectory(specificFolder);
        return specificFolder;
    }
    
    public BLitzEditIPC()
    {
        var specificFolder = GetFolder();         
         var watcher = new FileSystemWatcher(specificFolder, "*");
         watcher.EnableRaisingEvents = true;
         watcher.Created += WatcherOnCreated;
         watcher.Renamed += WatcherOnRenamed;
         watcher.Deleted += WatcherOnDeleted;
         watcher.Changed += WatcherOnChanged;
         _fileSystemWatcher = watcher;
    }

    /// <summary>
    /// Register an Action, who's string is the full Text contents of the file used to communicate, this can be JSON, or whatever serialization you chose 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="action"></param>
    public void RegisterAction(string name, Action<string> action)
    {
        _actions[name] = action;
    }

    private void DoActionWithFile(string fullFilename)
    {
        try
        {
            string action = Path.GetFileNameWithoutExtension(fullFilename).ToUpper();
            if (_actions.TryGetValue(action, out var function))
            {
                function.Invoke(File.ReadAllText(fullFilename));
            }
        }
        catch (Exception e)
        {
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
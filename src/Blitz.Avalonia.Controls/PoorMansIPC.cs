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
            if (_actions.TryGetValue(action, out var function))
            {
                function.Invoke(File.ReadAllText(fullFilename));
            }

        }
        catch (Exception e)
        {
            //Todo Message box.
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
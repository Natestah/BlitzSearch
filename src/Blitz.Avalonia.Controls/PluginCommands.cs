using System;
using System.Collections.Generic;
using System.IO;
using Blitz.Avalonia.Controls.ViewModels;

namespace Blitz.Avalonia.Controls;


/// <summary>
/// Plugin Commands acts as a shared Configuration folder between the various IDE/Plugins and Blitz Search,
/// it is used to communicate and store commands and context from each IDE to Blitz Search
/// Commands Deposited to filesystem, do not need to deal with the trappings of connections, ports and things.
/// A File System Watcher is installed to update Blitz Search when those things change.
/// </summary>
public class PluginCommands
{

    // Command Names Shared with Plugins, string changes need to be reflected in Plugin code.
    public const string SetSearch = "SET_SEARCH";  
    public const string SetReplace = "SET_REPLACE";  
    public const string SetContextSearch = "SET_CONTEXT_SEARCH";  
    public const string SetContextReplace = "SET_CONTEXT_REPLACE";  
    public const string UpdateVisualStudioSolution = "VS_SOLUTION";  
    public const string SetTheme = "SET_THEME";  
    public const string SetThemeLight = "SET_THEME_LIGHT";  
    public const string VisualStudioCodeWorkspaceUpdate = "WORKSPACE_UPDATE";  
    public const string SublimeTextWorkspaceUpdate = "SUBLIME_TEXT_WORKSPACE";  
    public const string UpdateVisualStudioProject = "VS_PROJECT";  
    public const string UpdateVisualStudioActiveFiles = "VS_ACTIVE_FILES";
    public static string SimpleFolderSearch = "SIMPLE_FOLDER_SEARCH";
    
    private Dictionary<string, Action<string>> _actions = new Dictionary<string, Action<string>>();

    private FileSystemWatcher _fileSystemWatcher;

    public static string GetCommandsFolder()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var specificFolder = Path.Combine(folder, "NathanSilvers", "POORMANS_IPC");
        Directory.CreateDirectory(specificFolder);
        return specificFolder;
    }
    public PluginCommands()
    {
        var specificFolder = GetCommandsFolder();
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
        return GetCommandPathFromSolutionId(searchingSolutionId, UpdateVisualStudioSolution, out path);
    }
    
    public bool GetCommandPathFromSolutionId(SolutionID searchingSolutionId, string command, out string path)
    {
        foreach (var solutionId in GetSolutionIDsForCommands(command))
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
        return GetSolutionIDsForCommands(UpdateVisualStudioSolution);
    }
    
    public IEnumerable<SolutionID> GetSolutionIDsForCommands(string command)
    {
        var di = new DirectoryInfo(_fileSystemWatcher.Path);
        FileInfo[] files = di.GetFiles(@command+"*.txt");
        Array.Sort(files, (x, y) => Comparer<DateTime>.Default.Compare(x.LastWriteTime, y.LastWriteTime));
        foreach (var file in files)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FullName);
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
    
    public string GetCommandPath(string name) => Path.Combine(_fileSystemWatcher.Path, $"{name}.txt");

    public void ExecuteNamedAction(string name)
    {
        if (!_actions.TryGetValue(name, out var action))
        {
            return;
        }

        string filePath = GetCommandPath(name);
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
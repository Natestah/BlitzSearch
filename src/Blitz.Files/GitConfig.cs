using System.ComponentModel;
using System.Diagnostics;

namespace Blitz.Files;

/// <summary>
/// Helper class for Managing Git Config Details, git commands are issued directly and results cached.
/// A watcher is installed to watch for changes and invalidate cached results.
/// </summary>
public class GitConfig
{
    private readonly FileSystemWatcher? _gitConfigWatcher;
    private FileSystemWatcher? _globalIgnoreWatcher;
    private string? _gitConfigCachedPath = null;
    public event EventHandler? GitConfigChanged;

    public GitConfig()
    {
        var gitConfigPath = Environment.GetEnvironmentVariable("userprofile")!;
        _gitConfigWatcher = CreateConfigResetWatcher(gitConfigPath, ".gitconfig");
        Task.Run(InstallIgnoreWatcher);
    }

    private FileSystemWatcher? CreateConfigResetWatcher(string path, string filter)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        var watcher = new FileSystemWatcher(path, filter);
        watcher.EnableRaisingEvents = true;
        watcher.Created += WatcherResetsConfigCache;
        watcher.Renamed += WatcherResetsConfigCache;
        watcher.Deleted += WatcherResetsConfigCache;
        watcher.Changed += WatcherResetsConfigCache;
        return watcher;
    }

    void InstallIgnoreWatcher()
    {
        string? globalIgnorePath = GetGlobalGitIgnoreFile();
        var directory = Path.GetDirectoryName(globalIgnorePath);
        if (directory == null)
        {
            return;
        }
        _globalIgnoreWatcher = CreateConfigResetWatcher(directory, ".gitignore");
    }
    
    public static GitConfig Instance { get; } = new GitConfig();

    private void WatcherResetsConfigCache(object sender, FileSystemEventArgs e)
    {
        _gitConfigCachedPath = null;
        GitConfigChanged?.Invoke(this, EventArgs.Empty);
    }
    
    public string GetGlobalGitIgnoreFile()
    {
        if (_gitConfigCachedPath != null)
        {
            return _gitConfigCachedPath;
        }
        try
        {
            var p = Process.Start(
                new ProcessStartInfo("git", "config --global core.excludesFile")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = Environment.CurrentDirectory
                }
            );
            if (p == null)
            {
                return _gitConfigCachedPath = string.Empty;
            }
            p.WaitForExit();
            _gitConfigCachedPath =p.StandardOutput.ReadToEnd().TrimEnd();
            var errorInfoIfAny =p.StandardError.ReadToEnd().TrimEnd();

            if (errorInfoIfAny.Length != 0)
            {
                Console.WriteLine($"error: {errorInfoIfAny}");
            }
            else
            {
                return _gitConfigCachedPath;
            }

        }
        catch (Win32Exception)
        {
            // It's ok that Git is not found.
            return _gitConfigCachedPath = string.Empty;
        }
    
    
        return _gitConfigCachedPath =  string.Empty;
    }
}
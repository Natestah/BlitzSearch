using System.Collections.Concurrent;
using Blitz.Interfacing;

namespace Blitz.Files;

public class FileDiscoveryPath
{
    private readonly CancellationTokenSource _cancelPopulateToken;
    private readonly FileDiscovery _fileDiscovery;
    private readonly FileSystemWatcher? _watcher;
    private readonly SearchPath _path;

    private FileDiscoveryPath? _rootFileDiscoveryPath;
    public string? Folder => _path.Folder;

    public bool TopLevelOnly => _path.TopLevelOnly;
    public FileDiscoveryPath(FileDiscovery fileDiscovery,SearchPath path, CancellationTokenSource cancellationTokenSource, FileDiscoveryPath? rootFileDiscoveryPath)
    {
        _path = path;
        _fileDiscovery = fileDiscovery;
        _cancelPopulateToken = cancellationTokenSource;
        if (_path.Folder == null)
        {
            throw new NullReferenceException();
        }
        _rootFileDiscoveryPath = rootFileDiscoveryPath;

        if (_rootFileDiscoveryPath == null)
        {
            _watcher = InstallRootWatcher();
            _globalIgnorePath = InstallGlobalGitIgnorePath();
            _rootFileDiscoveryPath = this;
        }
    }

    public void UpdateGlobalIgnorePath()
    {
        _globalIgnorePath = InstallGlobalGitIgnorePath();;
    }

    private FileSystemWatcher? InstallRootWatcher()
    {
        //root folders get watchers
        if (_path.Folder != null)
        {
            var watcher = new FileSystemWatcher(_path.Folder, "*");
            watcher.IncludeSubdirectories = !_path.TopLevelOnly;
            watcher.EnableRaisingEvents = true;
            watcher.Created += WatcherOnCreated;
            watcher.Renamed += WatcherOnRenamed;
            watcher.Deleted += WatcherOnDeleted;
            watcher.Changed += WatcherOnChanged;
            watcher.Error += WatcherOnError;
            return watcher;
        }

        return null;
    }

    private IgnorePath InstallGlobalGitIgnorePath()
    {
        var currentGlobalIgnoreFile = GitConfig.Instance.GetGlobalGitIgnoreFile();
        var ignorePath = new IgnorePath(currentGlobalIgnoreFile, _path.Folder);
        ignorePath.ParseIgnore();
        return ignorePath;
    }

    public void DisposeWatcher()
    {
        if (_watcher == null)
        {
            return;
        }
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
    }


    private bool IsFileHiddenOrBlocked(string fileOrDirectory)
    {
        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(fileOrDirectory);
        }
        catch (FileNotFoundException e)
        {
            //File.GetAttribues is claiming this exception but the stack has a check prior.
            // I am guessing the correct answer here. which is to say it's hidden.
            return false;
        }
        
        return (attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
    }

    public bool IsHiddenOrNonAccessible(string fileOrDirectory)
    {
        if (_path.Folder == null)
        {
            return false;
        }

        if (IsFileHiddenOrBlocked(fileOrDirectory))
        {
            return true;
        }
        
        var directory = Path.GetDirectoryName(fileOrDirectory);
        while (!string.IsNullOrEmpty(directory))
        {
            if (directory.Length <= _path.Folder.Length)
            {
                break;
            }
            if (IsFileHiddenOrBlocked(directory))
            {
                return true;
            }
            directory = Path.GetDirectoryName(directory);
        }
        return false;
    }
    
    
    public void DiscoverFiles(FileDiscoveryTask task, ConcurrentQueue<FileDiscoveryTask> taskBag, CancellationTokenSource cancellationTokenSource)
    {   
        var path = task.DiscoveryPath._path;
        var ignoreStack = task.IgnoreStack;
        var blitzIgnoreStack = task.BlitzIgnoreStack;

        if (string.IsNullOrEmpty(path.Folder))
        {
            return;
        }
        if (!Directory.Exists(path.Folder))
        {
            return;
        }
        

        bool discoveredIgnore = _fileDiscovery.DiscoverAndParseIgnore(path.Folder, false, ignoreStack);
        bool discoveredBlitzIgnore = _fileDiscovery.DiscoverAndParseIgnore(path.Folder, true, blitzIgnoreStack);


        if (cancellationTokenSource.IsCancellationRequested) return;
        try
        {
            foreach (var fileName in Directory.EnumerateFiles(path.Folder, "*", SearchOption.TopDirectoryOnly))
            {
                if (cancellationTokenSource.IsCancellationRequested) return;
                if (_fileDiscovery.UseGitIgnore && ignoreStack.Any(ignoreInstance => ignoreInstance.IsIgnored(fileName)))
                {
                    continue;
                }

                if (_fileDiscovery.UseGlobalIgnore && IsGlobalIgnored(fileName))
                {
                    continue;
                }
            

                if (_fileDiscovery.UseBlitzIgnore && blitzIgnoreStack.Any(ignoreInstance => ignoreInstance.IsIgnored(fileName)))
                {
                    continue;
                }
                _fileDiscovery.UpdateCreatedFile(fileName, false);
            }
            
            if (!path.TopLevelOnly)
            {
            
                foreach (var directory in Directory.EnumerateDirectories(path.Folder, "*", SearchOption.TopDirectoryOnly))
                {
                    if (IsHiddenOrNonAccessible(directory))
                    {
                        continue;
                    }
                    var searchPath = new SearchPath { Folder = directory, TopLevelOnly = false };
                    var rootPath = new FileDiscoveryPath(_fileDiscovery, searchPath, _cancelPopulateToken, _rootFileDiscoveryPath);
                    taskBag.Enqueue(new FileDiscoveryTask(rootPath, new Stack<IgnorePath>(ignoreStack) , new Stack<IgnorePath>(blitzIgnoreStack)));
                }
            }

        }
        catch (UnauthorizedAccessException e)
        {
            _fileDiscovery.RegisterException(e);
        }

        
        if (discoveredIgnore)
        {
            ignoreStack.Pop();
        }

        if (discoveredBlitzIgnore)
        {
            blitzIgnoreStack.Pop();
        }
    }

    private void WatcherOnError(object sender, ErrorEventArgs e)
    {
        _fileDiscovery.ResetFilePopulation();
    }

    private void WatcherOnDeleted(object sender, FileSystemEventArgs e)
    {
        _fileDiscovery.UpdateDeletedFile(e.FullPath);
    }

    private void WatcherOnRenamed(object sender, RenamedEventArgs e)
    {
        Task.Run(() => 
        {
            _fileDiscovery.UpdateDeletedFile(e.OldFullPath);
            if (IsIgnored(e.FullPath))
            {
                return;
            }
            _fileDiscovery.UpdateCreatedFile(e.FullPath);
        },_cancelPopulateToken.Token);
    }

    private void WatcherOnCreated(object sender, FileSystemEventArgs e)
    {
        _fileDiscovery.UpdateCreatedFile(e.FullPath);
        Task.Run(() => 
        { 
            if (!IsIgnored(e.FullPath))
            {
                var extension = Path.GetExtension(e.FullPath);
                _fileDiscovery.UpdateCreatedFile(e.FullPath);
            }
        },_cancelPopulateToken.Token);
    }

    
    private void WatcherOnChanged(object? sender, FileSystemEventArgs e)
    {
        Task.Run(() =>
        {
            if (IsIgnored(e.FullPath))
            {
                return;
            }
            _fileDiscovery.NotifyFileChanged(e.FullPath);
        },_cancelPopulateToken.Token);
    }
    public bool IsIgnored(string filename)
    {
        if (_fileDiscovery.UseGitIgnore && IsIgnored(filename, false) )
        {
            return true;
        }

        if (_fileDiscovery.UseBlitzIgnore && IsIgnored(filename, true))
        {
            return true;
        }

        return _fileDiscovery.UseGlobalIgnore && IsGlobalIgnored(filename);
    }

    public bool IsIgnored(string fileName, bool isBlitzIgnore)
    {
        if (!isBlitzIgnore && !_fileDiscovery.UseGitIgnore)
        {
            return false;
        }

        if (isBlitzIgnore && !_fileDiscovery.UseBlitzIgnore)
        {
            return false;
        }
        
        
        var paths = new Stack<IgnorePath>();
        var directory = Path.GetDirectoryName(fileName);
        while (!string.IsNullOrEmpty(directory))
        {
            if (_cancelPopulateToken.Token.IsCancellationRequested)
            {
                return false;
            }
            _fileDiscovery.DiscoverAndParseIgnore(directory, isBlitzIgnore, paths);
            directory = Path.GetDirectoryName(directory);
        }

        return paths.Any(ignoreInstance => ignoreInstance.IsIgnored(fileName));
    }

    private IgnorePath? _globalIgnorePath;

    private bool IsGlobalIgnored(string fileName)
    {
        return _rootFileDiscoveryPath?._globalIgnorePath?.IsIgnored(fileName) ?? false;
    }

    public bool ContainsFile(string fileName)
    {
        if (Folder == null)
        {
            return false;
        }
        var pathFolder = Folder.TrimEnd('\\');
        if (!fileName.StartsWith(pathFolder, StringComparison.OrdinalIgnoreCase) ||
            pathFolder.Length >= fileName.Length
            || (fileName[pathFolder.Length] != '\\' && fileName[pathFolder.Length] != '/'))
        {
            return false;
        }
        if (!TopLevelOnly)
        {
            return true;
        }
        var startIndex = pathFolder.Length + 1; 
        return fileName.IndexOf(Path.DirectorySeparatorChar,startIndex) == -1
               && fileName.IndexOf(Path.AltDirectorySeparatorChar,startIndex) == -1;
    }
}
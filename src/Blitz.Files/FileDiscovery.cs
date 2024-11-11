using System.Collections.Concurrent;
using Blitz.Interfacing;

namespace Blitz.Files;

public class FileDiscovery
{
    public EventHandler<string>? FileChanged { get; set; }
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _filesByExtension = new();
    private readonly object _populationTaskLockObject = new();
    private bool _resetPopulation;
    private Task _enumerateDirectoriesTask = Task.CompletedTask;
    private readonly IEnumerable<SearchPath> _rootPaths;
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly bool _useGitIgnore;
    private readonly CancellationTokenSource _cancelPopulateToken = new();

    /// <summary>
    /// Keeps files in memory, using a filesystem watcher to keep files up to date.
    /// </summary>
    /// <param name="rootPaths">Collection of SearchPaths used for Defining roots of the FileDiscovery</param>
    /// <param name="useGitIgnore">Use .gitignore files to ignore in results, Also can save memory</param>
    public FileDiscovery(IEnumerable<SearchPath> rootPaths, bool useGitIgnore)
    {
        _rootPaths = rootPaths;
        _useGitIgnore = useGitIgnore;
        _isFileDiscoveryWorking = false;
        _isFileDiscoveryFinished = false;
        InstallWatchers();
        _ = PopulateFiles();
    }

    public ConcurrentBag<ExceptionResult> ExceptionResults { get; } = [];

    private async Task PopulateFiles()
    {
        lock (_populationTaskLockObject)
        {
            if (!_enumerateDirectoriesTask.IsCompleted)
            {
                _resetPopulation = true;
                return;
            }

            _resetPopulation = false;
            _enumerateDirectoriesTask = Task.Run(PopulateFilesJob, _cancelPopulateToken.Token);
        }

        await _enumerateDirectoriesTask;
    }


    public bool IsIgnored(string filename)
    {
        if (!_useGitIgnore)
        {
            return false;
        }
        return IsIgnored(filename, false) || IsIgnored(filename, true);
    }
    
    public bool IsIgnored(string fileName, bool isBlitzIgnore)
    {
        if (!_useGitIgnore)
        {
            return false;
        }
        var paths = new Stack<IgnorePath>();
        var directory = Path.GetDirectoryName(fileName);
        while (!string.IsNullOrEmpty(directory))
        {
            DiscoverAndParseIgnore(directory, isBlitzIgnore, paths);
            directory = Path.GetDirectoryName(directory);
        }
        return paths.Any(ignoreInstance => ignoreInstance.IsIgnored(fileName));
    }
    
    private bool DiscoverAndParseIgnore(string folder,bool isBlitzIgnore, Stack<IgnorePath> ignoreStack)
    {
        if (!_useGitIgnore)
        {
            return false;
        }
        var dictionary = isBlitzIgnore ? _blitzIgnoreCache : _ignoreCache;
        var ignoreFile = isBlitzIgnore ? ".blitzIgnore" : ".gitignore";
        var tryIgnorePath = Path.Combine(folder, ignoreFile);
        if (!Path.Exists(tryIgnorePath))
        {
            return false;
        }
        var ignorePath = dictionary.GetOrAdd(folder, _ => new IgnorePath(tryIgnorePath));
        if (!ignorePath.ParseIgnore(tryIgnorePath))
        {
            return false;
        }
        ignoreStack.Push(ignorePath);
        return true;
    }

    private ConcurrentDictionary<string, IgnorePath> _ignoreCache = new ConcurrentDictionary<string, IgnorePath>();
    private ConcurrentDictionary<string, IgnorePath> _blitzIgnoreCache = new ConcurrentDictionary<string, IgnorePath>();


    private void DiscoverFiles(FileDiscoveryTask task, ConcurrentQueue<FileDiscoveryTask> taskBag, CancellationTokenSource cancellationTokenSource)
    {
        var path = task.Path;
        var ignoreStack = task.IgnoreStack;
        var blitzIgnoreStack = task.BlitzIgnoreStack;

        if (string.IsNullOrEmpty(path.Folder))
        {
            return;
        }

        bool discoveredIgnore = DiscoverAndParseIgnore(path.Folder, false, ignoreStack);
        bool discoveredBlitzIgnore = DiscoverAndParseIgnore(path.Folder, false, blitzIgnoreStack);

        if (!Directory.Exists(path.Folder))
        {
            return;
        }

        if (cancellationTokenSource.IsCancellationRequested) return;
        try
        {
            foreach (var fileName in Directory.EnumerateFiles(path.Folder, "*", SearchOption.TopDirectoryOnly))
            {
                if (cancellationTokenSource.IsCancellationRequested) return;
                if (_useGitIgnore && ignoreStack.Any(ignoreInstance => ignoreInstance.IsIgnored(fileName)))
                {
                    continue;
                }
                
                //Todo: separate preference.
                if (_useGitIgnore && blitzIgnoreStack.Any(ignoreInstance => ignoreInstance.IsIgnored(fileName)))
                {
                    continue;
                }

                var extension = Path.GetExtension(fileName);

                var fileSet = _filesByExtension.GetOrAdd(extension, (_) => new ConcurrentDictionary<string, byte>());
                fileSet.TryAdd(fileName, 1);
            }
            
            if (!path.TopLevelOnly)
            {
            
                foreach (var directory in Directory.EnumerateDirectories(path.Folder, "*", SearchOption.TopDirectoryOnly))
                {
                    taskBag.Enqueue(new FileDiscoveryTask(new SearchPath { Folder = directory, TopLevelOnly = false }, new Stack<IgnorePath>(ignoreStack) , new Stack<IgnorePath>(blitzIgnoreStack)));
                }
            }

        }
        catch (UnauthorizedAccessException e)
        {
            ExceptionResults.Add(ExceptionResult.CreateFromException(e));
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

    public List<string> GetFoundExtensions()
    {
        return _filesByExtension.Keys.ToList();
    }

    public IEnumerable<string> EnumerateFilesByExtension(string extension,
        CancellationTokenSource cancellationTokenSource)
    {
        var foundSet = new HashSet<string>();
            var fileSet = _filesByExtension.GetOrAdd(extension, (_) => new ConcurrentDictionary<string, byte>());
        foreach (var fileName in fileSet.Keys)
        {
            if (!foundSet.Add(fileName))
            {
                continue;
            }
            if (cancellationTokenSource.IsCancellationRequested)
            {
                yield break;
            }
            yield return fileName;
        }
    }

    public IEnumerable<string> EnumerateAllFiles(CancellationTokenSource cancellationTokenSource = null!)
    {
        var foundSet = new HashSet<string>();
        while (true)
        {
            bool completeBeforeEvaluation = false;
            lock (_populationTaskLockObject)
            {
                if (_enumerateDirectoriesTask.IsCompleted)
                {
                    completeBeforeEvaluation = true;
                }
            }

            foreach (var fileSet in _filesByExtension.Values)
            {
                foreach (var fileName in fileSet.Keys)
                {
                    if (foundSet.Add(fileName) )
                    {
                        yield return fileName;
                    }
                }
            }

            if (completeBeforeEvaluation)
            {
                break;
            }

            Task.Delay(50, cancellationTokenSource.Token);
        }
    }

    private bool _isFileDiscoveryWorking;
    private bool _isFileDiscoveryFinished;

    private async Task PopulateFilesJob()
    {
        _isFileDiscoveryFinished = false;
        _isFileDiscoveryWorking = true;
        var traversals = new List<Task>();

        var taskBag = new ConcurrentQueue<FileDiscoveryTask>();
        foreach (var path in _rootPaths)
        {
            var ignoreStack = new Stack<IgnorePath>();
            var blitzIgnoreStack = new Stack<IgnorePath>();
            taskBag.Enqueue(new FileDiscoveryTask(path,ignoreStack,blitzIgnoreStack));
        }

        while (true)
        {
            bool foundAny = false;
            while (taskBag.TryDequeue(out var fileDiscoveryTask))
            {
                foundAny = true;
                traversals.Add(Task.Run(() => { DiscoverFiles(fileDiscoveryTask, taskBag,_cancelPopulateToken); },
                     _cancelPopulateToken.Token));
            }

            if (!foundAny)
            {
                break;
            }

            await Task.WhenAll(traversals);
        }

        lock (_populationTaskLockObject)
        {
            if (_resetPopulation)
            {
                _resetPopulation = false;
                _enumerateDirectoriesTask = Task.Run(PopulateFilesJob, _cancelPopulateToken.Token);
            }
            else
            {
                _isFileDiscoveryFinished = true;
                _isFileDiscoveryWorking = false;
            }
        }
    }

    public List<MissingRequirementResult> ErrorInFileDiscoverMessage { get; } = [];

    public bool IsWorking => _isFileDiscoveryWorking;
    public bool IsFinished => _isFileDiscoveryFinished;
    public int FoundFilesCount => _filesByExtension.Values.Sum(dictionary => dictionary.Count);

    private void InstallWatchers()
    {
        foreach (var path in _rootPaths)
        {
            if (path.Folder == null)
            {
                continue;
            }

            if (path.Folder.EndsWith(Path.VolumeSeparatorChar))
            {
                var message = new MissingRequirementResult
                {
                    MissingRequirement = MissingRequirementResult.Requirement.FileDirectory,
                    CustomMessage = $"Path can't end with '{Path.VolumeSeparatorChar}'"
                };
                ErrorInFileDiscoverMessage.Add(message);
                continue;
            }

            if (!Directory.Exists(path.Folder))
            {
                var message = new MissingRequirementResult
                {
                    MissingRequirement = MissingRequirementResult.Requirement.FileDirectory,
                    CustomMessage = $"Directory '{path.Folder}' doesn't exist"
                };
                ErrorInFileDiscoverMessage.Add(message);
                continue;
            }

            try
            {
                var watcher = new FileSystemWatcher(path.Folder, "*");
                watcher.IncludeSubdirectories = !path.TopLevelOnly;
                watcher.EnableRaisingEvents = true;
                watcher.Created += WatcherOnCreated;
                watcher.Renamed += WatcherOnRenamed;
                watcher.Deleted += WatcherOnDeleted;
                watcher.Changed += WatcherOnChanged;
                watcher.Error += WatcherOnError;
                _watchers.Add(watcher);
            }
            catch (Exception e)
            {
                var message = new MissingRequirementResult
                {
                    MissingRequirement = MissingRequirementResult.Requirement.FileDirectory,
                    CustomMessage = $"Exception: {e.Message}"
                };
                ErrorInFileDiscoverMessage.Add(message);
            }
        }
    }

    private void WatcherOnChanged(object? sender, FileSystemEventArgs e)
    {
        Task.Run(() =>
        {
            if (IsIgnored(e.FullPath))
            {
                return;
            }
            FileChanged?.Invoke(sender, e.FullPath);
        },_cancelPopulateToken.Token);
    }

    private async void WatcherOnError(object sender, ErrorEventArgs e)
    {
        await PopulateFiles();
    }

    private void WatcherOnDeleted(object sender, FileSystemEventArgs e)
    {
        var extension = Path.GetExtension(e.FullPath);
        var fileSet = _filesByExtension.GetOrAdd(extension, (_) => new ConcurrentDictionary<string, byte>());
        fileSet.TryRemove(e.FullPath, out _);
    }


    private void WatcherOnRenamed(object sender, RenamedEventArgs e)
    {
        Task.Run(() => 
        { 
            var extension = Path.GetExtension(e.FullPath);
            var fileSet = _filesByExtension.GetOrAdd(extension, (_) => new ConcurrentDictionary<string, byte>());
            fileSet.TryRemove(e.OldFullPath, out _);
            if (!IsIgnored(e.FullPath))
            {
                fileSet.TryAdd(e.FullPath, 1);
                FileChanged?.Invoke(sender, e.FullPath);
            }
        },_cancelPopulateToken.Token);
    }

    private void WatcherOnCreated(object sender, FileSystemEventArgs e)
    {
        Task.Run(() => 
        { 
            var extension = Path.GetExtension(e.FullPath);
            var fileSet = _filesByExtension.GetOrAdd(extension, (_) => new ConcurrentDictionary<string, byte>());
            if (!IsIgnored(e.FullPath))
            {
                fileSet.TryAdd(e.FullPath, 1);
                FileChanged?.Invoke(sender, e.FullPath);
            }
        },_cancelPopulateToken.Token);
    }

    public void DisableWatchers()
    {
        _cancelPopulateToken.Cancel();
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }

    public void WaitUntilFinished(CancellationTokenSource cancellationTokenSource)
    {
        if (_enumerateDirectoriesTask.IsCompleted)
        {
            return;
        }
        try
        {
            _enumerateDirectoriesTask.Wait(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }
}

public class FileDiscoveryTask(SearchPath path, Stack<IgnorePath> ignoreStack, Stack<IgnorePath> blitzIgnoreStack)
{
    public SearchPath Path => path;
    public Stack<IgnorePath> IgnoreStack => ignoreStack;
    public Stack<IgnorePath> BlitzIgnoreStack => blitzIgnoreStack;
}
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
    
    private readonly List<FileDiscoveryPath> _fileRootPaths = [];
    public readonly bool UseGitIgnore;
    public readonly bool UseBlitzIgnore;
    public readonly bool UseGlobalIgnore;
    private readonly CancellationTokenSource _cancelPopulateToken = new();

    public bool CleanupCache(FilesByExtension extToDictionary, CancellationTokenSource searchCancellationToken )
    {
        bool updated = false;
        foreach (var fileName in extToDictionary.Keys)
        {
            if (searchCancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (FileValidate(fileName))
            {
                continue;
            }
            updated = true;
            extToDictionary.TryRemove(fileName, out _);
        }
        return updated;
    }

    public bool FileValidate(string fileName)
    {
        if (!File.Exists(fileName))
        {
            return false;
        }
        
        var anyPathMatches = false;
        var validFile = true;
        foreach (var filePath in _fileRootPaths)
        {
            if (filePath.Folder == null)
            {
                continue;
            }
            if (!filePath.ContainsFile(fileName))
            {
                continue;
            }

            if (filePath.IsIgnored(fileName) || filePath.IsHiddenOrNonAccessible(fileName))
            {
                validFile = false;
            }
            anyPathMatches = true;
        }

        if (!anyPathMatches)
        {
            validFile = false;
        }
        return validFile;
    }
    
    public void RegisterException(Exception exception)
    {
        ExceptionResults.Add(ExceptionResult.CreateFromException(exception));
    }
    public void UpdateDeletedFile(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        var fileSet = _filesByExtension.GetOrAdd(extension, (_) => new ConcurrentDictionary<string, byte>());
        fileSet.TryRemove(fileName, out _);
    }

    public void UpdateCreatedFile(string fileName,bool signalChange = true)
    {
        Task.Run(() => 
        { 
            var extension = Path.GetExtension(fileName);
            var fileSet = _filesByExtension.GetOrAdd(extension, (_) => new ConcurrentDictionary<string, byte>());
            fileSet.TryAdd(fileName, 1);
            if (signalChange)
            {
                FileChanged?.Invoke(this, fileName);
            }
        },_cancelPopulateToken.Token);
    }

    /// <summary>
    /// Keeps files in memory, using a filesystem watcher to keep files up to date.
    /// </summary>
    /// <param name="rootPaths">Collection of SearchPaths used for Defining roots of the FileDiscovery</param>
    /// <param name="useGitIgnore">Use .gitignore files to ignore in results, Also can save memory</param>
    public FileDiscovery(IEnumerable<SearchPath> rootPaths, bool useGitIgnore, bool useBlitzIgnore = true, bool useGlobalIgnore = true)
    {
        UseGitIgnore = useGitIgnore;
        UseBlitzIgnore = useBlitzIgnore;
        UseGlobalIgnore = useGlobalIgnore;
        _isFileDiscoveryWorking = false;
        _isFileDiscoveryFinished = false;
        if (useGlobalIgnore)
        {
            GitConfig.Instance.GitConfigChanged+=InstanceOnGitConfigChanged;
        }
        ConfigureRootPaths(rootPaths);
        _ = PopulateFiles();
    }

    private void InstanceOnGitConfigChanged(object? sender, EventArgs e)
    {
        foreach (var fileDiscoveryPath in _fileRootPaths)
        {
            fileDiscoveryPath.UpdateGlobalIgnorePath();
        }
        ResetFilePopulation();
        NotifyFileChanged(GitConfig.Instance.GetGlobalGitIgnoreFile());
    }

    public void ResetFilePopulation()
    {
        _ = PopulateFiles();
    }

    public void NotifyFileChanged(string fileName)
    {
        FileChanged?.Invoke(this, fileName);
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


    
    private readonly ConcurrentDictionary<string, IgnorePath> _ignoreCache = new();
    private readonly ConcurrentDictionary<string, IgnorePath> _blitzIgnoreCache = new ();

    public bool DiscoverAndParseIgnore(string folder,bool isBlitzIgnore, Stack<IgnorePath> ignoreStack)
    {
        switch (isBlitzIgnore)
        {
            case false when !UseGitIgnore:
            case true when !UseBlitzIgnore:
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
        if (!ignorePath.ParseIgnore())
        {
            return false;
        }
        ignoreStack.Push(ignorePath);
        return true;
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
        foreach (var path in _fileRootPaths)
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
                var task = Task.Run(() =>
                    {
                        fileDiscoveryTask.DiscoveryPath.DiscoverFiles(fileDiscoveryTask, taskBag, _cancelPopulateToken);
                    },
                    _cancelPopulateToken.Token);
                traversals.Add(task);
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

    private void ConfigureRootPaths(IEnumerable<SearchPath> rootPaths)
    {
        foreach (var path in rootPaths)
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
                _fileRootPaths.Add(new FileDiscoveryPath(this,path, _cancelPopulateToken, null));
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


    public void DisableWatchers()
    {
        _cancelPopulateToken.Cancel();
        if (UseGlobalIgnore)
        {
            GitConfig.Instance.GitConfigChanged -= InstanceOnGitConfigChanged;
        }
        foreach (var watcher in _fileRootPaths)
        {
            watcher.DisposeWatcher();
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

public class FileDiscoveryTask(FileDiscoveryPath discoveryPath, Stack<IgnorePath> ignoreStack, Stack<IgnorePath> blitzIgnoreStack)
{
    public FileDiscoveryPath DiscoveryPath => discoveryPath;
    public Stack<IgnorePath> IgnoreStack => ignoreStack;
    public Stack<IgnorePath> BlitzIgnoreStack => blitzIgnoreStack;
}
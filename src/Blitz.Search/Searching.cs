﻿using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Diagnostics;
using Blitz.Files;
using Blitz.Interfacing.QueryProcessing;
namespace Blitz.Search;
public class Searching
{
    private readonly MessageProcessDictionary _messageProcesses = new MessageProcessDictionary();
    public event EventHandler<SearchTaskResult>? ReceivedFileResultEventHandler;

    public SearchExtensionCache ExtensionCache = new SearchExtensionCache();
    public FileDiscovery? FileDiscoverer { get; private set; }
    public static TimeSpan QuietTime => DefaultSettings.QuietUITime;

    public TimeSpan LastResult { get; set; } = TimeSpan.Zero;


    public void ProcessSearchingRequest(SearchQuery currentQuery, bool needsFileSystemRestart)
    {
        SearchQuery query = currentQuery.Clone();
        ProcessSearchingRequestInternal(query, needsFileSystemRestart);
    }
    
    private void ProcessSearchingRequestInternal(SearchQuery query, bool needsFileSystemRestart)
    {
        ImmutableHashSet<string> recycledExclusions = [];
        var recycleList = new List<FileNameResult>();
        var messageDictionary = _messageProcesses.GetOrAdd(query.ProcessIdentity, _ => new MessageInstanceDictionary());

        var newTask = new SearchTask(query, this);

        bool recycle = false;
        if (messageDictionary.TryGetValue(query.InstanceIdentity, out var workingTask))
        {
            recycle = query.EnableResultsRecycling;
            workingTask.CancellationTokenSource.Cancel();
            _fileChangedCancel.Cancel();
            if (query.RestartedFromGitIGnore)
            {
                query.RestartedFromGitIGnore = false;
                var searchTaskResult = new SearchTaskResult
                {
                    ServerResultsResetClear = true
                };
                searchTaskResult.AlignIdentity(query);
                RaiseNewSearchTaskResult(searchTaskResult);
            }

            if (!needsFileSystemRestart)
            {
                DoPriorResultsRecycling(query, workingTask, newTask, out recycledExclusions, out recycleList);
            }
            else
            {
                recycle = false;
            }
        }

        
        if (needsFileSystemRestart || FileDiscoverer == null)
        {
            FileDiscoverer?.DisableWatchers();
            FileDiscoverer = new FileDiscovery(query.FilePaths, query.UseGitIgnore, query.UseBlitzIgnore, query.UseGlobalIgnore);
            ExtensionCache = new SearchExtensionCache();
            FileDiscoverer.FileChanged += FileChanged;
        }

        if (recycle)
        {
            newTask.Recycling.RecycledExclusions = recycledExclusions;
            foreach (var file in recycledExclusions)
            {
                newTask.Recycling.FilesWithNoResults.TryAdd(file,1);
            }

            newTask.Recycling.RecyclingResultsInOrder = recycleList.ToImmutableList();
            foreach (var recycled in recycleList)
            {
                if (recycled.FileName != null)
                {
                    newTask.Recycling.RetainedResults.TryAdd(recycled.FileName, recycled);
                }
            }

            newTask.Recycling.RecyclingResults = newTask.Recycling.RetainedResults.ToImmutableDictionary();
        }

        messageDictionary[query.InstanceIdentity] = newTask;
        newTask.StartSearch();
    }

    private ConcurrentDictionary<string, byte> _changedFiles = new();
    private Task _changedFileProcessor = Task.CompletedTask;
    private object _changedFileSync = new object();
    private CancellationTokenSource _fileChangedCancel = new CancellationTokenSource();
    
    private void FileChanged(object? sender, string e)
    {
        string extension = Path.GetExtension(e);
        if (ExtensionCache == null)
        {
            throw new ArgumentNullException(nameof(ExtensionCache));
        }
        if (ExtensionCache.TryGetValue(extension, out var dictionary) &&
            dictionary.TryGetValue(e, out var searchFileInformation))
        {
            if (searchFileInformation != null)
            {
                searchFileInformation.FileState = SearchFileInformation.ReadState.Unknown;
            }
        }

        _changedFiles.TryAdd(e, 1);
        lock (_changedFileSync)
        {
            if (_changedFileProcessor.IsCompleted)
            {
                _fileChangedCancel = new CancellationTokenSource();
                _changedFileProcessor = Task.Run(ProcessChangedFilesTask, _fileChangedCancel.Token);
            }
        }
    }

    private List<SearchTask> GetExistingSearchTasks()
    {
        return _messageProcesses.Values.SelectMany(processDictionary => processDictionary.Values).ToList();
    }

    private void  ProcessChangedFilesTask()
    { 
        
        while (_changedFiles.Count > 0)
        {
            var tasks = GetExistingSearchTasks();
            foreach (var changedFile in _changedFiles.Keys)
            {
                _changedFiles.TryRemove(changedFile, out _);
                foreach (var task in tasks)
                {
                    if (task.RestartIfGitIgnore(changedFile))
                    {
                        continue;
                    }

                    task.UpdateFileChanged(changedFile,_fileChangedCancel);
                }
            }
        }
    }

    private void DoPriorResultsRecycling(SearchQuery newQuery, SearchTask oldTask, SearchTask newTask,
        out ImmutableHashSet<string> acceptedExclusions, out List<FileNameResult> fileNameResults)
    {
        if (!newQuery.EnableResultsRecycling)
        {
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }
        if (oldTask == null)
        {
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }

        if (oldTask.SearchQuery.FileNameInResultsInResultsEnabled != newQuery.FileNameInResultsInResultsEnabled)
        {
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }

        if (oldTask.SearchQuery.SolutionExports == null && newQuery.SolutionExports != null
            ||newQuery.SolutionExports == null && oldTask.SearchQuery.SolutionExports != null)
        {
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }

        if (oldTask.SearchQuery.SolutionExports != null && newQuery.SolutionExports != null)
        {
            if (newQuery.SolutionExports.Count == 0 || oldTask.SearchQuery.SolutionExports.Count == 0)
            {
                acceptedExclusions = [];
                fileNameResults = [];
                return;
            }

            if (oldTask.SearchQuery.SolutionExports.Count != 1 && newQuery.SolutionExports.Count != 1)
            {
                throw new NotImplementedException(
                    "Make sure to upgrade this when adding support for searching multiple solutions");
            }

            var oldProjects = oldTask.SearchQuery.SolutionExports[0].Projects;
            var newProjects = newQuery.SolutionExports[0].Projects;
            if (oldProjects!.Count != newProjects!.Count)
            {
                acceptedExclusions = [];
                fileNameResults = [];
                return;
            }

            for (int i = 0; i < oldProjects.Count; i++)
            {
                if (oldProjects[i].Name != newProjects[i].Name
                    ||oldProjects[i].Files!.Count != newProjects[i].Files!.Count)
                {
                    acceptedExclusions = [];
                    fileNameResults = [];
                    return;
                }

                for (int j = 0; j < oldProjects[i].Files!.Count; j++)
                {
                    if (newProjects[i].Files[j] != oldProjects[i].Files[j])
                    {
                        acceptedExclusions = [];
                        fileNameResults = [];
                        return;
                    }
                }
            }
        }

        if (oldTask.SearchQuery.LiteralCaseSensitive != newQuery.LiteralCaseSensitive
            ||oldTask.SearchQuery.RegexCaseSensitive != newQuery.RegexCaseSensitive
            ||oldTask.SearchQuery.ReplaceCaseSensitive != newQuery.ReplaceCaseSensitive)
        {
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }

        if (!newQuery.EnableResultsRecycling)
        {
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }

        if (newQuery.LiteralSearchEnabled || oldTask.SearchQuery.LiteralSearchEnabled)
        {
            // lazy.. could maybe do recycling on literal.
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }
    

        if (newQuery.RegexSearchEnabled || oldTask.SearchQuery.RegexSearchEnabled)
        {
            // lazy.. could maybe do recycling on literal.
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }

        if (oldTask.SearchQuery.FileNameDebugQueryEnabled != newQuery.FileNameDebugQueryEnabled)
        {
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }

        if (newQuery.FileNameDebugQueryEnabled)
        {
            if (string.IsNullOrEmpty(oldTask.SearchQuery.FileNameQuery) && !string.IsNullOrEmpty(newQuery.FileNameQuery))
            {
                acceptedExclusions = [];
                fileNameResults = [];
                return;
            }

            if (oldTask.SearchQuery.FileNameQuery != newQuery.FileNameQuery)
            {
                acceptedExclusions = [];
                fileNameResults = [];
                return;
            }
        }

        if (oldTask.SearchQuery.ReplaceInFileEnabled != newQuery.ReplaceInFileEnabled)
        {
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }

        if (oldTask.SearchQuery.RawExtensionList != newQuery.RawExtensionList
            ||oldTask.SearchQuery.RawExtensionList != newQuery.RawExtensionList)
        {
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }

        BlitzAndQuery.QueryMatches(oldTask.SearchQuery.TextBoxQuery, out var oldTextQuery);
        BlitzAndQuery.QueryMatches(newQuery.TextBoxQuery, out var newTextQuery);
        
        for (int i = 0; i < oldTextQuery.SubQueries.Count; i++)
        {
            var oldSubQuery = oldTextQuery.SubQueries[i];
            if (i >= newTextQuery.SubQueries.Count)
            {
                acceptedExclusions = [];
                fileNameResults = [];
                return;
            }

            var newSubQuery = newTextQuery.SubQueries[i];

            if (IsOldQueryRecyclable(oldSubQuery, newSubQuery))
            {
                continue;
            }
            
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }
        
        if(oldTask.SearchQuery.EnableResultsRecycling != newQuery.EnableResultsRecycling)
        {
            acceptedExclusions = [];
            fileNameResults = [];
            return;
        }
        
        if (newQuery.EnableResultsRecycling)
        {
            if (oldTask.SearchQuery.ReplaceInFileEnabled && newQuery.ReplaceInFileEnabled)
            {
                BlitzAndQuery.QueryMatches(oldTask.SearchQuery.ReplaceTextQuery!, out var oldTextReplaceQuery);
                BlitzAndQuery.QueryMatches(newQuery.ReplaceTextQuery!, out var newTextReplaceQuery);

                if (!IsOldQueryRecyclable(oldTextReplaceQuery, newTextReplaceQuery))
                {
                    acceptedExclusions = [];
                    fileNameResults = [];
                    return;
                }
            }
        }

        acceptedExclusions = oldTask.Recycling.FilesWithNoResults.Keys.ToImmutableHashSet();

        fileNameResults = [];
        var newList = new List<FileNameResult>();

        var newTaskParameters = newTask.GetSearchTaskParameters();
        
        foreach (var fileResult in oldTask.Recycling.RetainedResults)
        {
            string fileName = fileResult.Key;
            var update = new FileNameResult { FileName = fileName };
            FileNameResult oldResult = fileResult.Value;

            update.FileName = fileName;
            
            bool presentThisFile = false;
            bool foundAnything = false;
            bool fileNameMatch = false;
            if (newTaskParameters.SearchFileNamesInResults)
            {
                var taskResult = newTask.InitializeSearch(newTaskParameters, fileName, ref presentThisFile,
                    ref foundAnything);
                update.BlitzMatches = taskResult.FileNames.First().BlitzMatches;
                fileNameMatch = update.BlitzMatches.Count > 0;
            }
            var newContentResults = new List<FileContentResult>();
            foreach (var contentResult in oldResult.ContentResults)
            {
                Debug.Assert(contentResult.CapturedContents != null, "contentResult.CapturedContents != null");
                if (newTextQuery.LineMatches(contentResult.CapturedContents,
                        out var updatedContentMatches))
                {
                    var newContentResult = new FileContentResult
                    {
                        LineNumber = contentResult.LineNumber,
                        CapturedContents = contentResult.CapturedContents,
                        BlitzMatches = updatedContentMatches
                    };
                    newContentResults.Add(newContentResult);
                }
            }

            update.ContentResults = newContentResults;
            if (fileNameMatch || update.ContentResults.Count > 0)
            {
                newList.Add(update);
            }
        }
        

        fileNameResults = newList;

    }

    private static bool IsOldQueryRecyclable(IBlitzMatchingQuery oldSubQuery, IBlitzMatchingQuery newSubQuery)
    {
        //don't recycle if they don't match in the order.
        if (oldSubQuery.GetType() != newSubQuery.GetType())
        {
            return false;
        }

        if (oldSubQuery is BlitzWordInQuery oldWordInQuery && newSubQuery is BlitzWordInQuery newWordQuery)
        {
            if (oldWordInQuery.WholeWord != newWordQuery.WholeWord)
            {
                return false;
            }
            else if (oldWordInQuery.SearchWord != newWordQuery.SearchWord)
            {
                return false;
            }
            if (oldWordInQuery.CaseSensitive != newWordQuery.CaseSensitive)
            {
                return false;
            }
            if (oldWordInQuery.IsExclude != newWordQuery.IsExclude)
            {
                return false;
            }
            
            if (oldWordInQuery.SearchWord == newWordQuery.SearchWord)
            {
                return true;
            }

            if (newWordQuery.IsExclude && oldWordInQuery.SearchWord != newWordQuery.SearchWord)
            {
                return false;
            }
            
            return newWordQuery.SearchWord.StartsWith(oldWordInQuery.SearchWord);
        }

        return false;
    }

    public bool QuietTimeElapsed(SearchTask currentSearchTask)
    {
        var runningTime = DateTime.Now - currentSearchTask.StartTime;
        return runningTime > QuietTime;
    }
    

    public void UnionTaskResult(SearchTaskResult searchTaskResult, SearchTask currentSearchTask)
    {
        var runningTime = DateTime.Now - currentSearchTask.StartTime;
        LastResult = runningTime;
        if (runningTime > QuietTime )
        {
            currentSearchTask.EmptyUnionResults();
            RaiseNewSearchTaskResult(searchTaskResult);
            return;
        }
        
        var newResult = new SearchTaskResult();
        newResult.AlignIdentity(searchTaskResult);
        lock (currentSearchTask.UnionedResultsSyncTaskLock)
        {
            var newListOfResults = new List<FileNameResult>(currentSearchTask.UnionResults.FileNames);
            newListOfResults.AddRange(searchTaskResult.FileNames);
            newResult.FileNames = newListOfResults;
            currentSearchTask.UnionResults = newResult;
        }
    }

    public void RaiseNewSearchTaskResult(SearchTaskResult searchTaskResult)
    {
        ReceivedFileResultEventHandler?.Invoke(this, searchTaskResult);
    }
}
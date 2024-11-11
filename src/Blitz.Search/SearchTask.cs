using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Blitz.Interfacing.QueryProcessing;
using Humanizer;

namespace Blitz.Search;

public class SearchTask
{
    public SearchQuery SearchQuery { get; }
    private Searching SearchRoot { get; }
    public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

    private int _fileNameCount;
    private long _totalFileSIze;
    private int _totalResultsCount;

    public bool Working { get; set; }

    public bool CappedResults { get; set; }

    private readonly object _capSync = new object();

    public DateTime StartTime { get; }
    public DateTime EndTime;

    public ImmutableHashSet<string> RecycledExclusions { get; set; } = [];
    public ConcurrentDictionary<string, byte> FilesWithNoResults { get; } = [];

    public ImmutableDictionary<string, FileNameResult> RecyclingResults { get; set; } =
        ImmutableDictionary.Create<string, FileNameResult>();

    public ImmutableList<FileNameResult> RecyclingResultsInOrder { get; set; } = ImmutableList.Create<FileNameResult>();


    public ConcurrentDictionary<string, FileNameResult> RetainedResults { get; } = [];

    public ConcurrentDictionary<string, SearchFileInformation> DeferredRobotFiles { get; set; } = [];
    public SearchTaskResult UnionResults { get; set; } = new SearchTaskResult();

    private ConcurrentDictionary<string, byte> _searchedFiles = new();

    public SearchTask(SearchQuery searchQuery, Searching searchRoot)
    {
        SearchQuery = searchQuery;
        SearchRoot = searchRoot;
        StartTime = DateTime.Now;
    }

    private void DoStatusUpdate()
    {
        if (SearchRoot.FileDiscoverer == null)
        {
            return;
        }

        var searchTaskResult = new SearchTaskResult();
        var status = searchTaskResult.FileSearchStatus;
        status.StatusUpdated = true;
        status.Working = !CappedResults && Working;

        if (status.Working)
        {
            var newRunningTime = DateTime.Now - StartTime;
            status.RunningTime = newRunningTime;
        }
        else
        {
            var newRunningTime = EndTime - StartTime;
            status.RunningTime = newRunningTime;
        }

        status.LastResultTime = SearchRoot.LastResult;

        status.FileDiscoveryFinished = SearchRoot.FileDiscoverer.IsFinished;
        status.DiscoveredCount = SearchRoot.FileDiscoverer.FoundFilesCount;
        status.FilesProcessed = _fileNameCount;
        status.TotalFileSIze = _totalFileSIze;
        status.Discovering = !SearchRoot.FileDiscoverer.IsFinished;
        searchTaskResult.AlignIdentity(SearchQuery);
        SearchRoot.RaiseNewSearchTaskResult(searchTaskResult);
    }

    private async void StatusTask()
    {
        while (!CancellationTokenSource.IsCancellationRequested && Working)
        {
            DoStatusUpdate();
            try
            {
                await Task.Delay(50, CancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    //Todo: Results Cap Option? 
    const int resultsCap = 10000;


    private bool DebugMatchesFile(BlitzAndQuery? debugFileQuery, string file)
    {
        if (debugFileQuery == null)
        {
            return false;
        }
        if (String.IsNullOrEmpty(debugFileQuery.SearchWord.Trim()) )
        {
            return false;
        }
        return debugFileQuery.LineMatches(file, false, out var _);
    }
    private void DebugReportPriorToCreation(BlitzAndQuery? debugFileQuery, string file, string debugMessage)
    {
        if (debugFileQuery == null)
        {
            return;
        }
        if (!DebugMatchesFile(debugFileQuery, file))
        {
            return;
        }
        var searchTaskResult = new SearchTaskResult
        {
            FileNames = [new FileNameResult { FileName = file, BlitzMatches = [], DebugInformation = debugMessage}]
        };
        searchTaskResult.AlignIdentity(SearchQuery);
        SearchRoot.RaiseNewSearchTaskResult(searchTaskResult);
    }

    private void DebugPostCreation(BlitzAndQuery? debugFileQuery, SearchTaskResult taskResult, string debugMessage)
    {
        if (debugFileQuery == null)
        {
            return;
        }
        if (!DebugMatchesFile(debugFileQuery, taskResult.FileNames[0].FileName!))
        {
            return;
        }

        if (string.IsNullOrEmpty(taskResult.FileNames[0].DebugInformation))
        {
            taskResult.FileNames[0].DebugInformation = string.Empty;
        }

        taskResult.FileNames[0].DebugInformation += $"[{debugMessage}]{Environment.NewLine}";
        taskResult.AlignIdentity(SearchQuery);
        SearchRoot.RaiseNewSearchTaskResult(taskResult);
    }

    private ParallelOptions GetParallelOptions() => new() { MaxDegreeOfParallelism = SearchQuery.SearchThreads, CancellationToken = CancellationTokenSource.Token};

    private SearchTaskResult InitializeSearch(SearchTaskParameters taskParameters, string file,  ref bool presentThisFile, ref bool foundAnything)
    {
        List<BlitzMatch>? matches = null;

        bool wordsFailed = false;
        bool regexFailed = false;
        bool literalFailed = false;
        if (taskParameters.TextBoxQuery.SubQueries.Count > 0 && !taskParameters.TextBoxQuery.LineMatches(file, caseSensitive:false, out matches))
        {
            wordsFailed = true;
        }

        if (taskParameters.RegexSearch != null )
        {
            if (taskParameters.RegexSearch.IsMatch(file))
            {
                matches ??= [];
                foreach (Match match in taskParameters.RegexSearch.Matches(file))
                {
                    matches.Add(new BlitzMatch() { MatchIndex = match.Index, MatchLength = match.Length });
                }
            }
            else
            {
                regexFailed = true;
            }
        }

        if (!string.IsNullOrEmpty(taskParameters.LiteralSearch) )
        {
            int index = file.IndexOf(taskParameters.LiteralSearch,
                taskParameters.LiteralCaseSensitive
                    ? StringComparison.CurrentCulture :StringComparison.CurrentCultureIgnoreCase);
            if (index != -1)
            {
               
                matches ??= [];
                matches.Add( new BlitzMatch(){MatchIndex = index, MatchLength = taskParameters.LiteralSearch.Length});
            }
            else
            {
                literalFailed = true;
            }
        }
        presentThisFile = false;
        foundAnything = false;

        if (!wordsFailed && !regexFailed && !literalFailed)
        {
            presentThisFile = true;
            foundAnything = true;
        }

        if (taskParameters.RegexSearch == null && string.IsNullOrEmpty(taskParameters.LiteralSearch) &&
            taskParameters.TextBoxQuery.SubQueries.Count == 0)
        {
            presentThisFile = false;
            foundAnything = false;
        }

        
        matches ??= [];
        
        var searchTaskResult = new SearchTaskResult
        {
            FileNames = [new FileNameResult { FileName = file, BlitzMatches = matches }]
        };
                
        searchTaskResult.AlignIdentity(SearchQuery);
        return searchTaskResult;
    }

    public IEnumerable<string> SolutionEnumerator(string extension, CancellationTokenSource cancellationTokenSource)
    {
        if(SearchQuery.SolutionExports == null) yield break;
        foreach (var solutionExport in SearchQuery.SolutionExports)
        {
            yield return solutionExport.Name!;
            foreach (var project in solutionExport.Projects ?? [])
            {
                yield return project.Name!;
                foreach (var file in project.Files?? [])
                {
                    string fileExtension = string.Empty;
                    try
                    {
                        fileExtension = Path.GetExtension(file);
                    }
                    catch (Exception e)
                    {
                        //don't bother with things that aren't a file
                        continue;
                    }

                    if (fileExtension == extension)
                    {
                        yield return file;
                    }
                }
            }
        }
    }

    public delegate IEnumerable<string> SearchEnumerator(string extension, CancellationTokenSource cancellationTokenSource);
    
    private bool SearchExtension(SearchTaskParameters taskParameters, SearchExtension extension, SearchEnumerator enumerator)
    {
        if (CancellationTokenSource.IsCancellationRequested)
        {
            return false;
        }
        Debug.Assert(SearchRoot.FileDiscoverer != null, "SearchRoot.FileDiscoverer != null");

        if (SearchQuery.SearchThreads == 0)
        {
            SearchQuery.SearchThreads = 4;
        }

        if (extension.Extension == null)
        {
            throw new ArgumentNullException(nameof(extension.Extension));
        }
        
        var fileCache = SearchRoot.ExtensionCache.GetOrAdd(extension.Extension,
            _ => new FilesByExtension());

        bool foundAnyInThisExtension = false;
        var parallelOptions = GetParallelOptions();
        Parallel.ForEach( enumerator(extension.Extension, CancellationTokenSource),
            parallelOptions,
            (file) =>
            {
                bool foundAnything = false;
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                if (!_searchedFiles.TryAdd(file,1))
                {
                    return;
                }

                long size = 0;
                try
                {
                    // This serves as file exists test
                    size = new FileInfo(file).Length;
                }
                catch (IOException)
                {
                    return;
                }
                
                _totalFileSIze = Interlocked.Add(ref _totalFileSIze,size);

                if (DebugMatchesFile(taskParameters.DebugFileNameQuery,file))
                {
                    string ext = Path.GetExtension(file);
                    if (SearchRoot.ExtensionCache.TryGetValue(ext, out var dictionary) 
                        && dictionary.TryGetValue(file, out var searchFileInformation))
                    {
                        StringBuilder builder = new StringBuilder();

                        if (searchFileInformation != null)
                        {
                            builder.Append("Filesize: ");
                            //builder.AppendLine(searchFileInformation.FileSize.Bytes().Humanize());
                            builder.AppendLine(searchFileInformation.FileSize.Bytes().Humanize());
                            builder.AppendLine();
                            builder.Append("UniqueWords: ");
                            builder.AppendLine(string.Join(" ",fileCache.GetWordStrings(searchFileInformation.UniqueWords)) );
                            builder.AppendLine();
                            builder.Append("LastModifiedTime: ");
                            builder.AppendLine(searchFileInformation.LastModifiedTime.Humanize());
                            builder.AppendLine();
                            builder.Append("FileState: ");
                            builder.AppendLine(searchFileInformation.FileState.ToString());
                            builder.AppendLine();
                        }
                        DebugReportPriorToCreation(taskParameters.DebugFileNameQuery, file +"DEBUG",builder.ToString() );
                    }
                }
                
                if (RecycledExclusions.Contains(file))
                {
                    DebugReportPriorToCreation(taskParameters.DebugFileNameQuery,file, "Recycled Excluded");
                    return;
                }
                Interlocked.Increment(ref _fileNameCount);

                if (SearchQuery.FileNameQueryEnabled && SearchQuery.FileNameQuery != null
                    && !taskParameters.FileNameQuery!.LineMatches(file, caseSensitive:false, out var fileNameMatches))
                {
                    DebugReportPriorToCreation(taskParameters.DebugFileNameQuery, file, "FileName Query disabled");
                    return;
                }

                bool presentThisFile = false;
                var searchTaskResult = InitializeSearch(taskParameters, file ,ref presentThisFile, ref foundAnything);
               
                ContentResult result = ContentResult.Unknown;
                try
                {
                    
                    result = SearchContents(taskParameters, fileCache, file, extension.Extension, searchTaskResult, null);
                    if (CancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    if (result == ContentResult.FoundContents)
                    {
                        presentThisFile = true;
                        foundAnything = true;
                    }
                    else if(result == ContentResult.NotFound) 
                    {
                        if (!foundAnything)
                        {
                            
                            DebugPostCreation(taskParameters.DebugFileNameQuery, searchTaskResult, "No Contents" );
                            if (CancellationTokenSource.IsCancellationRequested)
                            {
                                return;
                            }

                            FilesWithNoResults.TryAdd(file,1);
                        }
                        else
                        {
                            if (CancellationTokenSource.IsCancellationRequested)
                            {
                                return;
                            }

                            RetainedResults.TryAdd(searchTaskResult.FileNames[0].FileName!,
                                searchTaskResult.FileNames[0]);
                        }
                    }
                }
                catch (Exception ex ) when (ex is UnauthorizedAccessException or IOException)
                {
                    if (SearchQuery.VerboseFileSystemException)
                    {
                        ReportException(ex);
                    }
                }

                if (CancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
                
                if( !foundAnything && result == ContentResult.NotFound )
                {
                    FilesWithNoResults.TryAdd(file, 1);
                }
                else
                {
                    if (result == ContentResult.FoundContents)
                    {
                        foundAnyInThisExtension = true;
                    }
                }

                if (!presentThisFile)
                {
                    DebugPostCreation(taskParameters.DebugFileNameQuery, searchTaskResult, "Not presenting" );
                    return;
                }

                Interlocked.Add(ref _totalResultsCount,searchTaskResult.FileNames[0].ContentResults.Count + 1);

                if (_totalResultsCount > resultsCap)
                {
                    lock (_capSync)
                    {
                        DebugPostCreation(taskParameters.DebugFileNameQuery, searchTaskResult, "Capped Results" );
                        if (CappedResults)
                        {
                            return;
                        }
                        searchTaskResult.FileNames.Clear();
                        searchTaskResult.MissingRequirements.Add(new MissingRequirementResult
                        {
                            CustomMessage =
                                $"Results Capped 'at {resultsCap}' to prevent overflow, please try and be more specific.. ", 
                            MissingRequirement = MissingRequirementResult.Requirement.NeedsRefinement
                        });
                        EndTime = DateTime.Now;
                        CappedResults = true;
                        EmptyUnionResults();
                        SearchRoot.RaiseNewSearchTaskResult(searchTaskResult);
                        DoStatusUpdate();
                        CancellationTokenSource.Cancel();
                        return;
                    }
                }

                SearchRoot.UnionTaskResult(searchTaskResult,this);
            });
        

        return foundAnyInThisExtension;
    }

    private void ClearRecyclingInfo()
    {
        RecycledExclusions = [];
        RecyclingResultsInOrder = [];
        RetainedResults.Clear();
        DeferredRobotFiles.Clear();
        FilesWithNoResults.Clear();
    }

    private SearchTaskParameters GetSearchTaskParameters()
    {
        BlitzAndQuery.QueryMatches(SearchQuery.TextBoxQuery, out var searchTextBox);
        BlitzAndQuery.QueryMatches(SearchQuery.FileNameQuery ?? string.Empty, out var fileNameQuery);
        BlitzAndQuery.QueryMatches(SearchQuery.DebugFileNameQuery ?? string.Empty , out var debugFileNameQuery);

        IBlitzMatchingQuery? replaceTextQuery = null;
        BlitzAndQuery.QueryMatches(SearchQuery.ReplaceTextQuery ?? string.Empty , out var replaceAndTextQuery);
        string? replaceLiteral = SearchQuery.ReplaceLiteralTextQuery;
        Regex? replaceRegex = null;

        string? literal = SearchQuery.LiteralSearchEnabled ? SearchQuery.LiteralSearchQuery : null;
        if (SearchQuery.ReplaceInFileEnabled)
        {
            replaceTextQuery = replaceAndTextQuery.SubQueries.FirstOrDefault();
            try
            {
                if (!string.IsNullOrEmpty(SearchQuery.ReplaceRegexTextQuery))
                {
                    RegexOptions options = RegexOptions.None;
                    if (!SearchQuery.ReplaceCaseSensitive)
                    {
                        options = RegexOptions.IgnoreCase;
                        
                    }
                    replaceRegex = new Regex(SearchQuery.ReplaceRegexTextQuery, options);
                }
            }
            catch (ArgumentException)
            {
                replaceRegex = null;
            }
            replaceLiteral = SearchQuery.ReplaceLiteralTextQuery;
        }

        Regex? regex = null;

        if (SearchQuery.RegexSearchEnabled && !string.IsNullOrEmpty(SearchQuery.RegexSearchQuery))
        {
            try
            {
                
                RegexOptions rxOptions = RegexOptions.None;
                if (!SearchQuery.RegexCaseSensitive)
                {
                    rxOptions = RegexOptions.IgnoreCase; 
                }
                
                regex = new Regex(SearchQuery.RegexSearchQuery,rxOptions);
            }
            catch (ArgumentException)
            {
                //todo: what now? 
                regex = null;
            }
        }


        return new SearchTaskParameters(searchTextBox, 
            fileNameQuery,
            debugFileNameQuery, 
            replaceTextQuery,
            replaceRegex, 
            replaceLiteral, 
            SearchQuery.ReplaceTextWithQuery, 
            literal, 
            regex, 
            SearchQuery.LiteralCaseSensitive,
            SearchQuery.RegexCaseSensitive,
            SearchQuery.ReplaceCaseSensitive
            );
    }

    private void DoSearchTask()
    {
        _totalFileSIze = 0;
        if (ReportPreSearchProblem(out var taskResultsWithErrors))
        {
            Working = false;
            EndTime = DateTime.Now;
            ClearRecyclingInfo();
            SearchRoot.RaiseNewSearchTaskResult(taskResultsWithErrors);
            CancellationTokenSource.Cancel();
            DoStatusUpdate();
            return;
        }

        bool foundAnything = DoRecycledResults();
        var orderedSearch = new List<SearchExtension>();

        var userSetofExtensions = new HashSet<string>();
        foreach (var extension in SearchQuery.PriorityExtensions)
        {
            orderedSearch.Add(extension);
            userSetofExtensions.Add(extension.Extension!);
        }
        
        SearchRoot.ExtensionCache.RestoreExtensionsEverKnown(TypeDetection.Instance.GetBuiltInTextTypes(),TypeDetection.Instance.GetBuiltInBinaryTypes());

        var taskParameters = GetSearchTaskParameters();
        var allfoundExtensions = SearchRoot.FileDiscoverer!.GetFoundExtensions();
        foreach (var extension in allfoundExtensions)
        {
            if( SearchRoot.ExtensionCache.TryGetValue(extension, out var filesByExtension) 
               && taskParameters.TextBoxQuery.SearchIndexValidFor(filesByExtension.GetAllWords()))
            {
                orderedSearch.Add(new SearchExtension{Extension = extension} );
                userSetofExtensions.Add(extension);
            }
        }

        if (SearchQuery.SolutionExports != null)
        {
            foreach (var solutionExports in SearchQuery.SolutionExports)
            {
                foreach (var project in solutionExports.Projects ?? [])
                {
                    foreach (var file in project.Files ?? [])
                    {
                        string extension = Path.GetExtension(file);
                        if (userSetofExtensions.Contains(extension))
                        {
                            continue;
                        }
                        orderedSearch.Add(new SearchExtension{Extension = extension} );
                    }
                }
            }
        }
        
        foreach (var extension in allfoundExtensions)
        {
            if (userSetofExtensions.Contains(extension))
            {
                continue;
            }
            orderedSearch.Add(new SearchExtension{Extension = extension});
        }

        foreach(var extension in SearchRoot.ExtensionCache.ExtensionsEverKnown.Keys)
        {
            if (userSetofExtensions.Contains(extension))
            {
                continue;
            }
            orderedSearch.Add(new SearchExtension{Extension = extension});
        }

        List<string> paths = new List<string>();
        foreach (var path in SearchQuery.FilePaths)
        {
            if (path.Folder != null)
            {
                paths.Add(path.Folder);
            }
        }
            
        foreach (var extension in orderedSearch)
        {
            if (CancellationTokenSource.IsCancellationRequested)
            {
                return;
            }
            try
            {

                //For now this is a radio selection.  and SolutionExports != null means it's time to search solutions only.
                if (SearchQuery.SolutionExports != null)
                {
                    if (SearchExtension(taskParameters, extension, SolutionEnumerator ))
                    {
                        foundAnything = true;
                    }
                }
                else
                {
                    SearchRoot.ExtensionCache.RestoreCache(paths, !SearchQuery.EnableRobotFileFilterDefer,SearchQuery.RobotFilterMaxSizeMB, SearchQuery.RobotFilterMaxLineChars, SearchQuery.UseGitIgnore,extension.Extension!,CancellationTokenSource.Token);
                    if (SearchExtension(taskParameters, extension, SearchRoot.ExtensionCache.EnumCacheFileExtensions ))
                    {
                        foundAnything = true;
                    }

                }
                
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
            }
            catch (Exception e ) when (e is not OperationCanceledException)
            {
                Console.WriteLine(e);
            }
        }

        EmptyUnionResults();

        if (SearchQuery.SolutionExports != null)
        {
            SearchRoot.FileDiscoverer?.WaitUntilFinished(CancellationTokenSource);
            CleanupCache();
        }

        if (CancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        if (SearchQuery.SolutionExports == null)
        {
            foreach (var lateExtensions in SearchRoot.FileDiscoverer!.GetFoundExtensions())
            {
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                if (orderedSearch.All(extension => extension.Extension != lateExtensions))
                {
                    //only restore if they were late in this discovery.
                    SearchRoot.ExtensionCache.RestoreCache(paths, !SearchQuery.EnableRobotFileFilterDefer,SearchQuery.RobotFilterMaxSizeMB, SearchQuery.RobotFilterMaxLineChars, SearchQuery.UseGitIgnore,lateExtensions,CancellationTokenSource.Token);
                }
                
                SearchRoot.ExtensionCache.ExtensionsEverKnown[lateExtensions] = 1;
                if (SearchExtension(taskParameters, new SearchExtension{Extension = lateExtensions}, SearchRoot.FileDiscoverer.EnumerateFilesByExtension))
                {
                    foundAnything = true;
                }
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
                if (SearchRoot.QuietTimeElapsed(this) )
                {
                    EmptyUnionResults();
                }
            }
        }

        if (CancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        if (SearchQuery.EnableRobotFileFilterDefer)
        {
            EmptyUnionResults();
        }

        BuildRobotFileSummary();

        if (SearchQuery.EnableRobotFileFilterDefer)
        {
            foreach (var deferedKvp in DeferredRobotFiles)
            {
                bool presentThisFile = false;
                string extension = Path.GetExtension(deferedKvp.Key);
                if (!SearchRoot.ExtensionCache.ContainsKey(extension))
                {
                    continue;
                }
                var searchTaskResult = InitializeSearch( taskParameters, deferedKvp.Key, ref presentThisFile, ref foundAnything);
                if (FinalizeSearch(taskParameters, deferedKvp.Key, searchTaskResult, false, CancellationTokenSource) == ContentResult.FoundContents || presentThisFile) 
                {
                    foundAnything = true;
                    SearchRoot.UnionTaskResult(searchTaskResult, this);
                }
            }
        }

        if (!foundAnything)
        {
            ReportFilesNotFoundMessage(SearchQuery.TextBoxQuery, SearchQuery.FileNameQuery);
        }

        if (SearchQuery.SolutionExports == null)
        {
            foreach (var extension in orderedSearch)
            {
                if (CancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    if (SearchRoot.FileDiscoverer is { IsFinished: true })
                    {
                        SearchRoot.ExtensionCache.SaveCache(paths, !SearchQuery.EnableRobotFileFilterDefer,
                            SearchQuery.RobotFilterMaxSizeMB, SearchQuery.RobotFilterMaxLineChars, SearchQuery.UseGitIgnore,
                            extension.Extension!);
                    }
                }
                catch (Exception e)
                {
                    ReportException(e);
                }
            }
            try
            {
                SearchRoot.ExtensionCache.SaveAllKnownCacheTypes();
            }
            catch (Exception e)
            {
                ReportException(e);
                throw;
            }
        }
        

        ReportFileSystemExceptions();

        Working = false;
        EndTime = DateTime.Now;
        EmptyUnionResults();
        DoStatusUpdate();
        CancellationTokenSource.Cancel();
    }

    private void BuildRobotFileSummary()
    {
        if (DeferredRobotFiles.Count == 0)
        {
            return;
        }
        var list = new List<RobotFileDetails>();
        foreach (var kvp  in DeferredRobotFiles)
        {
            var newDetails = new RobotFileDetails
            {
                FileName = kvp.Key, FileSize = kvp.Value.FileSize, RobotState = kvp.Value.RobotState
            };
            list.Add(newDetails);
        }

        if (SearchQuery.EnableRobotFileFilterIgnore)
        {
            return;
        }

        var searchTaskResult = new SearchTaskResult()
        {
            RobotFileDetectionSummary = new RobotFileDetectionSummary { RobotFileDetailsList = list }
        };

        if (SearchQuery.EnableRobotFileFilterDefer)
        {
            searchTaskResult.RobotFileDetectionSummary.ActionMessage = "Set to Defer, Results to follow...";
        }

        if (SearchQuery.EnableRobotFileFilterSkipAndReport)
        {
            searchTaskResult.RobotFileDetectionSummary.ActionMessage = "Skipping search of these files...";
        }
        
        searchTaskResult.AlignIdentity(SearchQuery);
        SearchRoot.RaiseNewSearchTaskResult(searchTaskResult);
    }

    private void CleanupCache()
    {
        foreach (var extToDictionary in SearchRoot.ExtensionCache.Values)
        {
            foreach (var fileName in extToDictionary.Keys)
            {
                bool anyPathMatches = false;
                foreach (var filePath in SearchQuery.FilePaths)
                {
                    if (filePath.Folder == null)
                    {
                        continue;
                    }
                    string pathFolder = filePath.Folder.TrimEnd('\\');
                    if (fileName.StartsWith(pathFolder, StringComparison.OrdinalIgnoreCase) &&
                        pathFolder.Length < fileName.Length
                        && (fileName[pathFolder.Length] == '\\' || fileName[pathFolder.Length] == '/'))
                    {
                        if (filePath.TopLevelOnly)
                        {
                            int startIndex = pathFolder.Length + 1; 
                            if (fileName.IndexOf(Path.DirectorySeparatorChar,startIndex) != -1 
                                || fileName.IndexOf(Path.AltDirectorySeparatorChar,startIndex) != -1)
                            {
                                continue;
                            }
                            
                        }
                        anyPathMatches = true;
                        break;
                    }
                }

                if (anyPathMatches)
                {
                    continue;
                }
                
                extToDictionary.TryRemove(fileName, out _);
            }
        }
    }

    public void StartSearch()
    {
        Working = true;
        EndTime = DateTime.Now;
        // broadcast status at a consistent interval, status updates from within results tend to be u 
        Task.Run(StatusTask, CancellationTokenSource.Token);
        Task.Run(QuietTimeElapsedTask, CancellationTokenSource.Token);
        Task.Run(ClearAnyTimeTask, CancellationTokenSource.Token);
        Task.Run(DoSearchTask, CancellationTokenSource.Token);
    }

    private async void ClearAnyTimeTask()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        var searchTaskResult = new SearchTaskResult();
        searchTaskResult.AlignIdentity(SearchQuery);
        searchTaskResult.ScheduledClear = true;
        SearchRoot.RaiseNewSearchTaskResult(searchTaskResult);
    }


    private async void QuietTimeElapsedTask()
    {
        try
        {
            await Task.Delay(Searching.QuietTime + TimeSpan.FromMilliseconds(1), CancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        if (CancellationTokenSource.IsCancellationRequested) return;
        if (SearchRoot.QuietTimeElapsed(this) )
        {
            EmptyUnionResults();
        }
    }
    

    private bool DoRecycledResults()
    {
        _fileNameCount += RecycledExclusions.Count;
        if (this.RecyclingResults.Count == 0)
        {
            return false;
        }

        var taskParameters = GetSearchTaskParameters();
        var searchTaskResult = new SearchTaskResult();
        searchTaskResult.AlignIdentity(SearchQuery);

        var priorities = new Dictionary<string, int>();
        for (var index = 0; index < SearchQuery.PriorityExtensions.Count; index++)
        {
            var ext = SearchQuery.PriorityExtensions[index];
            if (ext.Extension != null)
            {
                priorities[ext.Extension] = index;
            }
        }
        var updated = RecyclingResultsInOrder.ToList();
        updated.Sort((a, b) =>
        {
            if (!priorities.TryGetValue(Path.GetExtension(a.FileName!), out var aOrder)) aOrder = priorities.Count;
            if (!priorities.TryGetValue(Path.GetExtension(b.FileName!), out var bOrder)) bOrder = priorities.Count;
            var aToBeTypeOrder = aOrder.CompareTo(bOrder);
            return aToBeTypeOrder != 0 ? aToBeTypeOrder : String.Compare(a.FileName, b.FileName, StringComparison.Ordinal);
        });

        
        foreach (var result in updated)
        {
            _fileNameCount++;
            if (SearchQuery.ReplaceInFileEnabled)
            {
                ApplyReplaceTo(result,taskParameters);
            }
            searchTaskResult.FileNames.Add(result);
        }

        SearchRoot.UnionTaskResult(searchTaskResult, this);
        return true;
    }

    enum ContentResult
    {
        Unknown,
        FoundContents,
        Skipped,
        WasRecycled,
        Deferred,
        NotFound
    }
    private ContentResult SearchContents(SearchTaskParameters taskParameters,
        FilesByExtension fileCache,
        string file,
        string extension,
        SearchTaskResult searchTaskResult,
        CancellationTokenSource? fileChangedCancellationTokenSource = null
        )
    {
        if (RetainedResults.TryGetValue(file, out var retainedValue) )
        {
            DebugPostCreation(taskParameters.DebugFileNameQuery, searchTaskResult, "RecyclingResults");
            return ContentResult.WasRecycled;
        }

        if (extension.EndsWith("~") || extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase))
        {
            DebugPostCreation(taskParameters.DebugFileNameQuery,searchTaskResult, "Is a backup file (ends with ~)");
            return ContentResult.Skipped;
        }
        try
        {
            if (TypeDetection.Instance.IsBinary(extension, file))
            {
                DebugPostCreation(taskParameters.DebugFileNameQuery,searchTaskResult, "Determined Binary");
                return ContentResult.Skipped;
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            DebugPostCreation(taskParameters.DebugFileNameQuery,searchTaskResult, "Unauthorized in Binary Detection");
            return ContentResult.Skipped;
        }

        SearchFileInformation searchFileInformation;
        if (!SearchQuery.EnableSearchIndex)
        {
            //user opted out so make sure the dictionary is cleared to retain memory.
            fileCache.Clear();
            var parsing = new SearchFileParsing(file, SearchQuery);
            searchFileInformation = parsing.ParseFile(fileCache);
        }
        else
        {
            searchFileInformation = fileCache.GetOrAdd(file, _ =>
            {
                SearchRoot.ExtensionCache.DirtyCache(extension);
                var parsing = new SearchFileParsing(file, SearchQuery);
                return parsing.ParseFile(fileCache);
            });
            if (searchFileInformation.FileState == SearchFileInformation.ReadState.Unknown)
            {
                SearchRoot.ExtensionCache.DirtyCache(extension);
                var updatedParsing = new SearchFileParsing(file, SearchQuery);
                searchFileInformation = updatedParsing.ParseFile(fileCache);
                fileCache.SetFile(file,searchFileInformation);
            }
        }

        if (searchFileInformation.RobotState != RobotFileState.LooksHuman)
        {
            if (SearchQuery.EnableRobotFileFilterIgnore)
            {
                DebugPostCreation(taskParameters.DebugFileNameQuery,searchTaskResult, "Looks like Generated");
                return ContentResult.Skipped;
            }

            //todo:
            if (SearchQuery.EnableRobotFileFilterDefer)
            {
                DeferredRobotFiles[file] = searchFileInformation;
                DebugPostCreation(taskParameters.DebugFileNameQuery,searchTaskResult, "Looks like Generated");
                return ContentResult.Deferred;
            }

            //todo
            if (SearchQuery.EnableRobotFileFilterSkipAndReport)
            {
                DeferredRobotFiles[file] = searchFileInformation;
                DebugPostCreation(taskParameters.DebugFileNameQuery,searchTaskResult, "Looks like Generated");
                return ContentResult.Skipped;
            }
        }

        if (!IsValidCache(taskParameters,searchFileInformation,fileCache,file))
        {
            DebugPostCreation(taskParameters.DebugFileNameQuery,searchTaskResult, "Invalid cache");
            return ContentResult.NotFound;
        }


        return FinalizeSearch(taskParameters,
            file,
            searchTaskResult,
            true,
            fileChangedCancellationTokenSource
        );
    }
    
    private void ApplyReplaceTo(FileNameResult fileNameResult, SearchTaskParameters taskParameters)
    {
        if(string.IsNullOrEmpty(taskParameters.ReplaceWith)) return;
        
        var contentResults = fileNameResult.ContentResults;

        var updated = new List<FileContentResult>();
        foreach (var contentResult in contentResults)
        {
            contentResult.Replacing = true;
            
            if (SearchFileContents.ReplaceMatches(contentResult.CapturedContents!, taskParameters,
                    out var replaceLine, out var blitzMatches))
            {
                contentResult.ReplacedContents = replaceLine;
                if (blitzMatches != null)
                {
                    contentResult.BlitzMatches ??= [];
                    contentResult.BlitzMatches.AddRange(blitzMatches);
                }
                updated.Add(contentResult);
            }
        }

        fileNameResult.ContentResults = updated;

    }

    private ContentResult FinalizeSearch(SearchTaskParameters taskParameters, 
        string file, 
        SearchTaskResult searchTaskResult,
        bool retainResults = true,
        CancellationTokenSource? fileChangingCancellationTokenSource = null
        )
    {
        var thisCancellation = fileChangingCancellationTokenSource ?? CancellationTokenSource;
        var searchFileContents =
            new SearchFileContents(file, taskParameters, thisCancellation);
        if (!searchFileContents.DoSearchFind(out var contentResults))
        {
            DebugPostCreation(taskParameters.DebugFileNameQuery, searchTaskResult, "DoSearchFind failed");
            return ContentResult.NotFound;
        }

        searchTaskResult.FileNames[0].ContentResults = contentResults;
        if (!retainResults || thisCancellation.IsCancellationRequested)
        {
            return ContentResult.FoundContents;
        }
        RetainedResults.TryAdd(searchTaskResult.FileNames[0].FileName!, searchTaskResult.FileNames[0]);
        return ContentResult.FoundContents;

    }
    
    
    
    private bool IsValidCache(SearchTaskParameters taskParameters, SearchFileInformation searchFileInformation, FilesByExtension filesByExtension, string filename)
    {
        if (taskParameters.RegexSearch != null )
        {
            // Searching cache for words is not ok with regex
            if (string.IsNullOrEmpty(taskParameters.TextBoxQuery?.SearchWord))
            {
                return true;
            }

            if (taskParameters.ReplaceQuery != null && string.IsNullOrEmpty(taskParameters.ReplaceQuery.SearchWord))
            {
                return true;
            }
        }
            
        var modifiedTime = new FileInfo(filename).LastWriteTime.ToUniversalTime();

        if (modifiedTime -searchFileInformation.LastModifiedTime > TimeSpan.FromMicroseconds(1))
        {
            searchFileInformation.FileState = SearchFileInformation.ReadState.Unknown;
            return false;
        }
        
        var words = filesByExtension.GetWordStrings(searchFileInformation.UniqueWords);
        if (taskParameters.TextBoxQuery != null)
        {
            foreach (var subQuery in taskParameters.TextBoxQuery.SubQueries)
            {
                if (subQuery is not (BlitzWordInQuery or BlitzOrQuery))
                {
                    continue;
                }

                if (!subQuery.SearchIndexValidFor(words))
                {
                    return false;
                }
            }
        }

        if (taskParameters.ReplaceQuery != null && !taskParameters.ReplaceQuery.SearchIndexValidFor(words))
        {
            return false;
        }
        
        return true;
    }

    private void ReportException(Exception ex)
    {
        var searchTaskResult = new SearchTaskResult();
        searchTaskResult.AlignIdentity(SearchQuery);
        searchTaskResult.Exceptions.Add(ExceptionResult.CreateFromException(ex));
        SearchRoot.RaiseNewSearchTaskResult(searchTaskResult);
    }

    private void ReportFileSystemExceptions()
    {
        if (SearchRoot.FileDiscoverer == null) return;
        
        if (!SearchQuery.VerboseFileSystemException || SearchRoot.FileDiscoverer.ErrorInFileDiscoverMessage.Count == 0)
        {
            return;
        }

        var searchTaskResult = new SearchTaskResult();
        searchTaskResult.AlignIdentity(SearchQuery);
        searchTaskResult.Exceptions.AddRange(SearchRoot.FileDiscoverer.ExceptionResults);
        SearchRoot.RaiseNewSearchTaskResult(searchTaskResult);
    }

    private bool ReportPreSearchProblem(out SearchTaskResult searchTaskResult)
    {
        searchTaskResult = new SearchTaskResult();
        searchTaskResult.AlignIdentity(SearchQuery);
        if (SearchRoot.FileDiscoverer == null)
        {
            return true;
        }
        if (SearchRoot.FileDiscoverer.ErrorInFileDiscoverMessage.Count > 0)
        {
            searchTaskResult.MissingRequirements.AddRange(SearchRoot.FileDiscoverer.ErrorInFileDiscoverMessage);
        }

        if (SearchQuery.TextBoxQuery.Trim().Length == 0 && !SearchQuery.ReplaceInFileEnabled && !SearchQuery.LiteralSearchEnabled && !SearchQuery.RegexSearchEnabled) 
        {
            searchTaskResult.MissingRequirements.Add(new MissingRequirementResult()
                { CustomMessage = "Enter a Search Term..", MissingRequirement = MissingRequirementResult.Requirement.SearchWords});
        }

        if (SearchQuery.ReplaceInFileEnabled)
        {
            BlitzAndQuery.QueryMatches(SearchQuery.ReplaceTextQuery ?? string.Empty , out var replaceTextQuery);
            if (string.IsNullOrEmpty(SearchQuery.ReplaceLiteralTextQuery) && replaceTextQuery.SubQueries.Count > 1 || replaceTextQuery.SubQueries.Count == 0)
            {
                searchTaskResult.MissingRequirements.Add(new MissingRequirementResult()
                    { CustomMessage = "Enter a single word for replace, can use the '|' and '@' Operators..", MissingRequirement = MissingRequirementResult.Requirement.ReplaceWords});
            }
            else if (replaceTextQuery.SubQueries.Count == 1)
            {
                if (replaceTextQuery.SubQueries.First() is BlitzWordInQuery { IsExclude: true })
                {
                    searchTaskResult.MissingRequirements.Add(new MissingRequirementResult()
                        { CustomMessage = "'!' Is not a valid replacement query", MissingRequirement = MissingRequirementResult.Requirement.ReplaceWords});
                }
            }
        }

        if (!string.IsNullOrEmpty(SearchQuery.RegexSearchQuery))
        {
            try
            {
                _ = new Regex(SearchQuery.RegexSearchQuery);
            }
            catch (ArgumentException exception)
            {
                searchTaskResult.MissingRequirements.Add(new MissingRequirementResult()
                    { CustomMessage = exception.Message, MissingRequirement = MissingRequirementResult.Requirement.SearchWords });
            }
        }

        
        
        return searchTaskResult.MissingRequirements.Count > 0;
    }

    private void ReportFilesNotFoundMessage(string rawSearchString, string? rawFileNamestring)
    {
        var searchTaskResult = new SearchTaskResult();
        searchTaskResult.AlignIdentity(SearchQuery);

        var message = new StringBuilder();
        if (SearchQuery.ReplaceInFileEnabled && !string.IsNullOrEmpty(SearchQuery.ReplaceTextQuery))
        {
            message.AppendLine($"No results for '{rawSearchString}' AND replace '{SearchQuery.ReplaceTextQuery}'");
            
        }
        else if( !string.IsNullOrEmpty(rawSearchString))
        {
            message.AppendLine($"No results for '{rawSearchString}'");
        }

        if (SearchQuery.RegexSearchEnabled && !string.IsNullOrEmpty(SearchQuery.RegexSearchQuery) )
        {
            try
            {
                _ = new Regex(SearchQuery.RegexSearchQuery);

            }
            catch (ArgumentException ex)
            {
                message.AppendLine($"Expression Error: '{ex.Message}'");
            }

        }
        
        message.AppendLine();
        message.AppendLine($"{_fileNameCount} files were searched");
        message.AppendLine();
        message.AppendLine("In folders:");
        foreach (var filePath in SearchQuery.FilePaths)
        {
            message.Append(filePath.Folder);
            if (filePath.TopLevelOnly)
            {
                message.Append("(TOP LEVEL ONLY)");
            }

            message.AppendLine();
        }

        if (SearchQuery.FileNameQueryEnabled && !string.IsNullOrEmpty(rawFileNamestring))
        {
            message.AppendLine();
            message.AppendLine("With FileName Filter:");
            message.AppendLine(rawFileNamestring);
        }

        searchTaskResult.MissingRequirements.Add(new MissingRequirementResult()
        {
            MissingRequirement = MissingRequirementResult.Requirement.SearchWords, CustomMessage = message.ToString()
        });
        if (CancellationTokenSource.IsCancellationRequested)
        {
            return;
        }

        SearchRoot.RaiseNewSearchTaskResult(searchTaskResult);
    }

    public void EmptyUnionResults()
    {
        if (UnionResults.FileNames.Count <= 0)
        {
            return;
        }
        var unionResults = UnionResults;
        UnionResults = new SearchTaskResult();
        SearchRoot.RaiseNewSearchTaskResult(unionResults);
    }

    public void UpdateFileChanged(string changedFile, CancellationTokenSource fileNameCancel)
    {
        bool presentThisFile = false;
        bool foundAnything = false;

        string extension = Path.GetExtension(changedFile);
        if (!SearchRoot.ExtensionCache.TryGetValue(extension, out var filesByExtension))
        {
            return;
        }

        if (extension.EndsWith("~") || extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        
        var taskParameters = GetSearchTaskParameters();
        var searchTaskResult = InitializeSearch(taskParameters,changedFile,ref presentThisFile, ref foundAnything);

        RetainedResults.TryRemove(changedFile, out _);
        FilesWithNoResults.TryRemove(changedFile, out _);
        var unlocked = RecyclingResultsInOrder.ToList();
        if( RecyclingResults.TryGetValue(changedFile, out var item))
        {
            unlocked.Remove(item);
            RecyclingResultsInOrder = unlocked.ToImmutableList();
        }

        var newRecycleDictionary = new Dictionary<string, FileNameResult>();
        foreach (var result in unlocked)
        {
            newRecycleDictionary[result.FileName!] = result;
        }
        RecyclingResults = newRecycleDictionary.ToImmutableDictionary();
        try
        {
            var fileCache = SearchRoot.ExtensionCache.GetOrAdd(extension,
                _ => new FilesByExtension());

            var result = SearchContents(taskParameters,fileCache,changedFile,extension,searchTaskResult, fileNameCancel );
                    
            if (result == ContentResult.FoundContents)
            {
                presentThisFile = true;
                foundAnything = true;
            }
            else if(result == ContentResult.NotFound) 
            {
                if (!foundAnything)
                {
                    DebugPostCreation(taskParameters.DebugFileNameQuery, searchTaskResult, "No Contents" );
                    FilesWithNoResults.TryAdd(changedFile,1);
                }
                else
                {
                    RetainedResults.TryAdd(searchTaskResult.FileNames[0].FileName!, searchTaskResult.FileNames[0]);
                }
            }
        }
        catch (Exception ex ) when (ex is UnauthorizedAccessException or IOException)
        {
            if (SearchQuery.VerboseFileSystemException)
            {
                ReportException(ex);
            }
        }
        
        var fileNames = searchTaskResult.FileNames;
        searchTaskResult.FileNames = new();
        searchTaskResult.ChangedFileNames = fileNames;
        SearchRoot.RaiseNewSearchTaskResult(searchTaskResult);
    }
}
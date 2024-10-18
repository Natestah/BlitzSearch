using System;
using System.Collections.Generic;
using System.Diagnostics;
using Humanizer;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Media;
using Blitz.Goto;
using TextMateSharp.Grammars;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using AvaloniaEdit.TextMate;
using AvaloniaEdit.Utils;
using Blitz.Interfacing;
using Blitz.AvaloniaEdit.Models;
using Blitz.AvaloniaEdit.ViewModels;
using Material.Icons;
using Application = Avalonia.Application;


namespace Blitz.Avalonia.Controls.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public RegistryOptions TextMateRegistryOptions
    {
        get
        {
            string themeString = Configuration.Instance.CurrentTheme.ThemeName;
            if (Enum.TryParse(themeString, out ThemeName themeName))
            {
                EditorViewModel.ConfiguredThemeName = themeName;
            }

            return EditorViewModel.TextMateRegistryOptions;
        }
        set => EditorViewModel.TextMateRegistryOptions = value;
    }

    public SearchQuery SearchQuery => _searchQuery;

    public AdsCollection AdsCollection { get; }

    bool ApplyBrushAction(TextMate.Installation e, string colorKeyNameFromJson, Action<IBrush> applyColorAction)
    {
        if (!e.TryGetThemeColor(colorKeyNameFromJson, out var colorString))
            return false;

        if (!Color.TryParse(colorString, out Color color))
            return false;

        var colorBrush = new SolidColorBrush(color);
        applyColorAction(colorBrush);
        return true;
    }

    public bool TimerDisplayTotalSearchTIme
    {
        get => Configuration.Instance.ShowTotalSearchTime;
        set
        {
            Configuration.Instance.ShowTotalSearchTime = value;
            this.RaisePropertyChanged();
        }
    }


    public double GeneralIconSize
    {
        get => Configuration.Instance.EditorConfig.GeneralIconSize;
        set
        {
            Configuration.Instance.EditorConfig.GeneralIconSize = value;
            this.RaisePropertyChanged();
        }
    }

    public bool ShowStatusBar
    {
        get => NewVersionAvailable || Configuration.Instance.ShowStatusBar;
        set
        {
            Configuration.Instance.ShowStatusBar = value;
            this.RaisePropertyChanged();
        }
    }

    public BlitzEditorViewModel EditorViewModel { get; set; } = new BlitzEditorViewModel();

    public IBrush? TextForeground
    {
        get => _textForeground;
        set => this.RaiseAndSetIfChanged(ref _textForeground, value);
    }


    public BlitzEditorViewModel.InstallationInstallerDelegate? TextMateInstaller => EditorViewModel.TextMateInstaller;
    
    public TextMate.Installation? TextMateInstallation
    {
        get => _textMateInstallation;
        set => this.RaiseAndSetIfChanged(ref _textMateInstallation, value);
    }

    public void UpdateRegistryOptions() => this.RaisePropertyChanged(nameof(TextMateRegistryOptions));

    public event EventHandler? SelectedFileChanged; 
    
    public void RefreshRegistryOptions()
    {
        this.RaisePropertyChanged(nameof(TextMateRegistryOptions));
    }

    public MainWindowViewModel( ISearchingClient searchingClient)
    {
        foreach (var gotoEditor in new GotoDefinitions().GetBuiltInEditors())
        {
            GotoEditorCollection.Add(new GotoEditorViewModel(gotoEditor));
        }
        
        SearchingClient = searchingClient;
        _searchQuery = new SearchQuery(String.Empty, [],[],true){ProcessIdentity = Environment.ProcessId};
        _resultBoxItems = new ObservableCollection<object>{};
        _selectedItems = new ObservableCollection<object>();
        _fileSearchStatus= new FileDiscoveryStatusViewModel(this);

        PropertyChanged += OnPropertyChanged;
        SearchingClient.ReceivedFileResultEventHandler+=SearchingClientOnReceivedFileResultEventHandler;
        
        
        CopyCommand = ReactiveCommand.Create(CopyCommandRun);
        GotoSelectedExternal = ReactiveCommand.Create(GotoSelectedExternalRun);
        GotoSelectedExplorer = ReactiveCommand.Create(GotoSelectedExplorerRun);
        GotoSelectedCmd = ReactiveCommand.Create(GotoSelectedCmdRun);
        ToggleTextEditCmd = ReactiveCommand.Create(ToggleTextEditRun);
        ToggleFileNameFilterCommand = ReactiveCommand.Create(ToggleFileNameFilterCommandRun);
        ToggleLiteralSearchCommand = ReactiveCommand.Create(ToggleLiteralSearchCommandRun);
        ToggleRegexSearchCommand = ReactiveCommand.Create(ToggleRegexSearchCommandRun);
        ShowPreviewTextCommand = ReactiveCommand.Create(ShowPreviewTextCommandCommandRun);
        HidePreviewPaneCommand = ReactiveCommand.Create(HidePreviewPaneCommandRun);
        ToggleFindInFilesFilterCommand = ReactiveCommand.Create(ToggleFindInFilesFilterCommandRun);
        
        
        LoadQuery();

        if (Configuration.Instance.ScopeConfigs.Count == 0)
        {
            Configuration.Instance.ScopeConfigs.Add(new ScopeConfig { ScopeTitle = "Untitled"});
        }


        foreach (var item in ReplaceModeViewModels)
        {
            if (item.Title == Configuration.Instance.ReplaceMode)
            {
                SelectedReplaceMode = item;
            }
        }
        
        BuildScopesViewModelsFromConfig();
        AdsCollection = new AdsCollection(this);

    }

    public void BuildScopesViewModelsFromConfig()
    {
        ScopeViewModels.Clear();
        //SelectedScope = null;
        ScopeViewModel? selected = null;
        foreach (var configScope in Configuration.Instance.ScopeConfigs)
        {
            var vm = new ScopeViewModel(this, configScope);
            this.ScopeViewModels.Add(vm);
            if (vm.ScopeTitle == Configuration.Instance.SelectedScope)
            {
                selected = vm;
            }
        }
        selected ??= ScopeViewModels.FirstOrDefault();
        Dispatcher.UIThread.Invoke(() => { SelectedScope = selected!; }, DispatcherPriority.Background);
       
    }

    public Action<string>? ShowImportantMessage { get; set; }
    public Action<TextMate.Installation>? BackGroundForeGroundUpdate;

    private BlitzTheme FromBase(BlitzTheme baseTheme,ThemeName themeName)
    {
        return new BlitzTheme
        {
            TextForeground = baseTheme.TextForeground,
            WindowBackground = baseTheme.WindowBackground,
            PassiveIcon = baseTheme.PassiveIcon,
            ContentHighlightBackground = baseTheme.ContentHighlightBackground,
            ContentHighlightBorder = baseTheme.ContentHighlightBorder,
            ContentHighlightReplaceBackground = baseTheme.ContentHighlightReplaceBackground,
            ContentHighlightReplaceBorder = baseTheme.ContentHighlightReplaceBorder,
            SelectedItemBackground =baseTheme.SelectedItemBackground,
            AvaloniaThemeVariant = baseTheme.AvaloniaThemeVariant,
            ThemeName = themeName.ToString()
        };
    }

    public void PopulateThemeModels()
    {
        AllThemeViewModels.Add(new BlitzThemeViewModel(this, BlitzTheme.Dark, true));
        AllThemeViewModels.Add(new BlitzThemeViewModel(this, BlitzTheme.Light, true));
        foreach (ThemeName themeName in Enum.GetValuesAsUnderlyingType(typeof(TextMateSharp.Grammars.ThemeName)))
        {
            if (themeName is ThemeName.DarkPlus or ThemeName.Light)
            {
                continue;
            }
            
            
            var newBlitzTHeme = themeName.ToString().ToLower().Contains("light") ? FromBase(BlitzTheme.Light, themeName) : FromBase(BlitzTheme.Dark, themeName);
            AllThemeViewModels.Add( new BlitzThemeViewModel(this, newBlitzTHeme, false));
        }

        foreach (var themeViewModel in AllThemeViewModels)
        {
            if ( themeViewModel.Theme.ThemeName == Configuration.Instance.SelectedThemePremium)
            {
                this.BlitzThemeViewModel = themeViewModel;
                return;
            }
        }

        this.BlitzThemeViewModel = AllThemeViewModels.First();
    }

    public bool SearchResultsHitTestEnabled
    {
        get => _searchResultsHitTestEnabled;
        set => this.RaiseAndSetIfChanged(ref _searchResultsHitTestEnabled,value);
    }

    public WindowState LastNonMinizedState { get; set; } = WindowState.Normal;
    
    

    private GotoEditorViewModel _selectedEditorViewModel =
        new GotoEditorViewModel(Configuration.Instance.GotoEditor);

    private bool _newVersionAvailable;
    private BlitzThemeViewModel? _blitzThemeViewModel = null;
    private string? _cacheStatus;
    private bool _cacheCleaning;


    public string Version => Assembly.GetEntryAssembly()!.GetName().Version!.ToString().TrimEnd();

    public string WorkingSet => Process.GetCurrentProcess().WorkingSet64.Bytes().Humanize();

    public void UpdateWorkingSet()
    {
        this.RaisePropertyChanged(nameof(WorkingSet));
    }
    
    /// <summary>
    /// Searching Client handles all search requests
    /// </summary>
    private ISearchingClient SearchingClient { get; set; }
                                                                               
    private SingleTask SaveQueryTask => _saveQueryTask ??= new SingleTask(SaveQuery);
    public ReactiveCommand<Unit,Unit> CopyCommand { get; set; }
    public ReactiveCommand<Unit,Unit> GotoSelectedExternal { get; set; }
    public ReactiveCommand<Unit,Unit> GotoPreviewLineSelectedExternal { get; set; }
    public ReactiveCommand<Unit,Unit> GotoSelectedExplorer { get; set; }
    public ReactiveCommand<Unit,Unit> GotoSelectedCmd { get; set; }
    
    public ReactiveCommand<Unit,Unit> ToggleTextEditCmd { get; set; }
    public ReactiveCommand<Unit,Unit> ToggleFileNameFilterCommand { get; set; }
    public ReactiveCommand<Unit,Unit> ToggleLiteralSearchCommand { get; set; }
    public ReactiveCommand<Unit,Unit> ToggleRegexSearchCommand { get; set; }
    
    public ReactiveCommand<Unit,Unit> ShowPreviewTextCommand { get; set; }
    public ReactiveCommand<Unit,Unit> HidePreviewPaneCommand { get; set; }
    public ReactiveCommand<Unit,Unit> ToggleFindInFilesFilterCommand { get; set; }
    public ObservableCollection<string> GetHistoryFromConfiguration(List<string> collection)
    {
        var config = new ObservableCollection<string>(collection);
        config.CollectionChanged += (sender, args) =>
        {
            collection.Clear();
            collection.AddRange(config);
        };
        return config;
    }

    private ObservableCollection<string>? _searchTextHistory;
    private ObservableCollection<string>? _searchFileHistory;
    private ObservableCollection<string>? _pathFolderHistory;
    private ObservableCollection<string>? _replaceHistory;
    private ObservableCollection<string>? _replaceWithHistory;
    private ObservableCollection<string>? _literalSearchHistory;
    private ObservableCollection<string>? _regexSearchHistory;

    public ObservableCollection<string> LiteralSearchTextHistory => _literalSearchHistory ??= GetHistoryFromConfiguration(Configuration.Instance.LiteralSearchTextHistory);
    public ObservableCollection<string> RegexSearchTextHistory => _regexSearchHistory ??= GetHistoryFromConfiguration(Configuration.Instance.RegexSearchTextHistory);
    public ObservableCollection<string> SearchTextHistory => _searchTextHistory ??= GetHistoryFromConfiguration(Configuration.Instance.SearchTextHistory);
    public ObservableCollection<string> SearchFileHistory =>  _searchFileHistory ??= GetHistoryFromConfiguration(Configuration.Instance.SearchFileNameTextHistory);
    public ObservableCollection<string> PathFolderHistory =>  _pathFolderHistory ??= GetHistoryFromConfiguration(Configuration.Instance.PathFolderHistory);
    public ObservableCollection<string> ReplaceHistory =>  _replaceHistory ??= GetHistoryFromConfiguration(Configuration.Instance.ReplaceHistory);
    public ObservableCollection<string> ReplaceWithHistory =>  _replaceWithHistory ??= GetHistoryFromConfiguration(Configuration.Instance.ReplaceWithHistory);

    public GotoEditorViewModel? SelectedEditorViewModel
    {
        get => _selectedEditorViewModel;
        set => this.RaiseAndSetIfChanged(ref _selectedEditorViewModel!,  value);
    }
    
    public ObservableCollection<GotoEditorViewModel> GotoEditorCollection { get; } = [];

    public bool NewVersionAvailable
    {
        get => _newVersionAvailable;
        set
        {
            this.RaiseAndSetIfChanged(ref _newVersionAvailable, value);
            this.RaisePropertyChanged(nameof(ShowStatusBar));
        }
    }

    public string NewVersionString
    {
        get => _newVersionString;
        set => this.RaiseAndSetIfChanged(ref _newVersionString, value);
    }

    public BlitzThemeViewModel? BlitzThemeViewModel
    {
        get => _blitzThemeViewModel;
        set
        {
            if (value == null) return;
            Configuration.Instance.CurrentTheme = value.Theme;
            Configuration.Instance.SelectedThemePremium = value.ThemeName.ToString();
            _blitzThemeViewModel = value;
            UpdateTheme();
        }
    }

    public FontFamily SelectedFontFamily
    {
        get => EditorViewModel.SelectedFontFamily;
        set
        {
            Configuration.Instance.EditorConfig.FontFamily = value.Name;
            bool raise = this.EditorViewModel.SelectedFontFamily != value; 
            this.EditorViewModel.SelectedFontFamily = value;
            if (raise)
            {
                this.RaisePropertyChanged();
            }
        }
    }

    public double LineSpacing
    {
        get => Configuration.Instance.EditorConfig.LineSpacing;
        set
        {
            Configuration.Instance.EditorConfig.LineSpacing = value;
            this.RaisePropertyChanged();
        }
    }

    
    public double FontSize
    {
        get => Configuration.Instance.EditorConfig.FontSize;
        set
        {
            Configuration.Instance.EditorConfig.FontSize = value;
            this.RaisePropertyChanged();
        }
    }

    private ObservableCollection<FontFamily>? _fontFamilies;

    public ObservableCollection<FontFamily> FontFamilies
    {
        get
        {
            if (_fontFamilies != null)
            {
                return _fontFamilies;
            }

            var families = new ObservableCollection<FontFamily>();
            
            foreach (var font in FontManager.Current.SystemFonts.OrderBy(f => f.Name))
            {
                //https://github.com/AvaloniaUI/Avalonia/issues/16145
                if ("Marlett".Equals(font.Name))
                {
                    continue;
                }
                families.Add(font);
            }
            return _fontFamilies = families;
        }
    }

    public ObservableCollection<BlitzThemeViewModel> AllThemeViewModels { get; } = [];

    public bool FileNameSearchEnabled
    {
        get => _searchQuery.FileNameQueryEnabled;
        set
        {
            _searchQuery.FileNameQueryEnabled = value;
            this.RaisePropertyChanged(nameof(FileNameSearchEnabled));
        }
    }

    public string LiteralSearchHeader => "Literal Search";
    public string RegexMatchHeader => "RegExp Search";
    
    public bool LiteralSearchEnabled
    {
        get => _searchQuery.LiteralSearchEnabled;
        set
        {
            _searchQuery.LiteralSearchEnabled = value;
            this.RaisePropertyChanged();
        }
    }
    public bool RegexSearchEnabled
    {
        get => _searchQuery.RegexSearchEnabled;
        set
        {
            _searchQuery.RegexSearchEnabled = value;
            this.RaisePropertyChanged();
        }
    }
    public bool ReplaceInFileEnabled
    {
        get => _searchQuery.ReplaceInFileEnabled;
        set
        {
            if (!_searchQuery.ReplaceInFileEnabled)
            {
                var potentialQueries = _searchQuery.TextBoxQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (potentialQueries.Length > 0)
                {
                    var chosenQuery = potentialQueries.Last();
                    var trimStart = chosenQuery.TrimStart("@|".ToCharArray());
                    if (!string.IsNullOrEmpty(trimStart))
                    {
                        ReplaceBoxText = trimStart;
                        ReplaceWithBoxText = trimStart;
                    }
                }
                

            }
            _searchQuery.ReplaceInFileEnabled = value;
            this.RaisePropertyChanged(nameof(ReplaceInFileEnabled));
        }
    }
    private ObservableCollection<ReplaceModeViewModel>? _replaceModeViewModels;
    public ObservableCollection<ReplaceModeViewModel> ReplaceModeViewModels
    {
        get
        {
            return _replaceModeViewModels ??=
            [
                new ReplaceModeViewModel(this, MaterialIconKind.FileWordBox, "Word", "Single word, supports '@' (whole word) and '|' (OR words)"),
                new ReplaceModeViewModel(this, MaterialIconKind.TextBoxSearch, "Literal", "Literal search, everything typed gets replaced"),
                new ReplaceModeViewModel(this, MaterialIconKind.RegularExpression, "Regular Expression", "Traditional Regular Expression")
            ];
        }
    }

    public ReplaceModeViewModel SelectedReplaceMode
    {
        get => _selectedReplaceMode;
        set
        {
            Configuration.Instance.ReplaceMode = value.Title;
            this.RaiseAndSetIfChanged(ref _selectedReplaceMode, value);
        }
    }

    public bool ShowOnTaskBar
    {
        get => Configuration.Instance.ShowOnTaskBar;
        set
        {
            Configuration.Instance.ShowOnTaskBar = value;
            this.RaisePropertyChanged();
        }
    }

    public bool SplitPane
    {
        get => Configuration.Instance.SplitPane;
        set
        {
            Configuration.Instance.SplitPane = value;
            if (!value)
            {
                DeselectAllPanes();
            }
            this.RaisePropertyChanged();
        }
    }

    public void UpdateTheme()
    {
        ResultsHighlighting.Instance.InstallHighlighting();
        RefreshBoxItemHighlights(); // syntax highlighting, sim^ply redo the search.

        TextMateRegistryOptions = new RegistryOptions(_blitzThemeViewModel!.ThemeName);
        this.RaisePropertyChanged(nameof(BlitzThemeViewModel));
        
    }

    public bool CacheCleaning
    {
        get => _cacheCleaning;
        set => this.RaiseAndSetIfChanged(ref _cacheCleaning, value);
    }

    public string? CacheStatus
    {
        get => _cacheStatus;
        set => this.RaiseAndSetIfChanged(ref _cacheStatus, value);
    }
    
    public bool IsMissingScopeRequirements
    {
        get => _isMissingScopeRequirements;
        set => this.RaiseAndSetIfChanged(ref _isMissingScopeRequirements, value);
    }

    private async void CopyCommandRun()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard is not { } provider)
            throw new NullReferenceException("Missing Clipboard instance.");

        var copyBuilder = new StringBuilder();
        foreach (var copyable in SelectedItems.OfType<IResultCopiable>())
        {
            copyBuilder.AppendLine(copyable.CopyText);
        }
        await provider.SetTextAsync(copyBuilder.ToString());
    }

    private void GotoSelectedExplorerRun()
    {
        if (SelectedEditorViewModel != null && !SelectedEditorViewModel.RunOpenExplorerOnObject(SelectedItems.FirstOrDefault()))
        {
            return;
        }
    }

    private void GotoSelectedCmdRun()
    {
        if (SelectedEditorViewModel != null && !SelectedEditorViewModel.RunOpenCmdOnObject(SelectedItems.FirstOrDefault()))
        {
        }
    }
    private void ToggleTextEditRun()
    {
        SplitPane = !SplitPane;
        JiggleSelection();
    }


    private void GotoSelectedExternalRun()
    {
        if (SelectedEditorViewModel != null && !SelectedEditorViewModel.RunTotoOnObjectGoto(SelectedItems.FirstOrDefault(), out var errorMessage))
        {
        }
    }



    private void ToggleFindInFilesFilterCommandRun()
    {
        ReplaceInFileEnabled = !ReplaceInFileEnabled;
    }

    private void ToggleFileNameFilterCommandRun()
    {
        FileNameSearchEnabled = !FileNameSearchEnabled;
    }
    
    
    private void ToggleLiteralSearchCommandRun()
    {
        LiteralSearchEnabled = !LiteralSearchEnabled;
    }

    
    private void ToggleRegexSearchCommandRun()
    {
        RegexSearchEnabled = !RegexSearchEnabled;
    }


    private void ShowPreviewTextCommandCommandRun()
    {
        bool wasShown = EnableTextPane && SplitPane;
        EnableTextPane = true;
        SplitPane = true;
        if (!wasShown)
        {
            JiggleSelection();
        }
    }

    public void JiggleSelection()
    {
        //Todo: fix funky AvalonEdit interactions so I can operate a little better here. 
        // Stimulate a selection changed event in order to show the things.
        var selected = SelectedItems.ToList();
        SelectedItems.Clear();
        SelectedItems.AddRange(selected);
    }
    
    
    private void HidePreviewPaneCommandRun()
    {
        SplitPane = false;
    }

    public object SelectedScope 
    {
        get => _selectedScope ?? new ScopeViewModel(this, new ScopeConfig(){ ScopeTitle = "Hi"});
        set
        {
            if (value == null)
            {
                return;
            }

            _selectedScope = value;
            if (value is ScopeViewModel scopeViewModel )
            {
                Configuration.Instance.SelectedScope = scopeViewModel.ScopeTitle;
                WorkingScope = scopeViewModel;
            }
            this.OnPropertyChangedFileSystemRestart(this,
                new PropertyChangedEventArgs(nameof(SelectedScope)));
        }
    }

    private void SearchingClientOnReceivedFileResultEventHandler(object? sender, SearchTaskResult searchTaskResult)
    {
        Dispatcher.UIThread.Post(()=>ProcessSearchTaskResult(searchTaskResult));
    }

    private void DoScheduledClear()
    {
        if (!_scheduledClear) return;
        _resultBoxItems.Clear();
        _scheduledClear = false;
    }
    private void ProcessSearchTaskResult(SearchTaskResult searchTaskResult)
    {
        if (!searchTaskResult.QueryMatches(_searchQuery))
        {
            return;
        }

        if (searchTaskResult.ScheduledClear)
        {
            DoScheduledClear();
        }
        

        var timeSinceInput = DateTime.Now - LastInputTime;
        var quietTime = DefaultSettings.QuietUITime;
        var fadeTime = TimeSpan.FromMilliseconds(400);
        if (timeSinceInput < quietTime)
        {
            FileSearchStatus.ProgressOpacity = 0;
        }
        else
        {
            var timeAfterInput = timeSinceInput - quietTime;
            if (timeAfterInput > fadeTime)
            {
                FileSearchStatus.ProgressOpacity = 1;
            }
            else
            {
                FileSearchStatus.ProgressOpacity = timeAfterInput / fadeTime;
            }
        }

        if (searchTaskResult.RobotFileDetectionSummary.RobotFileDetailsList.Count > 0)
        {
            DoScheduledClear();
            ResultBoxItems.Add(new RobotFileSummaryViewModel(searchTaskResult.RobotFileDetectionSummary));
        }


        string? selectedFilename = null;
        

        foreach (var item in SelectedItems)
        {
            if (item is ContentResultViewModel contentResultViewModel)
            {
                selectedFilename = contentResultViewModel.FileNameResult.FileName;
            }
            else if (item is FileNameResult fileNameResult)
            {
                selectedFilename = fileNameResult.FileName;
            }
        }

        if (!string.IsNullOrEmpty(_searchQuery.TextBoxQuery))
        {
            foreach (var changedFile in searchTaskResult.ChangedFileNames)
            {
                bool foundThis = false;
                if (selectedFilename != null && changedFile.FileName == selectedFilename)
                {
                    SelectedFileChanged?.Invoke(new ReloadPreviewRequest(selectedFilename) , new EventArgs());
                }
                for (int i = 0; i < ResultBoxItems.Count; i++)
                {
                    if (ResultBoxItems[i] is FileNameResultViewModel fileNameResultViewModel && fileNameResultViewModel.FileName == changedFile.FileName )
                    {
                        foundThis = true;
                        i++;
                        while (i < ResultBoxItems.Count && ResultBoxItems[i] is not FileNameResultViewModel)
                        {
                            ResultBoxItems.RemoveAt(i);
                        }

                        foreach (var content in changedFile.ContentResults)
                        {
                            ResultBoxItems.Insert(i,new ContentResultViewModel(this,content,changedFile));
                            i++;
                        }

                        fileNameResultViewModel.IsUpdated = true;
                    }
                }

                bool isMatchInFilename = changedFile.BlitzMatches != null && changedFile.BlitzMatches.Count > 0;
            
                if (!foundThis && (isMatchInFilename || changedFile.ContentResults.Count > 0) ) 
                {
                    ResultBoxItems.Add(new FileNameResultViewModel(this,changedFile){IsUpdated = true});
                    foreach (var item in changedFile.ContentResults)
                    {
                        ResultBoxItems.Add(new ContentResultViewModel(this,item,changedFile));
                    }
                }
            }
        }
        

        foreach(var fileResult in searchTaskResult.FileNames )
        {
            if (!searchTaskResult.QueryMatches(_searchQuery))
            {
                return;
            }
            DoScheduledClear();
            ResultBoxItems.Add(new FileNameResultViewModel(this,fileResult));
            if (fileResult.ContentResults.Count <= 0)
            {
                continue;
            }
            foreach (var contentResult in fileResult.ContentResults)
            {
                ResultBoxItems.Add(new ContentResultViewModel(this, contentResult, fileResult));
            }
        }

        if (searchTaskResult.MissingRequirements.Count > 0)
        {
            bool isScope = false;
            foreach (var result in searchTaskResult.MissingRequirements)
            {
                DoScheduledClear();

                var newMissingRequirement = new MissingRequirementsViewModel(result);
                ResultBoxItems.Add(newMissingRequirement);
                if (result.MissingRequirement == MissingRequirementResult.Requirement.FileDirectory ||
                    result.MissingRequirement == MissingRequirementResult.Requirement.FileExtension)
                {
                    isScope = true;
                }
            }

            IsMissingScopeRequirements = isScope;
        }


        if (searchTaskResult.FileSearchStatus is { StatusUpdated: true } fileDiscoveryStatus)
        {
            FileSearchStatus.UpdateStatus(fileDiscoveryStatus);
        }

        if (searchTaskResult.Exceptions.Count > 0)
        {
            foreach (var exception in searchTaskResult.Exceptions)
            {
                DoScheduledClear();
                var exceptionResultViewModel = new ExceptionViewModel(exception);
                ResultBoxItems.Add(exceptionResultViewModel);
            }
            
        }
    }

    private DateTime LastInputTime = DateTime.MinValue;
    private bool _scheduledClear = true;
    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IsMissingScopeRequirements) 
            or nameof(BlitzThemeViewModel) 
            or nameof(CacheStatus) 
            or nameof(TextMateRegistryOptions) 
            or nameof(TextMateInstallation) 
            or nameof(SelectedFontFamily)
            or nameof(EnableGotoPane)
            or nameof(EnableHelpPane)
            or nameof(EnableSettingsPane)
            or nameof(EnableTextPane)
            or nameof(EnableScopePane)
            or nameof(EnableThemePane)
            or nameof(SplitPane)
            )
        {
            return;
        }
        
        _scheduledClear = true;
        LastInputTime = DateTime.Now;;
        IsMissingScopeRequirements = false;
        ApplyConfigurationToQuery();
        SearchingClient.PostSearchRequest(_searchQuery, false);
        SaveQueryTask.Run();
    }

    public void ScopeChangedRunRestart()
    {
        _scheduledClear = true;
        LastInputTime = DateTime.Now;;
        IsMissingScopeRequirements = false;
        ApplyConfigurationToQuery();
        SearchingClient.PostSearchRequest(_searchQuery, true);
        SaveQueryTask.Run();
    }
    
    private void OnPropertyChangedFileSystemRestart(object? sender, PropertyChangedEventArgs e)
    {
        ScopeChangedRunRestart();
    }

    private void ApplyConfigurationToQuery()
    {
        var robotSetting = Configuration.Instance.RobotDetectionSettings; 
        _searchQuery.EnableRobotFileFilterIgnore = robotSetting.BehaviorIgnoreRobotFiles;
        _searchQuery.EnableRobotFileFilterSkipAndReport = robotSetting.BehaviorSkipAndReport;
        _searchQuery.EnableRobotFileFilterDefer = robotSetting.BehaviorDefer;
        _searchQuery.RobotFilterMaxSizeMB = robotSetting.MaxFileSizeInMB;
        _searchQuery.RobotFilterMaxLineChars = robotSetting.MaxLineSizeChars;

        _searchQuery.RegexCaseSensitive = IsRegexCaseSensitive;
        _searchQuery.LiteralCaseSensitive = IsLiteralCaseSensitive || IsSmartLiteralCaseSensitive;
        _searchQuery.ReplaceCaseSensitive = IsReplaceCaseSensitive || IsSmartReplaceCaseSensitive;
        _searchQuery.SmartCase = IsSmartCaseSensitive;

        var selectedFirst = SelectedScope  as ScopeViewModel;

        var filePaths = new List<SearchPath>();
        if (selectedFirst != null)
        {
            if (!string.IsNullOrEmpty(selectedFirst.ExtensionValidation))
            {
                _searchQuery.RawExtensionList = selectedFirst?.ExtensionText;
            }
            else
            {
                _searchQuery.RawExtensionList = string.Empty;
            }
            
            foreach (var path in selectedFirst.SearchPathViewModels)
            {
                filePaths.Add(new SearchPath{ TopLevelOnly = path.TopLevelOnly, Folder = path.SearchPath});
            }
            _searchQuery.UseGitIgnore = selectedFirst.UseGitIgnore;
        }

        _searchQuery.ReplaceLiteralTextQuery = string.Empty;
        _searchQuery.ReplaceRegexTextQuery = string.Empty;
        switch (Configuration.Instance.ReplaceMode)
        {
            case "Word":
                break;
            case "Literal":
                _searchQuery.ReplaceLiteralTextQuery = _searchQuery.ReplaceTextQuery;
                break;
            case "Regular Expression":
                _searchQuery.ReplaceRegexTextQuery = _searchQuery.ReplaceTextQuery;
                break;
        }
        
        
        _searchQuery.FilePaths = filePaths;
    }
    

    private const string LastLiveQuery = "LastLiveQuery.Blitz";
    private readonly object _saveLoadSync = new();
    private void SaveQuery()
    {
        lock (_saveLoadSync)
        {
            string storeFile = Blitz.Configuration.Instance.GetStoreFile(LastLiveQuery);
            SearchQuery.SaveFile(storeFile,_searchQuery);
        }
    }

    private void LoadQuery()
    {
        lock (_saveLoadSync)
        {
            var storeFile = Configuration.Instance.GetStoreFile(LastLiveQuery);
            if (File.Exists(storeFile))
            {
                try
                {
                    _searchQuery = SearchQuery.LoadFile(storeFile);
                    _searchQuery.ProcessIdentity = Environment.ProcessId;
                }
                catch (Exception e)
                {   
                    Console.WriteLine(e);
                    SetDefaultQuery();
                }

                _searchQuery.ProcessIdentity = Environment.ProcessId; 
            }
            else
            {
                SetDefaultQuery();
            }
        }
    }

    private void SetDefaultQuery()
    {
        _searchQuery = new SearchQuery(string.Empty, [], [], useGitIgnore:true)
        {
            ProcessIdentity = Environment.ProcessId
        };

        var selected = SelectedScope as ScopeViewModel;
        if (selected == null)
        {
            SelectedScope = selected = new ScopeViewModel(this, new ScopeConfig(){ ScopeTitle = "Default Scope"});
        }
        selected?.RunAddSearchCommand();
    }

    private SearchQuery _searchQuery;

    private ObservableCollection<object> _resultBoxItems;
    
    private SingleTask? _saveQueryTask;
    private ObservableCollection<object> _selectedItems;
    private FileDiscoveryStatusViewModel _fileSearchStatus;
    private bool _isMissingScopeRequirements;
    private TextMate.Installation? _textMateInstallation;
    private IBrush? _textForeground;
    private string? _whatsInUpdate;
    private string _newVersionString = "0.0.0.0";
    private bool _searchResultsHitTestEnabled = true;


    
    public string SearchTextWaterMark => "Search for words seperated by space..";

    public string SearchTextBox
    {
        get => _searchQuery.TextBoxQuery;
        set
        {
            _searchQuery.TextBoxQuery = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(IsSmartCaseSensitive));
            this.RaisePropertyChanged(nameof(IsSearchTermValid));
        }
    }


    private bool _enableGotoPane;
    private bool _enableHelpPane = true;
    private bool _enableSettingsPane;
    private bool _enableTextPane;
    private bool _enableScopePane;
    private bool _enableThemePane;
    private object? _selectedScope;
    private ScopeViewModel _workingScope;
    private ReplaceModeViewModel _selectedReplaceMode;


    public void CaseSmartCaseNotify()
    {
        this.RaisePropertyChanged(nameof(IsSmartCaseSensitive));
        this.RaisePropertyChanged(nameof(IsSmartLiteralCaseSensitive));
        this.RaisePropertyChanged(nameof(IsSmartReplaceCaseSensitive));
    }


    public bool IsSmartCaseSensitive
    {
        get
        {
            return !SearchTextBox.ToLower().Equals(SearchTextBox) && !SearchTextBox.Contains("^");
        }
    }

    public bool IsSmartReplaceCaseSensitive
    {
        get
        {
            if (!ReplaceInFileEnabled)
            {
                return false;
            }

            if (IsReplaceCaseSensitive)
            {
                return false;
            }

            if (string.IsNullOrEmpty(ReplaceBoxText)) return false;
            return !ReplaceBoxText.ToLower().Equals(ReplaceBoxText)  && !ReplaceBoxText.Contains("^");
        }
    }


    public bool IsLiteralCaseSensitive
    {
        get => Configuration.Instance.IsLiteralCaseSensitive;
        set
        {
            if (Configuration.Instance.IsLiteralCaseSensitive == value)
            {
                return;
            }
            Configuration.Instance.IsLiteralCaseSensitive = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(IsSmartLiteralCaseSensitive));
        }
    }



    public bool IsReplaceCaseSensitive
    {
        get => Configuration.Instance.IsReplaceCaseSensitive;
        set
        {
            if (Configuration.Instance.IsReplaceCaseSensitive == value)
            {
                return;
            }
            Configuration.Instance.IsReplaceCaseSensitive = value;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(IsSmartReplaceCaseSensitive));
        }
    }

    public bool IsRegexCaseSensitive
    {
        get => Configuration.Instance.IsRegexCaseSensitive;
        set
        {
            if (Configuration.Instance.IsRegexCaseSensitive == value)
            {
                return;
            }
            Configuration.Instance.IsRegexCaseSensitive = value;
            this.RaisePropertyChanged();
        }
    }

    public bool IsSmartLiteralCaseSensitive
    {
        get
        {
            if (!LiteralSearchEnabled || IsLiteralCaseSensitive)
            {
                return false;
            }
            if (string.IsNullOrEmpty(LiteralSearchTextBox)) return false;
            return !LiteralSearchTextBox.ToLower().Equals(LiteralSearchTextBox);
        }
    }

    public string FileNameSearchTextBox
    {
        get =>  _searchQuery.FileNameQuery;
        set
        {
            bool raise = value != _searchQuery.FileNameQuery;
            _searchQuery.FileNameQuery = value;
            if (raise)
            {
                this.RaisePropertyChanged();
            }
        }
    }

    public string LiteralSearchTextBox
    {
        get =>  _searchQuery.LiteralSearchQuery;
        set
        {
            bool raise = value != _searchQuery.LiteralSearchQuery;
            _searchQuery.LiteralSearchQuery = value;
            if (raise)
            {
                if (string.IsNullOrEmpty(SearchTextBox))
                {
                    _searchQuery.TextBoxQuery = value.Replace("@", "").Replace("!", "").Replace("|", " ");
                }
                
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(SearchTextWaterMark));
                CaseSmartCaseNotify();
            }
        }
    }

    public string RegexSearchTextBox
    {
        get =>  _searchQuery.RegexSearchQuery;
        set
        {
            bool raise = value != _searchQuery.RegexSearchQuery;
            _searchQuery.RegexSearchQuery = value;
            if (raise)
            {
                this.RaisePropertyChanged();
            }
        }
    }


    public string ReplaceBoxText
    {
        get =>  _searchQuery.ReplaceTextQuery;
        set
        {
            bool raise = value != _searchQuery.ReplaceTextQuery;
            _searchQuery.ReplaceTextQuery = value;
            if (raise)
            {
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(IsSmartReplaceCaseSensitive));
            }
        }
    }

    public string ReplaceWithBoxText
    {
        get =>  _searchQuery.ReplaceTextWithQuery;
        set
        {
            bool raise = value != _searchQuery.ReplaceTextWithQuery;
            _searchQuery.ReplaceTextWithQuery = value;
            if (raise)
            {
                this.RaisePropertyChanged();
            }
        }
    }
    public string DebugFileNameSearchTextBox
    {
        get => _searchQuery.DebugFileNameQuery ?? string.Empty;
        set
        {
            var raise = value != _searchQuery.DebugFileNameQuery;
            _searchQuery.DebugFileNameQuery = value;
            if (raise)
            {
                this.RaisePropertyChanged();
            }
        }
    }

    public bool VerboseFileSystemException
    {
        get => _searchQuery.VerboseFileSystemException;
        set
        {
            _searchQuery.VerboseFileSystemException = value;
            this.OnPropertyChangedFileSystemRestart(this,
                new PropertyChangedEventArgs(nameof(VerboseFileSystemException)));
        }
    }

    public bool EnableResultsRecycling
    {
        get => _searchQuery.EnableResultsRecycling;
        set
        {
            _searchQuery.EnableResultsRecycling = value;
            this.OnPropertyChangedFileSystemRestart(this,
                new PropertyChangedEventArgs(nameof(EnableResultsRecycling)));
        }
    }
    
    
    public bool EnableRobotFileFilterIgnore
    {
        get => Configuration.Instance.RobotDetectionSettings.BehaviorIgnoreRobotFiles;
        set
        {
            Configuration.Instance.RobotDetectionSettings.BehaviorIgnoreRobotFiles = value;
            this.OnPropertyChangedFileSystemRestart(this,
                new PropertyChangedEventArgs(nameof(EnableRobotFileFilterIgnore)));
        }
    }
    
    
    public bool EnableRobotFileFilterDefer
    {
        get => Configuration.Instance.RobotDetectionSettings.BehaviorDefer;
        set
        {
            Configuration.Instance.RobotDetectionSettings.BehaviorDefer = value;
            this.OnPropertyChangedFileSystemRestart(this,
                new PropertyChangedEventArgs(nameof(EnableRobotFileFilterDefer)));
        }
    }


    
    public bool EnableRobotFileFilterSkipAndReport
    {
        get => Configuration.Instance.RobotDetectionSettings.BehaviorSkipAndReport;
        set
        {
            Configuration.Instance.RobotDetectionSettings.BehaviorSkipAndReport = value;
            this.OnPropertyChangedFileSystemRestart(this,
                new PropertyChangedEventArgs(nameof(EnableRobotFileFilterSkipAndReport)));
        }
    }

    public double RobotFileDetectionSizeMB
    {
        get => Configuration.Instance.RobotDetectionSettings.MaxFileSizeInMB;
        set
        {
            Configuration.Instance.RobotDetectionSettings.MaxFileSizeInMB = value;
                this.OnPropertyChangedFileSystemRestart(this,
                new PropertyChangedEventArgs(nameof(RobotFileDetectionSizeMB)));
        }
    }

    public int RobotContentLineDetection
    {
        get => Configuration.Instance.RobotDetectionSettings.MaxLineSizeChars;
        set
        {
            Configuration.Instance.RobotDetectionSettings.MaxLineSizeChars = value;
            this.OnPropertyChangedFileSystemRestart(this,
                new PropertyChangedEventArgs(nameof(RobotContentLineDetection)));
        }
    }
    
    public ObservableCollection<object> ResultBoxItems
    {
        get => _resultBoxItems;
        set =>this.RaiseAndSetIfChanged(ref _resultBoxItems,value);
    }

    public ObservableCollection<object> SelectedItems
    {
        get => _selectedItems;
        set => this.RaiseAndSetIfChanged(ref _selectedItems, value);
    }

    public ObservableCollection<ScopeViewModel> ScopeViewModels { get; set; } = []; 
    
    public FileDiscoveryStatusViewModel FileSearchStatus
    {
        get => _fileSearchStatus;
        set => this.RaiseAndSetIfChanged(ref _fileSearchStatus,value);
    }

    public bool IsSearchTermValid => _searchQuery.TextBoxQuery.Trim().Length > 0;

    
    public bool EnableSearchIndex
    {
        get => _searchQuery.EnableSearchIndex;
        set
        {
            if (value == _searchQuery.EnableSearchIndex)
            {
                return;
            }

            _searchQuery.EnableSearchIndex = value;
            this.OnPropertyChangedFileSystemRestart(this, new  PropertyChangedEventArgs(nameof(EnableSearchIndex)));
        }
    }

    private void DeselectAllPanes()
    {
        EnableHelpPane = false;
        EnableGotoPane = false;
        EnableScopePane = false;
        EnableSettingsPane =  false;
        EnableTextPane = false;
        EnableThemePane = false;
        
    }

    private void DisablePaneIfNot(ref bool refVal, bool value, [CallerMemberName] string? propertyName = null)
    {
        if (value)
        {
            DeselectAllPanes();
        }
        
        this.RaiseAndSetIfChanged(ref refVal, value, propertyName);
    }

    public bool EnableGotoPane
    {
        get => _enableGotoPane;
        set => this.DisablePaneIfNot(ref _enableGotoPane, value);
    }

    public bool EnableHelpPane
    {
        get => _enableHelpPane;
        set => this.DisablePaneIfNot(ref _enableHelpPane , value);
    }

    public bool EnableSettingsPane
    {
        get => _enableSettingsPane;
        set => this.DisablePaneIfNot(ref _enableSettingsPane , value);
    }
    public bool EnableTextPane
    {
        get => _enableTextPane;
        set => this.DisablePaneIfNot(ref _enableTextPane , value);
    }

    public bool EnableScopePane
    {
        get => _enableScopePane;
        set => this.DisablePaneIfNot(ref _enableScopePane , value);
    }
    
    public bool EnableThemePane
    {
        get => _enableThemePane;
        set => this.DisablePaneIfNot(ref _enableThemePane , value);
    }
    public int SearchThreads 
    {
        get => _searchQuery.SearchThreads;
        set => this.RaiseAndSetIfChanged(ref _searchQuery.SearchThreads,value);
    }

    

    public int SearchThreadsMin => 1;
    public int SearchThreadsMax => Math.Max(Environment.ProcessorCount - 2, 1);

    public string? WhatsInUpdate
    {
        get => _whatsInUpdate;
        set => this.RaiseAndSetIfChanged(ref _whatsInUpdate , value);
    }

    public Action<object>? ShowPreview { get; set; }

    public ScopeViewModel WorkingScope
    {
        get => _workingScope;
        set
        {
            this.RaiseAndSetIfChanged(ref _workingScope, value);
            this.RaisePropertyChanged(nameof(IsBlitzLogoVisibile));
        }
    }

    public bool IsBlitzLogoVisibile =>  WorkingScope is not ScopeViewModel scopeViewModel || !scopeViewModel.ScopeImageVisible;

    public void RefreshBoxItemHighlights()
    {
        foreach (var item in _resultBoxItems)
        {
            if (item is ContentResultViewModel contentResultViewModel)
            {
                contentResultViewModel.RefreshPropertyVisuals();
            }
        }
    }

    public void StopRespondingToCurrentQuery()
    {
        _searchQuery.InstanceIdentity = 0; // This gets reset when Post message is called.
    }

    public void NotifyMainIconVisibility()
    {
        this.RaisePropertyChanged(nameof(IsBlitzLogoVisibile));
    }
}
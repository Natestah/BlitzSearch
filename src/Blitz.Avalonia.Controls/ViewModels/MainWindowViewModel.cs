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
using System.IO;
using System.Linq;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using AvaloniaEdit.TextMate;
using AvaloniaEdit.Utils;
using Blitz.Avalonia.Controls.Views;
using Blitz.Interfacing;
using Blitz.AvaloniaEdit.ViewModels;
using Material.Icons;
using Application = Avalonia.Application;


namespace Blitz.Avalonia.Controls.ViewModels;

public class MainWindowViewModel : ViewModelBase
{

    public SearchQuery SearchQuery => _searchQuery;

    
    public ExternalPluginInteractions ExternalPluginInteractions { get; } 
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

    private readonly Action<MainWindowViewModel> _requestMainWindowActivate;
    private readonly Action<MainWindowViewModel> _selectAndFocusSearch;
    private readonly Action<MainWindowViewModel> _selectAndFocusReplace;

    public void ActivateMainWindow()
    {
        _requestMainWindowActivate(this);
    }

    public void FocusSearch()
    {
        _selectAndFocusSearch(this);
    }
    public void FocusReplace()
    {
        _selectAndFocusReplace(this);
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

    public double GeneralIconSizeAdjustedForComboBox => GeneralIconSize + 30;

    public double GeneralIconSize => Configuration.Instance.EditorConfig.FontSize + 10;

    public bool ShowStatusBar
    {
        get => NewVersionAvailable || Configuration.Instance.ShowStatusBar;
        set
        {
            Configuration.Instance.ShowStatusBar = value;
            this.RaisePropertyChanged();
        }
    }

    public bool ShowScopeImage
    {
        get => Configuration.Instance.ShowScopeImage;
        set
        {
            Configuration.Instance.ShowScopeImage = value;
            this.RaisePropertyChanged();
        }
    }
    public BlitzEditorViewModel EditorViewModel { get; set; } = new BlitzEditorViewModel();

    public IBrush? TextForeground
    {
        get => _textForeground;
        set => this.RaiseAndSetIfChanged(ref _textForeground, value);
    }

    public event EventHandler? SelectedFileChanged; 
    
    public MainWindowViewModel( ISearchingClient searchingClient, 
        Action<MainWindowViewModel> requestMainWindowActivate, 
        Action<MainWindowViewModel> selectAndFocusSearch,
        Action<MainWindowViewModel> selectAndFocusReplace)
    {
        _requestMainWindowActivate = requestMainWindowActivate;
        _selectAndFocusSearch = selectAndFocusSearch;
        _selectAndFocusReplace = selectAndFocusReplace;
        GotoEditorViewModel? firstEditorThatExists = null;
        bool foundConfiguredEditor = false;
        var gotoEditors = new GotoDefinitions().GetBuiltInEditors().ToList();
        
        foreach (var gotoEditor in gotoEditors)
        {
            foundConfiguredEditor = AddEditorToViewModel(true, gotoEditor, foundConfiguredEditor, ref firstEditorThatExists);
        }
        
        foreach (var gotoEditor in Configuration.Instance.CustomEditors)
        {
            foundConfiguredEditor = AddEditorToViewModel( false, gotoEditor, foundConfiguredEditor, ref firstEditorThatExists);
        }
        

        if (!foundConfiguredEditor && firstEditorThatExists != null)
        {
            _selectedEditorViewModel = firstEditorThatExists;
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
        ToggleFileNameFilterCommand = ReactiveCommand.Create(ToggleFileNameDebugFilterCommandRun);
        ToggleLiteralSearchCommand = ReactiveCommand.Create(ToggleLiteralSearchCommandRun);
        ToggleRegexSearchCommand = ReactiveCommand.Create(ToggleRegexSearchCommandRun);
        ToggleFindInFilesFilterCommand = ReactiveCommand.Create(ToggleFindInFilesFilterCommandRun);
        EscapeMinimizeCommand = ReactiveCommand.Create(EscapeMininizeCommandRun);
        
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
        ResultsHighlighting = new ResultsHighlighting(this);
        ExternalPluginInteractions = new ExternalPluginInteractions(this);
    }

    private bool AddEditorToViewModel(bool isReadOnly, GotoEditor gotoEditor, bool foundConfiguredEditor,
        ref GotoEditorViewModel? firstEditorThatExists)
    {
        var editorVm = new GotoEditorViewModel(this,gotoEditor)
        {
            ReadOnly = isReadOnly
        };
        GotoEditorCollection.Add(editorVm);
        if (gotoEditor.Title == Configuration.Instance.GotoEditor.Title)
        {
            foundConfiguredEditor = true;
            _selectedEditorViewModel = editorVm;
        }

        if (firstEditorThatExists==null && editorVm.EditorExists())
        {
            firstEditorThatExists = editorVm;;
        }

        return foundConfiguredEditor;
    }

    private void EscapeMininizeCommandRun()
    {
        GotoMinimizer.Invoke();
    }

    public ResultsHighlighting ResultsHighlighting { get; set; } 

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

    public void SetSelectedThemeModels()
    {
        EditorViewModel.PopulateThemeModels();
        
        EditorViewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(EditorViewModel.ThemeViewModel) && EditorViewModel.ThemeViewModel != null)
            {
                Configuration.Instance.CurrentTheme = EditorViewModel.ThemeViewModel.Theme; 
                Configuration.Instance.SelectedThemePremium = EditorViewModel.ThemeViewModel.Theme.ThemeName;
                Configuration.Instance.SelectedThemeIsDark = EditorViewModel.ThemeViewModel.Theme.AvaloniaThemeVariant == "Dark";
            }
        };
        foreach (var themeViewModel in EditorViewModel.AllThemeViewModels)
        {
            if ( themeViewModel.Theme.ThemeName == Configuration.Instance.SelectedThemePremium)
            {
                EditorViewModel.ThemeViewModel = themeViewModel;
                return;
            }
        }

    }

    public bool SearchResultsHitTestEnabled
    {
        get => _searchResultsHitTestEnabled;
        set => this.RaiseAndSetIfChanged(ref _searchResultsHitTestEnabled,value);
    }

    public WindowState LastNonMinizedState { get; set; } = WindowState.Normal;



    private GotoEditorViewModel? _selectedEditorViewModel;
    private bool _newVersionAvailable;
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
    public ReactiveCommand<Unit,Unit> GotoSelectedExplorer { get; set; }
    public ReactiveCommand<Unit,Unit> GotoSelectedCmd { get; set; }
    
    public ReactiveCommand<Unit,Unit> ToggleTextEditCmd { get; set; }
    public ReactiveCommand<Unit,Unit> ToggleFileNameFilterCommand { get; set; }
    public ReactiveCommand<Unit,Unit> ToggleLiteralSearchCommand { get; set; }
    public ReactiveCommand<Unit,Unit> ToggleRegexSearchCommand { get; set; }
    
    public ReactiveCommand<Unit,Unit> ShowPreviewTextCommand { get; set; }
    public ReactiveCommand<Unit,Unit> HidePreviewPaneCommand { get; set; }
    
    public ReactiveCommand<Unit,Unit> ToggleFindInFilesFilterCommand { get; set; }
    public ReactiveCommand<Unit,Unit> EscapeMinimizeCommand { get; set; }
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
        set
        {
            if (value != null)
            {
                Configuration.Instance.GotoEditor = value.GotoEditor;
            }
            this.RaiseAndSetIfChanged(ref _selectedEditorViewModel!, value);
            UpdateScopeSelectionForEditor();
        }
    }

    void ApplyVisualStudioFromConfiguration()
    {
        var selectedSolutionTitle = Configuration.Instance.SelectedSolutionTitle;
        var existingSolutionViewModel = SolutionViewModels.FirstOrDefault(model => model.Title == selectedSolutionTitle);
        SolutionViewModel = existingSolutionViewModel ?? SolutionViewModels.FirstOrDefault();
        if (!AnyScopeSelected || IsWorkspaceScopeSelected)
        {
            IsSolutionScopeSelected = true;
        }
    }

    public void UpdateScopeSelectionForEditor()
    {
        if (_selectedEditorViewModel == null)
        {
            return;
        }

        IsWorkspaceScopeSelected = false;
        IsSolutionScopeSelected = false;
        SolutionViewModel = null;
        SelectedWorkspaceScopeViewModel = null;
        switch (_selectedEditorViewModel.GotoEditor.CodeExecute)
        {
            case CodeExecuteNames.VSCode:
            case CodeExecuteNames.Cursor:
            case CodeExecuteNames.Windsurf:
                RecallVisualStudioCodeWorkspacesVisited();
                break;
            case CodeExecuteNames.SublimeText:
                //Sublime Text always updates a single summary of it's Windows and workspaces.
                ExternalPluginInteractions.Commander.ExecuteNamedAction(PluginCommands.SublimeTextWorkspaceUpdate);
                break;
            case CodeExecuteNames.VisualStudio:
                ApplyVisualStudioFromConfiguration();
                break;
            default:
                break;
        }
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
            foreach (var result in this.ResultBoxItems.OfType<ContentResultViewModel>())
            {
                result.RefreshPropertyVisuals();
            }
        }
    }

    public bool ResultsFileNameScopeTrim
    {
        get => Configuration.Instance.EditorConfig.ResultsFileNameScopeTrim;
        set
        {
            Configuration.Instance.EditorConfig.ResultsFileNameScopeTrim = value;
            this.RaisePropertyChanged();
            foreach (var result in this.ResultBoxItems.OfType<FileNameResultViewModel>())
            {
                result.UpdateFileName();
            }
        }
    }

    public bool ShowDonationButton
    {
        get => Configuration.Instance.EditorConfig.ShowDonationButton;
        set
        {
            Configuration.Instance.EditorConfig.ShowDonationButton = value;
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
            this.RaisePropertyChanged(nameof(GeneralIconSize));
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

    public bool FileNameDebugQueryEnabled
    {
        get => _searchQuery.FileNameDebugQueryEnabled;
        set
        {
            _searchQuery.FileNameDebugQueryEnabled = value;
            this.RaisePropertyChanged(nameof(FileNameDebugQueryEnabled));
        }
    }

    public string LiteralSearchHeader => "Literal Search";
    public string RegexMatchHeader => "RegExp Search";
    
    public bool FileNameInResultsEnabled
    {
        get => _searchQuery.FileNameInResultsInResultsEnabled;
        set
        {
            _searchQuery.FileNameInResultsInResultsEnabled = value;
            this.RaisePropertyChanged();
        }
    }
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
                    var trimStart = chosenQuery.Trim("^@|".ToCharArray());
                    if (!string.IsNullOrEmpty(trimStart))
                    {
                        ReplaceBoxText = chosenQuery;
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
                new ReplaceModeViewModel(this, MaterialIconKind.FileWordBox, BlitzSingleWordQuery, "Single word, supports '@' (whole word) and '|' (OR words)"),
                new ReplaceModeViewModel(this, MaterialIconKind.TextBoxSearch, "Literal", "Literal search, everything typed gets replaced"),
                new ReplaceModeViewModel(this, MaterialIconKind.RegularExpression, "Regular Expression", "Traditional Regular Expression")
            ];
        }
    }

    private const string BlitzSingleWordQuery = "Blitz Word";

    private const int MaxHistoryEntries = 15;

    public void UpdateHistoryForColllection(ObservableCollection<string> history, string text)
    {
        var first = history.FirstOrDefault();
        if (text == first || string.IsNullOrEmpty(text)) return;
        history.Remove(text);
        history.Insert(0, text);
        while (history.Count > MaxHistoryEntries)
        {
            history.RemoveAt(history.Count - 1);
        }
    }

    public string ReplaceModeHint => SelectedReplaceMode?.Hint ?? "No Replace Models..";

    public ReplaceModeViewModel? SelectedReplaceMode
    {
        get
        {
            _selectedReplaceMode ??= ReplaceModeViewModels.FirstOrDefault(a => a.Title == Configuration.Instance.ReplaceMode);
            return _selectedReplaceMode ??= ReplaceModeViewModels?.FirstOrDefault();
        }
        set
        {
            if (value != null)
            {
                Configuration.Instance.ReplaceMode = value.Title;
            }
            this.RaiseAndSetIfChanged(ref _selectedReplaceMode, value);
            this.RaisePropertyChanged(nameof(IsReplaceCaseSensitivityVisible));
        }
    }

    public bool IsReplaceCaseSensitivityVisible
    {
        get
        {
            if(SelectedReplaceMode == null) return false;
            return SelectedReplaceMode.Title != BlitzSingleWordQuery;

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

    public void UpdateTheme()
    {
        RefreshBoxItemHighlights();
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
        EnableTextPane = !EnableTextPane;
        if (EnableTextPane)
        {
            ShowPaneIfSelection();
        }
    }


    private void GotoSelectedExternalRun()
    {
        if (SelectedEditorViewModel != null && !SelectedEditorViewModel.RunTotoOnObjectGoto(SelectedItems.FirstOrDefault(),false, out var errorMessage))
        {
        }
    }

    public void RaiseSolutionPropertyChanged()
    {
        this.RaisePropertyChanged(nameof(SolutionViewModel));
    }



    private void ToggleFindInFilesFilterCommandRun()
    {
        ReplaceInFileEnabled = !ReplaceInFileEnabled;
    }

    private void ToggleFileNameDebugFilterCommandRun()
    {
        FileNameDebugQueryEnabled = !FileNameDebugQueryEnabled;
    }
    
    
    private void ToggleLiteralSearchCommandRun()
    {
        LiteralSearchEnabled = !LiteralSearchEnabled;
    }
    
    private void ToggleRegexSearchCommandRun()
    {
        RegexSearchEnabled = !RegexSearchEnabled;
    }

    public void ShowPaneIfSelection()
    {
        if (SelectedItems.FirstOrDefault() is { } firstOrDefault)
        {
            UpdatePreviewForItem(firstOrDefault);
        }
    }
    
    public bool IsProjectScopeSelected
    {
        get => Configuration.Instance.IsProjectScopeSelected;
        set
        {
            if (value)
            {
                RadioClearConfig();
            }
            Configuration.Instance.IsProjectScopeSelected = value;
            this.OnPropertyChangedFileSystemRestart(this, new PropertyChangedEventArgs(nameof(IsProjectScopeSelected)));
        }
    }

    public void RadioClearConfig()
    {
        //Since this is setup as a listbox with items, the results of the next selection has events firing before the previous one.
        Configuration.Instance.IsFoldersScopeSelected = false;
        Configuration.Instance.IsProjectScopeSelected = false;
        Configuration.Instance.IsSolutionScopeSelected = false;
        Configuration.Instance.IsWorkspaceScopeSelected = false;
        Configuration.Instance.IsOpenScopeSelected = false;
        Configuration.Instance.IsActiveFileSelected = false;
    }
    
    public bool AnyScopeSelected =>        
        Configuration.Instance.IsFoldersScopeSelected ||
        Configuration.Instance.IsProjectScopeSelected  ||
        Configuration.Instance.IsSolutionScopeSelected  ||
        Configuration.Instance.IsWorkspaceScopeSelected  ||
        Configuration.Instance.IsOpenScopeSelected  ||
        Configuration.Instance.IsActiveFileSelected;


    public void RadioNotifyAndUpdateTitle()
    {
        this.RaisePropertyChanged(nameof(IsProjectScopeSelected));
        this.RaisePropertyChanged(nameof(IsProjectScopeSelected));
        this.RaisePropertyChanged(nameof(IsSolutionScopeSelected));
        this.RaisePropertyChanged(nameof(IsWorkspaceScopeSelected));
        this.RaisePropertyChanged(nameof(IsOpenScopeSelected));
        this.RaisePropertyChanged(nameof(IsActiveFileSelected));
    }
    
    
    
    
    public bool IsSolutionScopeSelected
    {
        get => Configuration.Instance.IsSolutionScopeSelected;
        set
        {
            if (value)
            {
                RadioClearConfig();
            }
            Configuration.Instance.IsSolutionScopeSelected = value;
            this.OnPropertyChangedFileSystemRestart(this, new PropertyChangedEventArgs(nameof(IsSolutionScopeSelected)));
            RadioNotifyAndUpdateTitle();
        }
    }
    public bool IsWorkspaceScopeSelected
    {
        get => Configuration.Instance.IsWorkspaceScopeSelected;
        set
        {
            if (value)
            {
                RadioClearConfig();
            }
            Configuration.Instance.IsWorkspaceScopeSelected = value;
            this.OnPropertyChangedFileSystemRestart(this, new PropertyChangedEventArgs(nameof(IsWorkspaceScopeSelected)));
            RadioNotifyAndUpdateTitle();

        }
    }

    
    public bool IsFoldersScopeSelected
    {
        get => Configuration.Instance.IsFoldersScopeSelected;
        set
        {
            if (value)
            {
                RadioClearConfig();                
            }
            Configuration.Instance.IsFoldersScopeSelected = value;
            this.OnPropertyChangedFileSystemRestart(this, new PropertyChangedEventArgs(nameof(IsFoldersScopeSelected)));
            RadioNotifyAndUpdateTitle();

        }
    }

    public bool IsOpenScopeSelected
    {
        get => Configuration.Instance.IsOpenScopeSelected;
        set
        {
            if (value)
            {
                RadioClearConfig();
            }
            Configuration.Instance.IsOpenScopeSelected = value;
            this.OnPropertyChangedFileSystemRestart(this, new PropertyChangedEventArgs(nameof(IsOpenScopeSelected)));
            RadioNotifyAndUpdateTitle();

        }
    }


    public bool IsActiveFileSelected
    {
        get => Configuration.Instance.IsActiveFileSelected;
        set
        {
            if (value)
            {
                RadioClearConfig();
            }
            Configuration.Instance.IsActiveFileSelected = value;
            OnPropertyChangedFileSystemRestart(this, new PropertyChangedEventArgs(nameof(IsActiveFileSelected)));
            RadioNotifyAndUpdateTitle();

        }
    }


    private SolutionViewModel? _solutionViewModel;

    public SolutionViewModel? SolutionViewModel
    {
        get => _solutionViewModel;
        set
        {
            if (value != null)
            {
                Configuration.Instance.SelectedSolutionTitle = value.Title;
                value.RestoreSelectionFromConfiguration();
                value.RestoreActiveFilesFromVisualStudio();

            }
            this.RaiseAndSetIfChanged(ref _solutionViewModel, value);
            this.RaisePropertyChanged(nameof(IsSolutionStyle));
            this.RaisePropertyChanged(nameof(IsSolutionScopeSelected));
        }
    }

    public ObservableCollection<SolutionViewModel> SolutionViewModels { get; } = [];

    public bool IsSolutionStyle => _solutionViewModel != null;
    public bool IsWorkspaceStyle => _selectedWorkspaceScopeViewModel != null;

    
    public ObservableCollection<WorkspaceScopeViewModel> WorkspaceScopeViewModels { get; } = [];
    private WorkspaceScopeViewModel? _selectedWorkspaceScopeViewModel;

    public WorkspaceScopeViewModel? SelectedWorkspaceScopeViewModel
    {
        get => _selectedWorkspaceScopeViewModel;
        set
        {
            if (value != null && SelectedEditorViewModel != null)
            {
                Configuration.Instance.EditorWorkSpaceTitleSelection[SelectedEditorViewModel.Title] = value.Title;
            }
            this.RaiseAndSetIfChanged(ref _selectedWorkspaceScopeViewModel, value);
            this.RaisePropertyChanged(nameof(IsWorkspaceStyle));
            OnPropertyChangedFileSystemRestart(this,
                new PropertyChangedEventArgs(nameof(SelectedWorkspaceScopeViewModel)));
        }
    }

    public ScopeViewModel? SelectedScope 
    {
        get => _selectedScope ?? new ScopeViewModel(this, new ScopeConfig(){ ScopeTitle = "Hi"});
        set
        {
            if (value == null)
            {
                return;
            }

            _selectedScope = value;
            if (value is { } scopeViewModel )
            {
                Configuration.Instance.SelectedScope = scopeViewModel.ScopeTitle;
                WorkingScope = scopeViewModel;
            }
            this.RaisePropertyChanged(nameof(IsSolutionStyle));
            OnPropertyChangedFileSystemRestart(this,
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

        if (searchTaskResult.ServerResultsResetClear)
        {
            _resultBoxItems.Clear();
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
                    if (ResultBoxItems[i] is ContentResultViewModel contenxtResultsViewModel && contenxtResultsViewModel.FileNameResult.FileName == changedFile.FileName )
                    {
                        foundThis = true;
                        while (i < ResultBoxItems.Count && ResultBoxItems[i] is ContentResultViewModel nextResultViewModel && nextResultViewModel.FileNameResult.FileName == changedFile.FileName )
                        {
                            ResultBoxItems.RemoveAt(i);
                        }

                        bool first = true;
                        foreach (var content in changedFile.ContentResults)
                        {
                            ResultBoxItems.Insert(i,new ContentResultViewModel(this,content,changedFile){IsUpdated = true, IsFirstFromFile = first});
                            first = false;
                            i++;
                        }
                    }
                }

                bool isMatchInFilename = changedFile.BlitzMatches != null && changedFile.BlitzMatches.Count > 0;
            
                if (!foundThis && (isMatchInFilename || changedFile.ContentResults.Count > 0) )
                {
                    if (isMatchInFilename)
                    {
                        var fileItem = new FileNameResultViewModel(this, changedFile) { IsUpdated = true };
                        ResultBoxItems.Add(fileItem);
                    }
                    bool isFirst = true;
                    foreach (var item in changedFile.ContentResults)
                    {
                        var contextItem = new ContentResultViewModel(this, item, changedFile);
                        if (isFirst)
                        {
                            contextItem.IsFirstFromFile = true;
                            isFirst = false;
                        }
                        ResultBoxItems.Add(contextItem);
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
            bool isMatchInFilename = fileResult.BlitzMatches.Count > 0;
            if ( isMatchInFilename )
            {
                ResultBoxItems.Add(new FileNameResultViewModel(this,fileResult));
                continue;
            }

            bool isFirst = true;
            foreach (var contentResult in fileResult.ContentResults)
            {
                var thisItem = new ContentResultViewModel(this, contentResult, fileResult);
                if (isFirst)
                {
                    thisItem.IsFirstFromFile = true;
                    isFirst = false;
                }
                ResultBoxItems.Add(thisItem);
            }
        }
        
        if (SelectedItems.Count == 0 && EnableTextPane)
        {
            var first = ResultBoxItems.OfType<ContentResultViewModel>().FirstOrDefault()
                        ?? ResultBoxItems.FirstOrDefault();

            if (first != null)
            {
                SelectedItems.Add(first);
                this.RaisePropertyChanged(nameof(SelectedItems));
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
            or nameof(CacheStatus) 
            or nameof(SelectedFontFamily)
            or nameof(ResultsFileNameScopeTrim)
            or nameof(FontSize)
            or nameof(GeneralIconSize)
            or nameof(LineSpacing)
            or nameof(EnableGotoPane)
            or nameof(EnableHelpPane)
            or nameof(EnableSettingsPane)
            or nameof(EnableTextPane)
            or nameof(EnableScopePane)
            or nameof(EnableThemePane)
            or nameof(SelectedItems)
            or nameof(IsSmartCaseSensitive)
            )
        {
            return;
        }
        
        if(e.PropertyName != null && _suppressedFileSystemPropertiesForever.Contains(e.PropertyName))
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

    //Blitz Responds to All the properties
    private HashSet<string> _suppressedFileSystemPropertiesForever = [];
    private void OnPropertyChangedFileSystemRestart(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != null)
        {
            _suppressedFileSystemPropertiesForever.Add(e.PropertyName);
        }
        this.RaisePropertyChanged(e.PropertyName);
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
                _searchQuery.RawExtensionList = selectedFirst.ExtensionText;
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
            _searchQuery.UseBlitzIgnore = selectedFirst.UseBlitzIgnore;
            _searchQuery.UseGlobalIgnore = selectedFirst.UseGlobalGitIgnore;
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

        _searchQuery.FilePaths = [];
        _searchQuery.FlatSearchFilesList = null;
        if (IsFoldersScopeSelected)
        {
            _searchQuery.SelectedProjectName = null;
            _searchQuery.SolutionExports = null;
            _searchQuery.FilePaths = filePaths;
        }
        else if (IsWorkspaceScopeSelected)
        {
            _searchQuery.SelectedProjectName = null;
            _searchQuery.SolutionExports = null;
            _searchQuery.FilePaths = [];
            if (SelectedWorkspaceScopeViewModel?.WorkspaceExport == null)
            {
                return;
            }
            foreach (var folder in SelectedWorkspaceScopeViewModel.WorkspaceExport.Folders)
            {
                _searchQuery.FilePaths.Add(new SearchPath { TopLevelOnly = false, Folder = folder });
            }
        }
        else if (IsProjectScopeSelected)
        {
            var isolatedSolutionExport = new SolutionExport();
            if (SolutionViewModel == null)
            {
                return;
            }

            var currentExport = SolutionViewModel.Export;
            if (currentExport != null)
            {
                isolatedSolutionExport.Name = currentExport.Name;
                isolatedSolutionExport.Projects = [];
                if (SolutionViewModel.SelectedProject != null)
                {
                    var match = currentExport.Projects.FirstOrDefault(x =>
                        x.Name == SolutionViewModel.SelectedProject.Name);
                    if (match != null)
                    {
                        isolatedSolutionExport.Projects.Add(match);
                    }
                }
            }
            _searchQuery.SolutionExports = [isolatedSolutionExport];
        }
        else if (IsSolutionScopeSelected)
        {
            if (SolutionViewModel == null)
            {
                return;
            }

            if (SolutionViewModel.Export != null)
            {
                _searchQuery.SolutionExports = [SolutionViewModel.Export];
                _searchQuery.SelectedSolutionExports = SolutionViewModel.Export.Name;
            }
            else
            {
                _searchQuery.SolutionExports = [];
            }
        }
        else if (IsOpenScopeSelected)
        {
            _searchQuery.SelectedProjectName = null;
            _searchQuery.SolutionExports = null;
            if (SolutionViewModel == null)
            {
                return;
            }
            _searchQuery.FlatSearchFilesList = SolutionViewModel.ActiveFiles.ToList();
        }
        else if (IsActiveFileSelected)
        {
            _searchQuery.SelectedProjectName = null;
            _searchQuery.SolutionExports = null;
            if (SolutionViewModel is { ActiveFiles.Count: > 0 })
            {
                _searchQuery.FlatSearchFilesList = [SolutionViewModel.ActiveFiles[0]];
            }
        }
        else
        {
            _searchQuery.SolutionExports = null;
            _searchQuery.FilePaths = filePaths;
        }
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
    private IBrush? _textForeground;
    private string? _whatsInUpdate;
    private string _newVersionString = "0.0.0.0";
    private bool _searchResultsHitTestEnabled = true;


    
    public string SearchTextWaterMark => "Search for words separated by space..";

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
    private ScopeViewModel? _selectedScope;
    private ScopeViewModel? _workingScope;
    private ReplaceModeViewModel? _selectedReplaceMode;


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
        get =>  _searchQuery.FileNameQuery ?? string.Empty;
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
        get =>  _searchQuery.LiteralSearchQuery ?? string.Empty;
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
        get =>  _searchQuery.RegexSearchQuery ?? string.Empty;
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
        get =>  _searchQuery.ReplaceTextQuery ?? string.Empty;
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
        get =>  _searchQuery.ReplaceTextWithQuery ?? string.Empty;
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
        set => this.RaiseAndSetIfChanged(ref _enableTextPane, value);
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

    public ScopeViewModel? WorkingScope
    {
        get => _workingScope;
        set
        {
            this.RaiseAndSetIfChanged(ref _workingScope, value);
            this.RaisePropertyChanged(nameof(IsBlitzLogoVisibile));
        }
    }

    public bool IsBlitzLogoVisibile =>  WorkingScope is not ScopeViewModel scopeViewModel || !scopeViewModel.ScopeImageVisible;
    public Action GotoMinimizer { get; set; }
    public BoolDelegate PreviewOnlyWhenFocused { get; set; }

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
    public delegate bool BoolDelegate();

    public void StopRespondingToCurrentQuery()
    {
        _searchQuery.InstanceIdentity = 0; // This gets reset when Post message is called.
    }

    public void NotifyMainIconVisibility()
    {
        this.RaisePropertyChanged(nameof(IsBlitzLogoVisibile));
    }

    public void RebuildCustomEditorList()
    {
        Configuration.Instance.CustomEditors.Clear();
        foreach (var gotoEditorViewModel in GotoEditorCollection)
        {
            if (!gotoEditorViewModel.ReadOnly)
            {
                Configuration.Instance.CustomEditors.Add(gotoEditorViewModel.GotoEditor);
            }
        }
    }

    public void UpdatePreviewForItem(object eAddedItem)
    {
        if (EnableTextPane == true) 
        {
            //update preview on internal text pane.
            ShowPreview?.Invoke(eAddedItem);
        }
        else if (SelectedEditorViewModel != null && !SelectedEditorViewModel.RunTotoOnObjectGoto(eAddedItem,true,
                     out string errorMessage))
        {
            ShowImportantMessage?.Invoke(errorMessage);
        }
    }

    public void UpdateActiveFiles(ActiveFilesList? list)
    {
        if (list is null || SolutionViewModel == null
                         ||SolutionViewModel.ActiveFiles.SequenceEqual(list.ActiveFiles))
        {
            return;
        }
            
        SolutionViewModel.ActiveFiles.Clear();
        SolutionViewModel.ActiveFiles.AddRange(list.ActiveFiles);
        if (IsActiveFileSelected)
        {
            OnPropertyChangedFileSystemRestart(this, new PropertyChangedEventArgs(nameof(IsActiveFileSelected)));
        }
        
    }

    public void RecallVisualStudioCodeWorkspacesVisited()
    {
        if (SelectedEditorViewModel is not { IsVsCode: true } and not { IsCursor: true } and not
            { IsWindsurf: true })
        {
            return;
        }

        WorkspaceScopeViewModels.Clear();
        int max = 20;
        int count = 0;
        foreach (var solutionId in ExternalPluginInteractions.Commander.GetSolutionIDsForCommands(PluginCommands.VisualStudioCodeWorkspaceUpdate))
        {
            var workspaceScopeViewModel = new WorkspaceScopeViewModel(this, solutionId);
            WorkspaceScopeViewModels.Add(workspaceScopeViewModel);
            count++;
            if (count > max)
                break;
        }

        if (Configuration.Instance.EditorWorkSpaceTitleSelection.TryGetValue(SelectedEditorViewModel.Title,
                out var configTitle))
        {
            SelectedWorkspaceScopeViewModel = WorkspaceScopeViewModels.FirstOrDefault(vm=>vm.Title == configTitle);
        }
        
        SelectedWorkspaceScopeViewModel ??= WorkspaceScopeViewModels.FirstOrDefault();

        SolutionViewModel = null;

        if (!IsFoldersScopeSelected)
        {
            IsWorkspaceScopeSelected = true;
        }
    }

    private string GetNameFromSublimeTextWorkspace(FolderWorkspace workspace)
    {
        if (!string.IsNullOrEmpty(workspace.ProjectName))
        {
            try
            {
                return Path.GetFileNameWithoutExtension(workspace.ProjectName);
            }
            catch (Exception)
            {
                return workspace.ProjectName;
            }
        }

        if (!string.IsNullOrEmpty(workspace.WorkspaceFileName))
        {
            try
            {
                return Path.GetFileNameWithoutExtension(workspace.WorkspaceFileName);
            }
            catch (Exception)
            {
                return workspace.WorkspaceFileName;
            }
        }

        StringBuilder builder = new();
        builder.Append('(');
        bool continuation = false;
        foreach (var folder in workspace.Folders)
        {
            try
            {
                if (continuation)
                {
                    builder.Append(',');
                }

                builder.Append(System.IO.Path.GetFileName(folder));
                continuation = true;
            }
            catch (Exception)
            {
                builder.Append('-');
                throw;
            }
        }

        builder.Append(')');
        return builder.ToString();
    }

    public void ApplySublimeTextListOfWorkspaces(List<FolderWorkspace> workspaces)
    {
        SolutionViewModel = null;


        var sublimeTextViewModels = new HashSet<WorkspaceScopeViewModel>();
        
        WorkspaceScopeViewModels.Clear();
        
        foreach (var folderWorkspace in workspaces)
        {
            if (string.IsNullOrEmpty(folderWorkspace.Name))
            {
                folderWorkspace.Name = GetNameFromSublimeTextWorkspace(folderWorkspace);
            }

            var solutionId = SolutionID.CreateFromSolutionPath(folderWorkspace.Name);
            var workspaceScopeViewModel = new WorkspaceScopeViewModel(this, solutionId,true)
            {
                WorkspaceExport = folderWorkspace
            };
            WorkspaceScopeViewModels.Insert(0, workspaceScopeViewModel);
            sublimeTextViewModels.Add(workspaceScopeViewModel);
        }

        if (SelectedEditorViewModel is not null)
        {
            if (Configuration.Instance.EditorWorkSpaceTitleSelection.TryGetValue(SelectedEditorViewModel.Title,
                    out var configTitle))
            {
                SelectedWorkspaceScopeViewModel =
                    WorkspaceScopeViewModels.FirstOrDefault(vm => vm.Title == configTitle);
            }
        }

        SelectedWorkspaceScopeViewModel ??= WorkspaceScopeViewModels.FirstOrDefault();

        if (!IsFoldersScopeSelected)
        {
            IsWorkspaceScopeSelected = true;
        }
    }
    
    public async Task ApplyReplacement(FileNameResult item)
    {
        var text = await File.ReadAllTextAsync(item.FileName);
        text = item.GetReplaceResults(text);
        await File.WriteAllTextAsync(item.FileName, text);
    }
    
    
        
    public string GetRelativePathForFileName(string filenName)
    {
        // enumerate the selected WorkSpace/Project/Solutions to discover what the project is for incoming result.
        if (SelectedScope != null)
        {
            foreach (var scopeViewModel in SelectedScope.SearchPathViewModels)
            {
                if (filenName.StartsWith(scopeViewModel.SearchPath, StringComparison.OrdinalIgnoreCase))
                {
                    return scopeViewModel.SearchPath;
                }
            }
        }

        if (SolutionViewModel is not null)
        {
            SolutionViewModel.SolutionIdentity.SolutionPath = filenName;
            if (filenName.StartsWith(SolutionViewModel.Title, StringComparison.OrdinalIgnoreCase))
            {
                return SolutionViewModel.SolutionIdentity.SolutionPath;
            }
        }

        return "";
    }

    public void ShowPreferences()
    {
        //Todo:
        // Create show preference delegate in mainpanel
        // start the window
        throw new NotImplementedException();
        
        
    }
}
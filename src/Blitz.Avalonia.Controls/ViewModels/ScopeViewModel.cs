using System;
using System.Collections.Generic;
using System.ComponentModel;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Text;
using Avalonia.Media.Imaging;
using Blitz.Interfacing;


namespace Blitz.Avalonia.Controls.ViewModels;

public class ScopeViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindowViewModel;

    private readonly ScopeConfig _scopeConfig;
    private bool _scopeImageVisible;
    public ReactiveCommand<Unit, Unit> AddSearchPath { get; }
    public ObservableCollection<SearchPathViewModel> SearchPathViewModels { get; set; } = [];

    public string FoldersList
    {
        get
        {
            if (SearchPathViewModels.Count == 0)
            {
                return string.Empty;
            }
            
            var builder = new StringBuilder();
            builder.Append(SearchPathViewModels[0].ConfigSearchPath.Folder);
            if (SearchPathViewModels.Count <= 1)
            {
                return builder.ToString();
            }
            var diff = SearchPathViewModels.Count - 1;
            builder.AppendLine();
            builder.Append($"+ {diff} folders..");
            return builder.ToString();
            
        }
    }
    
    public MainWindowViewModel MainWindowViewModel => _mainWindowViewModel;
    
    public ScopeViewModel(MainWindowViewModel mainWindowViewModel,  ScopeConfig scopeConfig)
    {
        SearchPathViewModels.CollectionChanged += (sender, args) =>
        {
            this.RaisePropertyChanged(nameof(FoldersList));
            this.RaisePropertyChanged(nameof(FirstNameSummary));
        };
        _mainWindowViewModel = mainWindowViewModel;
        _scopeConfig = scopeConfig;
        AddSearchPath = ReactiveCommand.Create(RunAddSearchCommand);
        ReBuildFilePathCollectionViewModels();
        ScopeImage = scopeConfig.ScopeImage;
        if (!string.IsNullOrEmpty(ScopeImage))
        {
            try
            {
                ScopeImageVisible = File.Exists(ScopeImage);
            }
            catch (Exception)
            {
                // ignored
            }
        }
        ValidateExtensionText();
    }

    public string FirstNameSummary
    {
        get
        {
            try
            {
                if (SearchPathViewModels.Count == 0)
                {
                    return string.Empty;
                }
                if (SearchPathViewModels.Count == 1)
                {
                    return "\\"+ Path.GetFileName(SearchPathViewModels[0].ConfigSearchPath.Folder);
                }
                var diff = SearchPathViewModels.Count - 1;
                return "\\"+ Path.GetFileName(SearchPathViewModels[0].ConfigSearchPath.Folder) + $"+{diff}";
            }
            catch (Exception)
            {
                return "Error";
            }
        }
    }
    
    public void RemoveMe()
    {
        Configuration.Instance.ScopeConfigs.Remove(_scopeConfig);
    }

    public MainWindowViewModel MainWindowVM => _mainWindowViewModel;

    public string ScopeTitle
    {
        get => _scopeConfig.ScopeTitle;
        set
        {
            _scopeConfig.ScopeTitle = value;
            this.RaisePropertyChanged();
        }
    }

    public bool ScopeImageVisible
    {
        get => _scopeImageVisible;
        set => this.RaiseAndSetIfChanged(ref _scopeImageVisible, value);
    }

    public Bitmap? ScopeBitmap
    {
        get
        {
            try
            {
                return new Bitmap(ScopeImage);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public string ScopeImage
    {
        get => _scopeConfig.ScopeImage;
        set
        {
            _scopeConfig.ScopeImage = value;
            try
            {
                ScopeImageVisible = File.Exists(value);
                if (ScopeImageVisible)
                {
                    this.RaisePropertyChanged(nameof(ScopeBitmap));
                    _mainWindowViewModel.NotifyMainIconVisibility();
                }

            }
            catch (Exception)
            {
                ScopeImageVisible = false;
            }
            this.RaisePropertyChanged();
        }
    }
    
    public bool UseGitIgnore
    {
        get => _scopeConfig.UseGitIgnore;
        set
        {
            _scopeConfig.UseGitIgnore = value;
            this.RaisePropertyChanged();
            _mainWindowViewModel.ScopeChangedRunRestart();
        }
    }
    
    public bool UseBlitzIgnore
    {
        get => _scopeConfig.UseBlitzGitIgnore;
        set
        {
            _scopeConfig.UseBlitzGitIgnore = value;
            this.RaisePropertyChanged();
            _mainWindowViewModel.ScopeChangedRunRestart();
        }
    }

    public bool UseGlobalGitIgnore
    {
        get => _scopeConfig.UseGlobalGitIgnore;
        set
        {
            _scopeConfig.UseGlobalGitIgnore = value;
            this.RaisePropertyChanged();
            _mainWindowViewModel.ScopeChangedRunRestart();
        }
    }
    
    
    
    public string ExtensionText
    {
        get => _scopeConfig.RawExtensionList;
        set
        {
            _scopeConfig.RawExtensionList = value;
            ValidateExtensionText();
            this.RaisePropertyChanged();
        }
    }

    private void ValidateExtensionText()
    {
        if (_scopeConfig?.RawExtensionList == null) return;
        bool valid = true;
        var inValidChars = new HashSet<char>( Path.GetInvalidFileNameChars());
        inValidChars.Add(';');
        foreach (char c  in _scopeConfig.RawExtensionList)
        {
            if (inValidChars.Contains(c))
            {
                _extensionValidationMessage = $"\u26d4 '{c}' is not a valid character (IE 'ext1 ext2 ext3') '";
                valid = false;
            }
        }

        if (valid)
        {
            _extensionValidationMessage = "";
        }
        this.RaisePropertyChanged(nameof(ExtensionValidation));
    }

    private string _extensionValidationMessage = "";

    public string ExtensionValidation
    {
        get => _extensionValidationMessage;
    }

    public void RunAddSearchCommand()
    {
        var configItem = new ConfigSearchPath();
        _scopeConfig.SearchPaths.Add(configItem);
        ReBuildFilePathCollectionViewModels();
    }

    public void DeleteSearchPath(SearchPathViewModel vm)
    {
        _scopeConfig.SearchPaths.Remove(vm.ConfigSearchPath);
        ReBuildFilePathCollectionViewModels();
        _mainWindowViewModel.ScopeChangedRunRestart();
    }

    private void ReBuildFilePathCollectionViewModels()
    {
        foreach (var model in SearchPathViewModels)
        {
            model.PropertyChanged -= OnPropertyChangedFileSystemRestart;
        }
        SearchPathViewModels.Clear();

        if (_scopeConfig.SearchPaths == null || _scopeConfig.SearchPaths.Count == 0)
        {
            _scopeConfig.SearchPaths = new List<ConfigSearchPath> { new(){Folder = string.Empty} };
        }
        
        foreach (var filePath in _scopeConfig.SearchPaths)
        {
            var model = new SearchPathViewModel(filePath,_mainWindowViewModel, this);
            model.PropertyChanged += OnPropertyChangedFileSystemRestart;
            SearchPathViewModels.Add(model);
        }
        // foreach (var filePath in _mainWindowViewModel.SearchQuery.FilePaths)
        // {
        //     var model = new SearchPathViewModel(filePath,_mainWindowViewModel, this);
        //     model.PropertyChanged += OnPropertyChangedFileSystemRestart;
        //     SearchPathViewModels.Add(model);
        // }
    }

    private void OnPropertyChangedFileSystemRestart(object? sender, PropertyChangedEventArgs e)
    {
        _mainWindowViewModel.ScopeChangedRunRestart();
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;

namespace Blitz.Avalonia.Controls.ViewModels;

public class SearchPathViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> DeleteSearchPath { get; }

    private readonly ConfigSearchPath _searchPath;
    private readonly MainWindowViewModel _parentModel;
    private readonly ScopeViewModel _scopeViewModel;
    private bool _isCompletionFocused;

    public SearchPathViewModel(ConfigSearchPath searchPath, MainWindowViewModel parentModel, ScopeViewModel scopeViewModel)
    {
        _searchPath = searchPath;
        _parentModel = parentModel;
        _scopeViewModel = scopeViewModel;
        DeleteSearchPath = ReactiveCommand.Create(RunDeleteSearchPath);
        _isSearchPathValid = Directory.Exists(_searchPath.Folder);
    }

    public MainWindowViewModel ParentModel => _parentModel;

    public ConfigSearchPath ConfigSearchPath => _searchPath;

    private void RunDeleteSearchPath()
    {
        _scopeViewModel.DeleteSearchPath(this);
    }

    private bool _isSearchPathValid;

    public string SearchPath
    {
        get => _searchPath.Folder;
        set
        {
            AdjustCompletionItems();
            _searchPath.Folder = value;
            this.RaisePropertyChanged();
            IsSearchPathValid = Directory.Exists(value);
        }
    }

    public bool TopLevelOnly
    {
        get => _searchPath .TopLevelOnly;
        set
        {
            _searchPath.TopLevelOnly = value;
            this.RaisePropertyChanged();
        }
    }

    public bool IsSearchPathValid
    {
        get => _isSearchPathValid;
        set => this.RaiseAndSetIfChanged(ref _isSearchPathValid, value);
    }
    
    public bool IsCompletionFocused
    {
        get => _isCompletionFocused;
        set => this.RaiseAndSetIfChanged(ref _isCompletionFocused, value);
    }

    private async void AdjustCompletionItems()
    {
        await Task.Delay(1);
        char[] slashes = new[] { '/', '\\' };
        int slashIndex = _searchPath.Folder.LastIndexOfAny(slashes);
        if (IsCompletionFocused && _searchPath.Folder.Length > 1)
        {
            return;
        }
        CompletionItems.Clear();
        if (slashIndex != -1)
        {
            string dir = _searchPath.Folder.Substring(0, slashIndex + 1);
            if (Directory.Exists(dir))
            {
                var singledepth = new List<string>();
                foreach (var pathEnum in Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly))
                {
                    CompletionItems.Add(pathEnum);
                    singledepth.Add(pathEnum);
                }

                foreach (var dirOneDeep in singledepth)
                {
                    try
                    {
                        foreach (var pathEnum in Directory.EnumerateDirectories(dirOneDeep, "*", SearchOption.TopDirectoryOnly))
                        {
                            CompletionItems.Add(pathEnum);
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                }
            }
        }
        else
        {
            if (_searchPath.Folder.Length == 2 && _searchPath.Folder[1] == ':')
            {
                string dir = _searchPath.Folder + "\\";
                
                if (Directory.Exists(dir))
                {
                    foreach (var pathEnum in Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly))
                    {
                        CompletionItems.Add(pathEnum);
                    }
                }
            }
        }
    
    }
    public ObservableCollection<string> CompletionItems { get; set; } = new ObservableCollection<string>();
}
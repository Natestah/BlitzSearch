using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Blitz.Avalonia.Controls.ViewModels;
using Blitz.AvaloniaEdit.Models;
using Blitz.AvaloniaEdit.ViewModels;
using Blitz.Goto;
using Blitz.Interfacing;
using DynamicData;
using Markdown.Avalonia;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using TextMateSharp.Grammars;
using Tmds.DBus.Protocol;
using MainWindowViewModel = Blitz.Avalonia.Controls.ViewModels.MainWindowViewModel;

namespace Blitz.Avalonia.Controls.Views;

public partial class BlitzMainPanel : UserControl
{
    public BlitzMainPanel()
    {
        InitializeComponent();
        ReactiveCommand.Create(CopyCommandRun);
        PoorMansIPC.Instance.RegisterAction("SET_SEARCH", IPC_SETSEARCH);
        PoorMansIPC.Instance.RegisterAction("SET_REPLACE", IPC_SETREPLACE);
        PoorMansIPC.Instance.RegisterAction("SET_THEME", IPC_SET_THEME);
        PoorMansIPC.Instance.RegisterAction("SET_THEME_LIGHT", IPC_SET_THEME_LIGHT);
        PoorMansIPC.Instance.RegisterAction("WORKSPACE_UPDATE", IPC_UPDATE_WORKSPACE_UPDATE);
        PoorMansIPC.Instance.RegisterAction("VS_SOLUTION", IPC_UPDATE_VS_SOLUTION);
        PoorMansIPC.Instance.RegisterAction("VS_PROJECT", IPC_UPDATE_VS_PROJECT_SELECTED);
        PoorMansIPC.Instance.RegisterAction("VS_ACTIVE_FILES", IPC_UPDATE_VS_ACTIVE_FILES);
        PoorMansIPC.Instance.RegisterAction("SUBLIME_TEXT_WORKSPACE", IPC_SUBLIME_TEXT_WORKSPACE);
        PoorMansIPC.Instance.ExecuteWithin(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    void IPC_UPDATE_VS_ACTIVE_FILES(string text)
    {
        if (_mainWindowViewModel == null || string.IsNullOrEmpty(text))
        {
            return;
        }
        var list = JsonSerializer.Deserialize(text,JsonContext.Default.ActiveFilesList);
        Dispatcher.UIThread.Post(() => _mainWindowViewModel.UpdateActiveFiles(list));
    }

    private void IPC_UPDATE_VS_SOLUTION(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_mainWindowViewModel == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            SolutionExport? configFromFile = null;
            try
            {
                configFromFile = JsonSerializer.Deserialize(text,JsonContext.Default.SolutionExport);
                if (configFromFile is null)
                {
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            string? existingProject = _mainWindowViewModel.SolutionViewModel?.SelectedProject?.Name;

            var solutionID = SolutionID.CreateFromSolutionPath(configFromFile.Name);
            
            
            
            var existingSolutionViewModel =
                _mainWindowViewModel.SolutionViewModels.FirstOrDefault(model => model.SolutionIdentity.Identity == solutionID.Identity);
            if (existingSolutionViewModel != null)
            {
                var index = _mainWindowViewModel.SolutionViewModels.IndexOf(existingSolutionViewModel);
                _mainWindowViewModel.SolutionViewModels.Remove(existingSolutionViewModel);
                //needs a rebuild..
               
                var newViewModel = new SolutionViewModel(existingSolutionViewModel.SolutionIdentity, _mainWindowViewModel)
                {
                    Export = configFromFile
                };
                _mainWindowViewModel.SolutionViewModels.Insert(index, newViewModel);
                _mainWindowViewModel.SolutionViewModel = newViewModel;
            }
            else
            {
                var newViewModel = new SolutionViewModel(solutionID, _mainWindowViewModel)
                {
                    Export = configFromFile
                };
                _mainWindowViewModel.SolutionViewModels.Insert(0, newViewModel);
                _mainWindowViewModel.SolutionViewModel = newViewModel;
            }

            var slnList = new List<SolutionID>();
            foreach (var solution in _mainWindowViewModel.SolutionViewModels)
            {
                slnList.Add(solution.SolutionIdentity);
            }
            Configuration.Instance.SolutionsVisited = slnList;
            
            if (string.IsNullOrEmpty(existingProject))
            {
                _mainWindowViewModel.SolutionViewModel.SelectedProject = _mainWindowViewModel.SolutionViewModel.Projects.FirstOrDefault() ?? new ProjectViewModel(new Project(){Name = "Default"});
            }
            else
            {
                _mainWindowViewModel.SolutionViewModel.SelectedProject = _mainWindowViewModel.SolutionViewModel.Projects.FirstOrDefault(project=>project.Name==existingProject) 
                                                                         ?? _mainWindowViewModel.SolutionViewModel.Projects.FirstOrDefault() 
                                                                         ?? new ProjectViewModel(new Project(){Name = "Default"});
            }

            if (!_mainWindowViewModel.IsFoldersScopeSelected)
            {
                _mainWindowViewModel.IsSolutionScopeSelected = true;
            }
        });
    }

    
    
    private void IPC_UPDATE_VS_PROJECT_SELECTED(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_mainWindowViewModel == null)
            {
                return;
            }
            var configFromFile = JsonSerializer.Deserialize(text,Blitz.JsonContext.Default.SelectedProjectExport);
            if (configFromFile is null)
            {
                return;
            }

            var currentExport = _mainWindowViewModel?.SolutionViewModel?.Export;
            if(currentExport?.Name != configFromFile.BelongsToSolution)
            {
                return;
            }
             
            var existingProject = _mainWindowViewModel?.SolutionViewModel?.Projects.FirstOrDefault( project=>project.Name == configFromFile.Name );
            if (existingProject == null)
            {
                return;
            }

            if (_mainWindowViewModel?.SolutionViewModel != null)
            {
                _mainWindowViewModel.SolutionViewModel.SelectedProject = existingProject;
            }
        });
    }


    private void IPC_UPDATE_WORKSPACE_UPDATE(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
             if (DataContext is not MainWindowViewModel mainWindowViewModel)
             {
                 return;
             }
            
             if (mainWindowViewModel.SelectedEditorViewModel is not { IsVsCode: true } and not { IsCursor: true } and not { IsWindsurf: true })
             {
                 return;
             }
            
             var configFromFile = JsonSerializer.Deserialize(text,Blitz.JsonContext.Default.FolderWorkspace);
             if (string.IsNullOrEmpty(configFromFile?.Name))
             {
                 mainWindowViewModel.SelectedWorkspaceScopeViewModel = null;
                 return;
             }
            
             var workspaceExport = new WorkspaceExport()
                 { Name = configFromFile.Name, Folders = configFromFile.Folders };
             
             
             var workspaceScopeViewModel = mainWindowViewModel.WorkspaceScopeViewModels.FirstOrDefault(space=>space.WorkspaceExport.Name==configFromFile.Name);

             if (workspaceScopeViewModel is null)
             {
                 workspaceScopeViewModel = new WorkspaceScopeViewModel(mainWindowViewModel, workspaceExport)
                 {
                     ExecutableIconHint = configFromFile.ExeForIcon
                 };
                 mainWindowViewModel.WorkspaceScopeViewModels.Add(workspaceScopeViewModel);
             }
             else
             {
                 workspaceScopeViewModel.ExecutableIconHint = configFromFile.ExeForIcon;
                 workspaceScopeViewModel.WorkspaceExport = workspaceExport;
             }
             
             mainWindowViewModel.SelectedWorkspaceScopeViewModel = workspaceScopeViewModel;
             mainWindowViewModel.SolutionViewModel = null;
            
             if (!mainWindowViewModel.IsFoldersScopeSelected)
             {
                 mainWindowViewModel.IsWorkspaceScopeSelected = true;
             }
        });
    }

    private string GetNameFromWorkspace(FolderWorkspace workspace)
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
                builder.Append( System.IO.Path.GetFileName(folder) );
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
    
    private void IPC_SUBLIME_TEXT_WORKSPACE(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
             if (DataContext is not MainWindowViewModel { SelectedEditorViewModel.IsSublimeText: true } mainWindowViewModel)
             {
                 return;
             }
            
             if(string.IsNullOrEmpty(text)) return;
             
             var workspaces = JsonSerializer.Deserialize(text, JsonContext.Default.ListFolderWorkspace);


             if (workspaces is null)
             {
                 return;
             }

             //switching to workspace
             mainWindowViewModel.SolutionViewModel = null;
                
             bool selectedWorkspace = false;
             bool existingIsSelected = false;

             foreach (var folderWorkspace in workspaces)
             {
                 var name = GetNameFromWorkspace(folderWorkspace);
                 var existing = mainWindowViewModel.WorkspaceScopeViewModels.FirstOrDefault(space=>space.Title==name);
                 var updatedExport = new WorkspaceExport{Name = name, Folders = folderWorkspace.Folders};
                 if (existing != null)
                 {
                     existing.WorkspaceExport = updatedExport;
                     if (existing == mainWindowViewModel.SelectedWorkspaceScopeViewModel)
                     {
                         existingIsSelected = true;
                     }
                 }
                 else
                 {
                     var workspaceScopeViewModel = new WorkspaceScopeViewModel(mainWindowViewModel, updatedExport){ExecutableIconHint = folderWorkspace.ExeForIcon};
                     mainWindowViewModel.WorkspaceScopeViewModels.Insert(0,workspaceScopeViewModel);
                     mainWindowViewModel.SelectedWorkspaceScopeViewModel = workspaceScopeViewModel;
                     selectedWorkspace = true;
                 }
             }

             if (!existingIsSelected && !selectedWorkspace)
             {
                 mainWindowViewModel.SelectedWorkspaceScopeViewModel =
                     mainWindowViewModel.WorkspaceScopeViewModels.FirstOrDefault();
             }
             
             if (!mainWindowViewModel.IsFoldersScopeSelected)
             {
                 mainWindowViewModel.IsWorkspaceScopeSelected = true;
             }
        });
    }

    private MainWindowViewModel? _mainWindowViewModel;
    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainWindowViewModel)
        {
            _mainWindowViewModel = mainWindowViewModel;
            BlitzSecondary.ShowHelp();
            mainWindowViewModel.SelectedFileChanged += (o, _) =>
            {
                if (o != null)
                {
                    BlitzSecondary.ShowPreview(o);
                }
            };
            mainWindowViewModel.SelectedFontFamily =
                mainWindowViewModel.FontFamilies.First(font =>
                    font.Name.Equals(Configuration.Instance.EditorConfig.FontFamily));
            mainWindowViewModel.ShowImportantMessage = ShowImportantMessage;

            //This is here for now, but needs to be configurable
            //https://github.com/Natestah/BlitzSearch/issues/87
            AddDefaultKeyBoundCommand(CommandNames.SaveFile, Key.S, KeyModifiers.Control,
                BlitzSecondary.FileView.SaveCurrentDocument);
            AddDefaultKeyBoundCommand(CommandNames.SearchThis, Key.F8, KeyModifiers.Control, SearchThisAction);
            AddDefaultKeyBoundCommand(CommandNames.SearchThisGithub, Key.F9, KeyModifiers.Control,
                BlitzSecondary.SearchThisGitHubAction);
            AddDefaultKeyBoundCommand(CommandNames.GotoInEditor, Key.F12, KeyModifiers.Control,
                BlitzSecondary.GotoPreviewLineRun);

            BlitzSecondary.AvaloniaTextEditor.ContextRequested += ContextMenuOnContextRequested;

            SearchPanel.KeyDownAction = MainSearchField_OnKeyDown;
        }

        base.OnLoaded(e);

        if (DataContext is MainWindowViewModel mv)
        {
            mv.EditorViewModel.PopulateThemeModels();

            AddFileBasedSelectedTheme();
            
            mv.SetSelectedThemeModels();
            BlitzSecondary.FileView.ReApplyTheme();
            mv.UpdateScopeSelectionForEditor();
        }

        RecallSolutionsVisited();
    }

    private void RecallSolutionsVisited()
    {
        if (_mainWindowViewModel is null)
        {
            throw new NullReferenceException();
        }
        var solutions = Configuration.Instance.SolutionsVisited;
        
        HashSet<string> visited = [];
        foreach (var solution in solutions)
            visited.Add(solution.Identity);
        
        // Add outside of configuration ( say if VS sent the command but Blitz was shut down )
        foreach (var solutionId in PoorMansIPC.Instance.GetSolutionTitles())
        {
            if (visited.Contains(solutionId.Identity))
            {
                continue;
            }
            solutions.Add(solutionId);
        }

        bool foundSelection = false;
        foreach (var solution in solutions)
        {
            var newViewModel = new SolutionViewModel(solution, _mainWindowViewModel);
            _mainWindowViewModel.SolutionViewModels.Add(newViewModel);
            if (!solution.Identity.Equals(Configuration.Instance.SelectedSolutionID.Identity,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            foundSelection = true;
            _mainWindowViewModel.SolutionViewModel = newViewModel;
        }

        if (!foundSelection && _mainWindowViewModel.SolutionViewModels.Count > 0)
        {
            _mainWindowViewModel.SolutionViewModel = _mainWindowViewModel.SolutionViewModels[0];
        }
    }
    
    

    private void AddFileBasedSelectedTheme()
    {
        if (DataContext is not MainWindowViewModel mv)
        {
            return; 
        }

        //Return if it's NOT a path.
        try
        {
            if (!File.Exists(Configuration.Instance.SelectedThemePremium))
            {
                return;
            }
        }
        catch (Exception)
        {
            //Selected theme can be the enum name for built ins
            return;
        }
        
        var baseTheme = Configuration.Instance.SelectedThemeIsDark ? BlitzTheme.Dark : BlitzTheme.Light;
        var theme = mv.EditorViewModel.FromBase(baseTheme, Configuration.Instance.SelectedThemePremium);
        var themeViewModel = new ThemeViewModel(theme);
        mv.EditorViewModel.AllThemeViewModels.Add(themeViewModel);
    }

    private void ContextMenuOnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        var vm = this.DataContext as MainWindowViewModel;
        if (vm == null)
        {
            return;
        }
        
        BlitzSecondary.AvaloniaTextEditor.ContextMenu ??= new ContextMenu();

        var menu = BlitzSecondary.AvaloniaTextEditor.ContextMenu;
        menu.Items.Clear();

        string text = BlitzSecondary.UpdateSearchThisPreview();
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        var keyShortcut = _commandKeybindings[CommandNames.SearchThis];
        var menuItem = new MenuItem
        {
            Header = $"Search '{text}'",
            InputGesture = keyShortcut.Gesture,
            Command = keyShortcut.Command,
        };
        menu.Items.Add(menuItem);

        keyShortcut = _commandKeybindings[CommandNames.GotoInEditor];
        var iconConverter = new GotoEditorImageConverter();
        var gotoEditorVm = vm.SelectedEditorViewModel;
        if (gotoEditorVm != null)
        {
            var convertedIcon = iconConverter.Convert([gotoEditorVm.Executable, gotoEditorVm.ExecutableIconHint],
                typeof(Bitmap), null, CultureInfo.CurrentCulture);
            var image = new Image
            {
                Source = convertedIcon as Bitmap,
            };

            menuItem = new MenuItem()
            {
                Header = $"Open in '{gotoEditorVm.Title}'",
                InputGesture = keyShortcut.Gesture,
                Command = keyShortcut.Command,
                Icon = image
            };
            menu.Items.Add(menuItem);
        }
        

        keyShortcut = _commandKeybindings[CommandNames.SearchThisGithub];
        var materialIcon = new MaterialIcon
        {
            Kind = MaterialIconKind.Github,
        };
        menuItem = new MenuItem
        {
            Header = $"Search GitHub for '{text}'",
            InputGesture = keyShortcut.Gesture,
            Command = keyShortcut.Command,
            Icon = materialIcon
        };

        menu.Items.Add(menuItem);
    }


    enum CommandNames
    {
        SaveFile,
        SearchThis,
        SearchThisGithub,
        GotoInEditor
    }

    private Dictionary<CommandNames, KeyBinding> _commandKeybindings = new();

    private void AddDefaultKeyBoundCommand(CommandNames name, Key key, KeyModifiers modifiers, Action action)
    {
        var binding = AddDefaultKeyBoundCommand(key, modifiers, action);
        _commandKeybindings.Add(name, binding);
    }

    public KeyBinding AddDefaultKeyBoundCommand(Key key, KeyModifiers modifiers, Action command)
    {
        var binding = new KeyBinding() { Gesture = new KeyGesture(key), Command = ReactiveCommand.Create(command) };
        BlitzSecondary.AvaloniaTextEditor.KeyBindings.Add(binding);
        return binding;
    }


    private void SearchThisAction()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        var searchText = BlitzSecondary.UpdateSearchThisPreview();
        mainWindowViewModel.SearchTextBox = searchText;
        SearchPanel.MainSearchField.SelectAll();
        SearchPanel.MainSearchField.Focus();
    }

    private void IPC_SETSEARCH(string search)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is not MainWindowViewModel mainWindowViewModel)
            {
                return;
            }

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
                {
                    MainWindow: { } window
                })
            {
                if (window.WindowState == WindowState.Minimized)
                {
                    window.WindowState = mainWindowViewModel.LastNonMinizedState;
                }
                else
                {
                    window.WindowState = WindowState.Minimized;
                    window.WindowState = mainWindowViewModel.LastNonMinizedState;
                }

                window.BringIntoView();
                window.Activate();
            }

            mainWindowViewModel.SearchTextBox = search;
            mainWindowViewModel.ReplaceInFileEnabled = false;
            SearchPanel.MainSearchField.SelectAll();
            SearchPanel.MainSearchField.Focus();
        });
    }

    private void SetTheme(string themePath, bool islight)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is not MainWindowViewModel mainWindowViewModel)
            {
                return;
            }
            var existing = mainWindowViewModel.EditorViewModel.AllThemeViewModels.FirstOrDefault(a=>a.Theme.ThemeName == themePath);
            if (existing != null)
            {
                mainWindowViewModel.EditorViewModel.AllThemeViewModels.Remove(existing);
            }
            var baseTheme = islight? BlitzTheme.Light : BlitzTheme.Dark;
            var theme = mainWindowViewModel.EditorViewModel.FromBase(baseTheme, themePath);
            try
            {
                var themeViewModel = new ThemeViewModel(theme);
                mainWindowViewModel.EditorViewModel.AllThemeViewModels.Add(themeViewModel);
                mainWindowViewModel.EditorViewModel.ThemeViewModel = themeViewModel;
            }
            catch (Exception exception)
            {
                //Need a box for the message,  https://github.com/Natestah/BlitzSearch/issues/85
                Console.WriteLine(exception);
                return;
            }

        });
    }
    private void IPC_SET_THEME(string themeName)
    {
        SetTheme(themeName, false);
    }
    private void IPC_SET_THEME_LIGHT(string themeName)
    {
        SetTheme(themeName, true);
    }

    private void IPC_SETREPLACE(string search)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is not MainWindowViewModel mainWindowViewModel) return;

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime
                {
                    MainWindow: { } window
                })
            {
                if (window.WindowState == WindowState.Minimized)
                {
                    window.WindowState = mainWindowViewModel.LastNonMinizedState;
                }
                else
                {
                    window.WindowState = WindowState.Minimized;
                    window.WindowState = mainWindowViewModel.LastNonMinizedState;
                }

                window.BringIntoView();
                window.Activate();
            }


            mainWindowViewModel.SearchTextBox = search;
            mainWindowViewModel.ReplaceInFileEnabled = true;
            mainWindowViewModel.SelectedReplaceMode = mainWindowViewModel.ReplaceModeViewModels
                .Where(a => a.IconKind == MaterialIconKind.FileWordBox).FirstOrDefault();
            mainWindowViewModel.ReplaceBoxText = search;
            mainWindowViewModel.ReplaceWithBoxText = search.Replace("@", "").Replace("^", "");
            ReplacePanel.ReplaceTextWithBox.SelectAll();
            ReplacePanel.ReplaceTextWithBox.Focus();
        });
    }

    private async void CopyCommandRun()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.Clipboard is not { } provider)
            throw new NullReferenceException("Missing Clipboard instance.");

        if (DataContext is MainWindowViewModel vm)
        {
            var copyBuilder = new StringBuilder();
            foreach (var copyable in vm.SelectedItems.OfType<IResultCopiable>())
            {
                copyBuilder.AppendLine(copyable.CopyText);
            }

            await provider.SetTextAsync(copyBuilder.ToString());
        }
    }

    public void ShowImportantMessage(string message)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.EnableTextPane = true;
        BlitzSecondary.ShowPreview(message);
    }

    
    public void SetRestartAction(Action doUpdateAndRestart)
    {
        this.StatusBar.InstallerClick = doUpdateAndRestart;
    }

    private async void AcceptChangesClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;

        mainWindowViewModel.StopRespondingToCurrentQuery();

        List<FileNameResult> items = [];
        foreach (var change in mainWindowViewModel.ResultBoxItems.OfType<FileNameResultViewModel>())
        {
            items.Add(change.FileNameResult);
        }

        mainWindowViewModel.ResultBoxItems.Clear();

        var summary = new StringBuilder();
        int total = items.Count;
        int successCount = 0;
        foreach (var item in items)
        {
            summary.AppendLine(item.FileName);
            summary.AppendLine();

            try
            {
                var text = await File.ReadAllTextAsync(item.FileName);
                text = item.GetReplaceResults(text);
                await File.WriteAllTextAsync(item.FileName, text);
            }
            catch (Exception ex)
            {
                mainWindowViewModel.ResultBoxItems.Add(ExceptionResult.CreateFromException(ex));
            }

            foreach (var contentResult in item.ContentResults)
            {
                summary.Append("      ");
                summary.AppendLine(contentResult.CapturedContents);
                summary.Append("    \u27a1\ufe0f");
                summary.AppendLine(contentResult.ReplacedContents);
                summary.AppendLine();
            }

            successCount++;
        }

        var replaceTextViewModel = new ReplaceTextViewModel(summary.ToString(), successCount, total);
        mainWindowViewModel.SelectedItems.Clear();
        mainWindowViewModel.ResultBoxItems.Add(replaceTextViewModel);
        mainWindowViewModel.SelectedItems.Add(replaceTextViewModel);
    }

    private void MainSearchField_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (e.Key == Key.Down)
        {
            var first = mainWindowViewModel.ResultBoxItems.OfType<ContentResultViewModel>().FirstOrDefault()
                        ?? mainWindowViewModel.ResultBoxItems.FirstOrDefault();

            if (first is null) return;
            mainWindowViewModel.SelectedItems.Clear();
            mainWindowViewModel.SelectedItems.Add(first);
            if (!ResultsBox.ResultsListBox.Focus())
            {
                ResultsBox.ResultsListBox.Focusable = true;
                ResultsBox.ResultsListBox.Focus();
                ResultsBox.ResultsListBox.Focusable = false;
            }

            e.Handled = true;
        }

        if (e.Key == Key.Enter)
        {
            var first = mainWindowViewModel.ResultBoxItems.OfType<ContentResultViewModel>().FirstOrDefault()
                        ?? mainWindowViewModel.ResultBoxItems.FirstOrDefault();

            if (first is null || mainWindowViewModel.SelectedEditorViewModel == null) return;

            if (!mainWindowViewModel.SelectedEditorViewModel.RunTotoOnObjectGoto(first,false,
                    out string errorMessage))
            {
                mainWindowViewModel.ShowImportantMessage?.Invoke(errorMessage);
            }
        }
    }
    
    

}
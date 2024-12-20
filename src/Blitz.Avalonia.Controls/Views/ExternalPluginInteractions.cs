using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Avalonia.Threading;
using Blitz.Avalonia.Controls.ViewModels;
using Blitz.AvaloniaEdit.Models;
using Blitz.AvaloniaEdit.ViewModels;
using Blitz.Goto;
using Blitz.Interfacing;
using Material.Icons;
using MainWindowViewModel = Blitz.Avalonia.Controls.ViewModels.MainWindowViewModel;

namespace Blitz.Avalonia.Controls.Views;

public class ExternalPluginInteractions
{
    private readonly MainWindowViewModel _viewModel;
    private readonly PluginCommands _commander;
    public ExternalPluginInteractions(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        _commander = new PluginCommands();
        RegisterCommands();
    }
    
    public PluginCommands Commander => _commander;

    private void RegisterCommands()
    {
        _commander.RegisterAction(PluginCommands.SetSearch, SetSearchBoxText);
        _commander.RegisterAction(PluginCommands.SetContextSearch, SetSearchBoxContextSearch);
        _commander.RegisterAction(PluginCommands.SetReplace, SetReplaceBoxText);
        _commander.RegisterAction(PluginCommands.SetContextReplace, SetSearchBoxContextReplace);
        _commander.RegisterAction(PluginCommands.SetTheme, SetTheme);
        _commander.RegisterAction(PluginCommands.SetThemeLight, SetThemeLight);
        _commander.RegisterAction(PluginCommands.VisualStudioCodeWorkspaceUpdate, UpdateVisualStudioCodeFolderWorkspace);
        _commander.RegisterAction(PluginCommands.UpdateVisualStudioSolution, UpdateVisualStudioSolution);
        _commander.RegisterAction(PluginCommands.UpdateVisualStudioProject, UpdateVisualStudioSelectedProject);
        _commander.RegisterAction(PluginCommands.UpdateVisualStudioActiveFiles, UpdateVisualStudioActiveFilesList);
        _commander.RegisterAction(PluginCommands.SublimeTextWorkspaceUpdate, UpdateSublimeTextWorkspace);
        _commander.RegisterAction(PluginCommands.SimpleFolderSearch, SetSimpleFolderSearch);
        _commander.ExecuteWithin(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    void SetSimpleFolderSearch(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            const string ExplorerFolder = "Explorer";
            var existing = _viewModel.ScopeViewModels.FirstOrDefault(x => x.ScopeTitle == ExplorerFolder);
            var path = new ConfigSearchPath
            {
                Folder = text,
            };
            var index = 0;
            if (existing != null)
            {
                index = _viewModel.ScopeViewModels.IndexOf(existing);
                _viewModel.ScopeViewModels.Remove(existing);
            }
            
            var scopeConfig = new ScopeConfig()
            {
                ScopeTitle = ExplorerFolder,
                SearchPaths = [path],
            };
        
            var newVm = new ScopeViewModel(_viewModel, scopeConfig);
            _viewModel.ScopeViewModels.Insert(index, newVm);
            _viewModel.SelectedScope = newVm;
            _viewModel.IsFoldersScopeSelected = true;
            _viewModel.ActivateMainWindow();
        });
    }

    void UpdateVisualStudioActiveFilesList(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var list = JsonSerializer.Deserialize(text, JsonContext.Default.ActiveFilesList);
        Dispatcher.UIThread.Post(() => _viewModel.UpdateActiveFiles(list));
    }

    private void SetSearchBoxContextSearch(string context)=>SetSearchBoxContext(context,false);
    private void SetSearchBoxContextReplace(string context)=>SetSearchBoxContext(context,true);
    
    private void SetSearchBoxContext(string context, bool forReplace)
    {
        var searchBoxContext = JsonSerializer.Deserialize(context, JsonContext.Default.SetSearchBoxContext);
        if (searchBoxContext == null)
        {
            return;
        }
        Dispatcher.UIThread.Post(() =>
        {
            if (forReplace)
            {
                SetReplaceBoxText(searchBoxContext.SearchBoxString);
            }
            else
            {
                SetSearchBoxText(searchBoxContext.SearchBoxString);
            }
            if (searchBoxContext.EditorId == null)
            {
                return;
            }
            
            try
            {
                var process = Process.GetProcessById(searchBoxContext.ProcessId);
                var name = Path.GetFileName(process.MainModule!.FileName);
                foreach (var editor in _viewModel.GotoEditorCollection)
                {
                    var processName = string.IsNullOrEmpty(editor.RunningProcessName) ? editor.Executable : editor.RunningProcessName;
                    if (!processName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    _viewModel.SelectedEditorViewModel = editor;;
                    break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            if (_viewModel.SolutionViewModel != null)
            {
                if (_viewModel.SolutionViewModel.SolutionIdentity == searchBoxContext.EditorId)
                {
                    return;
                }

                
                foreach (var solutionVm in _viewModel.SolutionViewModels)
                {
                    if (solutionVm.SolutionIdentity != searchBoxContext.EditorId)
                    {
                        continue;
                    }
                    _viewModel.SolutionViewModel = solutionVm;
                    return;
                }
                    
                var newModel = new SolutionViewModel(searchBoxContext.EditorId,_viewModel);
                _viewModel.SolutionViewModels.Insert(0,newModel);
                _viewModel.SolutionViewModel = newModel;
                return;
            }
                
            if (_viewModel.SelectedWorkspaceScopeViewModel != null)
            {
                if (_viewModel.SelectedWorkspaceScopeViewModel.SolutionIdentity == searchBoxContext.EditorId)
                {
                    return;
                }

                foreach (var solutionVm in _viewModel.WorkspaceScopeViewModels)
                {
                    if (solutionVm.SolutionIdentity != searchBoxContext.EditorId)
                    {
                        continue;
                    }
                    _viewModel.SelectedWorkspaceScopeViewModel = solutionVm;
                    return;
                }
                var newModel = new WorkspaceScopeViewModel(_viewModel, searchBoxContext.EditorId);
                _viewModel.WorkspaceScopeViewModels.Insert(0,newModel);
                _viewModel.SelectedWorkspaceScopeViewModel = newModel;
            }
        });
        
    }

    private void SetSearchBoxText(string search)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _viewModel.ActivateMainWindow();
            _viewModel.SearchTextBox = search;
            _viewModel.ReplaceInFileEnabled = false;
            _viewModel.FocusSearch();
        });
    }


    private void SetReplaceBoxText(string search)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _viewModel.ActivateMainWindow();
            _viewModel.SearchTextBox = search;
            _viewModel.ReplaceInFileEnabled = true;
            _viewModel.SelectedReplaceMode =
                _viewModel.ReplaceModeViewModels.FirstOrDefault(a => a.IconKind == MaterialIconKind.FileWordBox);
            _viewModel.ReplaceBoxText = search;
            _viewModel.ReplaceWithBoxText = search.Replace("@", "").Replace("^", "");
            _viewModel.FocusReplace();
        });
    }


    private void SetTheme(string themePath, bool islight)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var existing =
                _viewModel.EditorViewModel.AllThemeViewModels.FirstOrDefault(a => a.Theme.ThemeName == themePath);
            if (existing != null)
            {
                _viewModel.EditorViewModel.AllThemeViewModels.Remove(existing);
            }

            var baseTheme = islight ? BlitzTheme.Light : BlitzTheme.Dark;
            var theme = _viewModel.EditorViewModel.FromBase(baseTheme, themePath);
            try
            {
                var themeViewModel = new ThemeViewModel(theme);
                _viewModel.EditorViewModel.AllThemeViewModels.Add(themeViewModel);
                _viewModel.EditorViewModel.ThemeViewModel = themeViewModel;
            }
            catch (Exception exception)
            {
                //Need a box for the message,  https://github.com/Natestah/BlitzSearch/issues/85
                Console.WriteLine(exception);
            }
        });
    }

    private void SetTheme(string themeName) => SetTheme(themeName, false);
    private void SetThemeLight(string themeName) => SetTheme(themeName, true);

    private void UpdateVisualStudioCodeFolderWorkspace(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (_viewModel.SelectedEditorViewModel is not { IsVsCode: true } and not { IsCursor: true } and not
                { IsWindsurf: true })
            {
                return;
            }

            var configFromFile = JsonSerializer.Deserialize(text, Blitz.JsonContext.Default.FolderWorkspace);
            if (string.IsNullOrEmpty(configFromFile?.Name))
            {
                _viewModel.SelectedWorkspaceScopeViewModel = null;
                return;
            }


            var workspaceScopeViewModel =
                _viewModel.WorkspaceScopeViewModels.FirstOrDefault(space =>
                    space.WorkspaceExport?.Name == configFromFile.Name);

            if (workspaceScopeViewModel is null)
            {
                var solutionId = SolutionID.CreateFromSolutionPath(configFromFile.Name);
                workspaceScopeViewModel = new WorkspaceScopeViewModel(_viewModel, solutionId)
                {
                    WorkspaceExport = configFromFile
                };
                _viewModel.WorkspaceScopeViewModels.Add(workspaceScopeViewModel);
            }
            else
            {
                workspaceScopeViewModel.WorkspaceExport = configFromFile;
            }

            _viewModel.SelectedWorkspaceScopeViewModel = workspaceScopeViewModel;
            _viewModel.SolutionViewModel = null;

            if (!_viewModel.IsFoldersScopeSelected)
            {
                _viewModel.IsWorkspaceScopeSelected = true;
            }
        });
    }


    private void UpdateVisualStudioSolution(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            SolutionExport? configFromFile = null;
            try
            {
                configFromFile = JsonSerializer.Deserialize(text, JsonContext.Default.SolutionExport);
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

            if (_viewModel.SelectedEditorViewModel is { IsVisualStudio: false })
            {
                foreach (var editor in _viewModel.GotoEditorCollection)
                {
                    if (editor.IsVisualStudio)
                    {
                        _viewModel.SelectedEditorViewModel = editor;
                        break;
                    }
                }
            }
            

            var existingProject = _viewModel.SolutionViewModel?.SelectedProject?.Name;

            var solutionId = SolutionID.CreateFromSolutionPath(configFromFile.Name);
            var existingSolutionViewModel =
                _viewModel.SolutionViewModels.FirstOrDefault(model =>
                    model.SolutionIdentity.Identity == solutionId.Identity);
            if (existingSolutionViewModel != null)
            {
                var index = _viewModel.SolutionViewModels.IndexOf(existingSolutionViewModel);
                _viewModel.SolutionViewModels.Remove(existingSolutionViewModel);
                //needs a rebuild..

                var newViewModel = new SolutionViewModel(existingSolutionViewModel.SolutionIdentity, _viewModel)
                {
                    Export = configFromFile
                };
                _viewModel.SolutionViewModels.Insert(index, newViewModel);
                _viewModel.SolutionViewModel = newViewModel;
            }
            else
            {
                var newViewModel = new SolutionViewModel(solutionId, _viewModel)
                {
                    Export = configFromFile
                };
                _viewModel.SolutionViewModels.Insert(0, newViewModel);
                _viewModel.SolutionViewModel = newViewModel;
            }

            var slnList = new List<SolutionID>();
            foreach (var solution in _viewModel.SolutionViewModels)
            {
                slnList.Add(solution.SolutionIdentity);
            }

            Configuration.Instance.SolutionsVisited = slnList;

            if (string.IsNullOrEmpty(existingProject))
            {
                _viewModel.SolutionViewModel.SelectedProject = _viewModel.SolutionViewModel.Projects.FirstOrDefault() ??
                                                               new ProjectViewModel(new Project() { Name = "Default" });
            }
            else
            {
                var toSelect =
                    _viewModel.SolutionViewModel.Projects.FirstOrDefault(project => project.Name == existingProject)
                    ?? _viewModel.SolutionViewModel.Projects.FirstOrDefault()
                    ?? new ProjectViewModel(new Project() { Name = "Default" });
                _viewModel.SolutionViewModel.SelectedProject = toSelect;
            }

            if (!_viewModel.IsFoldersScopeSelected)
            {
                _viewModel.IsSolutionScopeSelected = true;
            }
        });
    }

    private void UpdateVisualStudioSelectedProject(string text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var configFromFile = JsonSerializer.Deserialize(text, Blitz.JsonContext.Default.SelectedProjectExport);
            if (configFromFile is null)
            {
                return;
            }

            var currentExport = _viewModel.SolutionViewModel?.Export;
            if (currentExport?.Name != configFromFile.BelongsToSolution)
            {
                return;
            }

            var existingProject =
                _viewModel.SolutionViewModel?.Projects.FirstOrDefault(project => project.Name == configFromFile.Name);
            if (existingProject == null)
            {
                return;
            }

            if (_viewModel.SolutionViewModel != null)
            {
                _viewModel.SolutionViewModel.SelectedProject = existingProject;
            }
        });
    }



    private void UpdateSublimeTextWorkspace(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_viewModel is not { SelectedEditorViewModel.IsSublimeText: true })
            {
                return;
            }
            var workspaces = JsonSerializer.Deserialize(text, JsonContext.Default.ListFolderWorkspace);
            if (workspaces is null)
            {
                return;
            }
            _viewModel.ApplySublimeTextListOfWorkspaces(workspaces);
        });
    }

    public void RecallSharedConfigurations()
    {
        RecallSolutionsVisited();
    }


    private void RecallSolutionsVisited()
    {
        var solutions = Configuration.Instance.SolutionsVisited;
        
        HashSet<string> visited = [];
        foreach (var solution in solutions)
            visited.Add(solution.Identity);
        
        // Add outside of configuration ( say if VS sent the command but Blitz was shut down )
        foreach (var solutionId in _viewModel.ExternalPluginInteractions.Commander.GetSolutionTitles())
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
            var newViewModel = new SolutionViewModel(solution, _viewModel);
            _viewModel.SolutionViewModels.Add(newViewModel);
            if (!solution.Identity.Equals(Configuration.Instance.SelectedSolutionID.Identity,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            foundSelection = true;
            _viewModel.SolutionViewModel = newViewModel;
        }

        if (!foundSelection && _viewModel.SolutionViewModels.Count > 0)
        {
            _viewModel.SolutionViewModel = _viewModel.SolutionViewModels[0];
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using Markdown.Avalonia;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using TextMateSharp.Grammars;
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
//        PoorMansIPC.Instance.ExecuteNamedAction("VS_SOLUTION");

        
        PoorMansIPC.Instance.ExecuteWithin(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }


    private void IPC_UPDATE_VS_SOLUTION(string text)
    {
         Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is not MainWindowViewModel mainWindowViewModel)
            {
                return;
            }
            var configFromFile = JsonSerializer.Deserialize(text,Blitz.JsonContext.Default.SolutionExport);
            if (configFromFile is null)
            {
                return;
            }

            string? existingProject = mainWindowViewModel.SolutionViewModel?.SelectedProject.Name;
            mainWindowViewModel.SolutionViewModel = new SolutionViewModel(configFromFile, mainWindowViewModel);
            if (string.IsNullOrEmpty(existingProject))
            {
                mainWindowViewModel.SolutionViewModel.SelectedProject = mainWindowViewModel.SolutionViewModel.Projects.FirstOrDefault() ?? new ProjectViewModel(new Project(){Name = "Default"});
            }
            else
            {
                mainWindowViewModel.SolutionViewModel.SelectedProject = mainWindowViewModel.SolutionViewModel.Projects.FirstOrDefault(project=>project.Name==existingProject) ?? new ProjectViewModel(new Project(){Name = "Default"});
            }
            // bool selectingNewlyCreatedNode = false;
            // var existing = mainWindowViewModel.ScopeViewModels.FirstOrDefault(v=>v.ScopeTitle == configFromFile.Name);
            // if (existing is null)
            // {
            //     existing = new ScopeViewModel(mainWindowViewModel, new ScopeConfig());
            //     existing.ScopeTitle = configFromFile.Name;
            //     mainWindowViewModel.ScopeViewModels.Add(existing);
            //     selectingNewlyCreatedNode = true;
            // }
            //
            // existing.SearchPathViewModels.Clear();
            // foreach (var folder in configFromFile.Folders)
            // {
            //     var path = new ConfigSearchPath { Folder = folder, TopLevelOnly = false };
            //     existing.SearchPathViewModels.Add( new SearchPathViewModel(path,mainWindowViewModel,existing));
            // }
            //
            // var gotoeditorConverter = new GotoEditorImageConverter();
            // var bitmap = gotoeditorConverter.Convert([configFromFile.ExeForIcon,configFromFile.ExeForIcon],typeof(Bitmap),null, CultureInfo.CurrentCulture) as Bitmap;
            //
            // var appFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            // var specificFolder = Path.Combine(appFolder, "NathanSilvers", "IMAGES");
            // Directory.CreateDirectory(specificFolder);
            // string bitmapPath = Path.Combine(specificFolder, configFromFile.ExeForIcon);
            // bitmapPath = Path.ChangeExtension(bitmapPath, "png");
            // bitmap.Save(bitmapPath);
            // existing.ScopeImage = bitmapPath;
            // mainWindowViewModel.SelectedScope = existing;
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
            
            if (mainWindowViewModel.SelectedEditorViewModel is not { IsVsCode: true })
            {
                return;
            }

            var configFromFile = JsonSerializer.Deserialize(text,Blitz.JsonContext.Default.FolderWorkspace);
            if (string.IsNullOrEmpty(configFromFile?.Name))
            {
                mainWindowViewModel.SolutionViewModel = null;
                return;
            }
            
            var solutionExport = new SolutionExport(){Name = configFromFile.Name};
            
            //for Now we're going to pretend like VS Code Workspaces are Solutions/Project.
            var fauxProject = new Project() { Name = configFromFile.Name, Files = configFromFile.Folders };
            solutionExport.Projects = [fauxProject];
            mainWindowViewModel.SolutionViewModel = new SolutionViewModel(solutionExport, mainWindowViewModel)
                {
                    ISVSCodeSolution = true
                };
            
            if (mainWindowViewModel.IsProjectScopeSelected)
            {
                mainWindowViewModel.IsProjectScopeSelected = false;
                mainWindowViewModel.IsSolutionScopeSelected = true;
            }
        });
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainWindowViewModel)
        {
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

            //Todo: user configurable bindings.. For now lets keep it simple and apply in code, as opposed to xaml bindings and such. 
            AddDefaultKeyBoundCommand(CommandNames.SaveFile, Key.S, KeyModifiers.Control,
                BlitzSecondary.FileView.SaveCurrentDocument);
            AddDefaultKeyBoundCommand(CommandNames.SearchThis, Key.F8, KeyModifiers.Control, SearchThisAction);
            AddDefaultKeyBoundCommand(CommandNames.SearchThisGithub, Key.F9, KeyModifiers.Control,
                BlitzSecondary.SearchThisGitHubAction);
            AddDefaultKeyBoundCommand(CommandNames.GotoInEditor, Key.F12, KeyModifiers.Control,
                BlitzSecondary.GotoPreviewLineRun);

            BlitzSecondary.AvaloniaTextEditor.ContextRequested += ContextMenuOnContextRequested;
            

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
        catch (Exception e)
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
        if (BlitzSecondary.AvaloniaTextEditor.ContextMenu == null)
        {
            BlitzSecondary.AvaloniaTextEditor.ContextMenu = new ContextMenu();
        }

        var menu = BlitzSecondary.AvaloniaTextEditor.ContextMenu;
        menu.Items.Clear();

        string text = BlitzSecondary.UpdateSearchThisPreview();
        if (!string.IsNullOrEmpty(text))
        {
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
        MainSearchField.SelectAll();
        MainSearchField.Focus();
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
            MainSearchField.SelectAll();
            MainSearchField.Focus();
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
                //Todo: Still need a proper message box for editor..
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
            ReplaceTextWithBox.SelectAll();
            ReplaceTextWithBox.Focus();
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

    public Action? InstallerClick;

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void NewVersionButton_OnClick(object? o, RoutedEventArgs e)
    {
        InstallerClick?.Invoke();
    }

    // ReSharper disable once UnusedParameter.Local
    private void FileNameFilterBox_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            throw new Exception("NoTextBox");
        }

        if (DataContext is MainWindowViewModel mainWindowViewModel)
            mainWindowViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.FileNameSearchEnabled) &&
                    mainWindowViewModel.FileNameSearchEnabled)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            };
    }

    // ReSharper disable once UnusedParameter.Local
    private void LiterSearchTextBox_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            throw new Exception("NoTextBox");
        }

        if (DataContext is MainWindowViewModel mainWindowViewModel)
            mainWindowViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.LiteralSearchEnabled) &&
                    mainWindowViewModel.LiteralSearchEnabled)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            };
    }

    // ReSharper disable once UnusedParameter.Local
    private void RegexSearchTextBox_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            throw new Exception("NoTextBox");
        }

        if (DataContext is MainWindowViewModel mainWindowViewModel)
            mainWindowViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.RegexSearchEnabled) &&
                    mainWindowViewModel.RegexSearchEnabled)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            };
    }

    // ReSharper disable once UnusedParameter.Local
    private void ReplaceWithBox_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            throw new Exception("NoTextBox");
        }

        if (DataContext is MainWindowViewModel mainWindowViewModel)
            mainWindowViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.ReplaceInFileEnabled) &&
                    mainWindowViewModel.ReplaceInFileEnabled)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            };
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.FileNameSearchEnabled = false;
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void LiteralSearchCloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.LiteralSearchEnabled = false;
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void RegexSearchCloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.RegexSearchEnabled = false;
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void CloseReplaceBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.ReplaceInFileEnabled = false;
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void SmartCaseOffButton(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.SearchTextBox =
            mainWindowViewModel.SearchTextBox.ToLower();
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void SmartCaseLiteralOffButton(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.LiteralSearchTextBox = mainWindowViewModel.LiteralSearchTextBox.ToLower();
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void SmartCaseRegexOffButton(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.RegexSearchTextBox = mainWindowViewModel.RegexSearchTextBox.ToLower();
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void SmartCaseReplaceOffButton(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.ReplaceBoxText = mainWindowViewModel.ReplaceBoxText.ToLower();
    }

    private const int MaxHistoryEntries = 15;

    private void MainSearchField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        UpdateHistoryForColllection(mainWindowViewModel.SearchTextHistory, mainWindowViewModel.SearchTextBox);
    }


    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not string itemName) return;
        mainWindowViewModel.SearchTextBox = itemName;
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void FileNameSearchField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        UpdateHistoryForColllection(mainWindowViewModel.SearchFileHistory, mainWindowViewModel.FileNameSearchTextBox);
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void LiteralSearchField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        UpdateHistoryForColllection(mainWindowViewModel.LiteralSearchTextHistory,
            mainWindowViewModel.LiteralSearchTextBox);
    }

    private void UpdateHistoryForColllection(ObservableCollection<string> history, string text)
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


    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void RegexSearchField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        UpdateHistoryForColllection(mainWindowViewModel.RegexSearchTextHistory, mainWindowViewModel.RegexSearchTextBox);
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void ReplaceField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        UpdateHistoryForColllection(mainWindowViewModel.SearchFileHistory, mainWindowViewModel.ReplaceBoxText);
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void LiteralSearchControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not string itemName) return;
        mainWindowViewModel.LiteralSearchTextBox = itemName;
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void RegexSearchControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not string itemName) return;
        mainWindowViewModel.RegexSearchTextBox = itemName;
    }


    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void SelectingFileItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not string itemName) return;
        mainWindowViewModel.FileNameSearchTextBox = itemName;
    }


    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void ReplaceHistoryItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not string itemName) return;
        mainWindowViewModel.ReplaceBoxText = itemName;
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void ReplaceWithHistoryItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not string itemName) return;
        mainWindowViewModel.ReplaceWithBoxText = itemName;
    }

    public void UpdateSearchThisPreview()
    {
        BlitzSecondary.UpdateSearchThisPreview();
    }

    public void SetRestartAction(Action doUpdateAndRestart)
    {
        this.InstallerClick = doUpdateAndRestart;
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

    private void SeePremiumPage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        var processStartInfo = new ProcessStartInfo("https://natestah.com/premium")
        {
            UseShellExecute = true
        };
        Process.Start(processStartInfo); // todo landing page for new version.
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

    private void AdButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is not AdSpaceViewModel adSpaceViewModel)
        {
            return;
        }

        var processStartInfo = new ProcessStartInfo(adSpaceViewModel.LinkUrl)
        {
            UseShellExecute = true
        };
        Process.Start(processStartInfo);
    }
}
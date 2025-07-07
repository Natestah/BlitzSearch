using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Blitz.Avalonia.Controls.ViewModels;
using Blitz.AvaloniaEdit.Models;
using Blitz.AvaloniaEdit.ViewModels;
using Blitz.Interfacing;
using Material.Icons;
using Material.Icons.Avalonia;
using ReactiveUI;
using MainWindowViewModel = Blitz.Avalonia.Controls.ViewModels.MainWindowViewModel;

namespace Blitz.Avalonia.Controls.Views;


public partial class BlitzMainPanel : UserControl
{
    public BlitzMainPanel()
    {
        InitializeComponent();
        ReactiveCommand.Create(CopyCommandRun);
    }


    private MainWindowViewModel? _mainWindowViewModel;
    protected override void OnLoaded(RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            _mainWindowViewModel = viewModel;
            BlitzSecondary.ShowHelp();
            viewModel.SelectedFileChanged += (o, _) =>
            {
                if (o != null)
                {
                    BlitzSecondary.ShowPreview(o);
                }
            };
            viewModel.SelectedFontFamily =
                viewModel.FontFamilies.First(font =>
                    font.Name.Equals(Configuration.Instance.EditorConfig.FontFamily));
            viewModel.ShowImportantMessage = ShowImportantMessage;

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
            _mainWindowViewModel.ExternalPluginInteractions.RecallSharedConfigurations();
            base.OnLoaded(e);
            viewModel.EditorViewModel.PopulateThemeModels();

            AddFileBasedSelectedTheme();
            
            viewModel.SetSelectedThemeModels();
            BlitzSecondary.FileView.ReApplyTheme();
            viewModel.UpdateScopeSelectionForEditor();
        }
        else
        {
            base.OnLoaded(e);
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
        var binding = new KeyBinding() { Gesture = new KeyGesture(key,modifiers), Command = ReactiveCommand.Create(command) };
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

    private static bool PerforceEditReadonlyFiles(IEnumerable<FileNameResult> files)
    {
        var readonlyFiles = new HashSet<string>();
        foreach (var item in files)
        {
            if (new FileInfo(item.FileName).IsReadOnly)
            {
                readonlyFiles.Add(item.FileName);
            }
        }

        if (readonlyFiles.Count == 0)
        {
            return false;
        }
        
        return Perforce.EditBatch(readonlyFiles);
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
        
        var readonlyFailures = new ObservableCollection<ReplaceFailureReport>();

        bool perforceFilesAdded = false;
        if (Perforce.IsDetectPerforceCommandLineInstalled())
        {
            perforceFilesAdded = PerforceEditReadonlyFiles(items);
        }
        
        foreach (var item in items)
        {
            summary.AppendLine(item.FileName);
            summary.AppendLine();

            try
            {
                await mainWindowViewModel.ApplyReplacement(item);
            }
            catch (Exception ex)
            {
                var exceptionViewModel = new ExceptionViewModel(ExceptionResult.CreateFromException(ex));
                
                if (new FileInfo(item.FileName).IsReadOnly)
                {
                    var newReadonly = new ReplaceFailureReport{ ExceptionViewModel = exceptionViewModel, FilenameResult = item };
                    readonlyFailures.Add(newReadonly);
                    mainWindowViewModel.ResultBoxItems.Add(exceptionViewModel);
                    continue;
                }
                
                mainWindowViewModel.ResultBoxItems.Add(exceptionViewModel);
                continue;
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

        var replaceTextViewModel = new ReplaceTextViewModel(summary.ToString(), successCount, total)
            {
                PerforceReplaced = perforceFilesAdded,
                ReplaceFileNameResultFailures = readonlyFailures
            };
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
            // mainWindowViewModel.SelectedItems.Clear();
            // mainWindowViewModel.SelectedItems.Add(first);
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


    public void SelectAndFocusMainSearchField()
    {
        SearchPanel.MainSearchField.SelectAll();
        SearchPanel.MainSearchField.Focus();
    }
    
    public void SelectAndFocusReplaceBoxField()
    {
        ReplacePanel.ReplaceTextWithBox.SelectAll();
        ReplacePanel.ReplaceTextWithBox.Focus();
    }
}
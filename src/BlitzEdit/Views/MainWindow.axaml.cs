using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Blitz.AvaloniaEdit.Models;
using Blitz.AvaloniaEdit.ViewModels;
using ReactiveUI;
using TextMateSharp.Grammars;
using MainWindowViewModel = BlitzEdit.ViewModels.MainWindowViewModel;

namespace BlitzEdit.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        OpenFileCommand = ReactiveCommand.Create( OpenFile );
        DataContext = new MainWindowViewModel();
        Loaded+=OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        vm.EditorViewModel.PopulateThemeModels();
        vm.EditorViewModel.PropertyChanged +=EditorViewModelOnPropertyChanged;
    }
    

    private void EditorViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not BlitzEditorViewModel blitzEditorViewModel) return;
        if (e.PropertyName == nameof(MainWindowViewModel.EditorViewModel.ThemeViewModel))
        {
            RequestedThemeVariant = blitzEditorViewModel.ThemeViewModel != null && blitzEditorViewModel.ThemeViewModel.ThemeName.ToString().ToLower().Contains("light")
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
        }
    }

    ReactiveCommand<Unit, Unit> OpenFileCommand { get; }

    public async void OpenFile()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open File",
            AllowMultiple = false,
        });

        if (files.Count < 1)
        {
            return;
        }

        foreach (var file in files)
        {
            OpenFile(file.Path.AbsolutePath, 1, 1);
        }
    }

    public void OpenFile(string path, int line, int column)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.EditorViewModel.GetOpenedOrCreateFile(path, false, line, column);
    }


    private async void ThemeImportMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open File",
            AllowMultiple = false,
        });

        if (files.Count < 1)
        {
            return;
        }

        foreach (var file in files)
        {
            var theme = mainWindowViewModel.EditorViewModel.FromBase(BlitzTheme.Dark, ThemeName.DarkPlus);
            theme.ThemeName = file.Path.AbsolutePath;
            try
            {
                var themeViewModel = new ThemeViewModel(theme);
                mainWindowViewModel.EditorViewModel.ThemeViewModel = themeViewModel;
                FileView.ReApplyTheme();
            }
            catch (Exception exception)
            {
                //Need a box for the message,  https://github.com/Natestah/BlitzSearch/issues/85
                Console.WriteLine(exception);
                return;
            }
        }
    }

    private void ThemeBuiltInMenuItem_OnClick(object? sender, RoutedEventArgs e)
    { 
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is MenuItem item)
        {
            var themeName = item.CommandParameter as string;
            
            var toSelect = mainWindowViewModel.EditorViewModel.AllThemeViewModels.FirstOrDefault(a=>a.ThemeName.ToString() == themeName);

            if (toSelect != null)
            {
                mainWindowViewModel.EditorViewModel.ThemeViewModel = toSelect;
                FileView.ReApplyTheme();
            }
            
        }
    }
}

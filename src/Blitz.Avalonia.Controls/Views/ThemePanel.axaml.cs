using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Blitz.AvaloniaEdit.Models;
using Blitz.AvaloniaEdit.ViewModels;
using MainWindowViewModel = Blitz.Avalonia.Controls.ViewModels.MainWindowViewModel;

namespace Blitz.Avalonia.Controls.Views;

public partial class ThemePanel : UserControl
{
    public ThemePanel()
    {
        InitializeComponent();
    }

    private async void ImportTheme_OnClick(object? sender, RoutedEventArgs e)
    {
        var baseTheme = sender is Button { CommandParameter: "Light" } ? BlitzTheme.Light : BlitzTheme.Dark;
        
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
            var theme = mainWindowViewModel.EditorViewModel.FromBase(baseTheme, file.Path.AbsolutePath);
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
        }
    }
}
using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit.TextMate;
using Blitz.Avalonia.Controls.ViewModels;
using Blitz.Search;
using Blitz.Views;

namespace Blitz;

public partial class App : Application
{
    public override void Initialize()
    {
        Interfacing.MessagePackSetup.Init();
        AvaloniaXamlLoader.Load(this);
    }

    
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
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowViewModel = new MainWindowViewModel(new InProcessSearchHandler());

            var mainWindow = desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel,
            };
            mainWindowViewModel.EditorViewModel.BackGroundForeGroundUpdate = (textMateInstallation) =>
            {
                ApplyBrushAction(textMateInstallation, "editor.background",brush => mainWindow.Background = brush);
                if (!ApplyBrushAction(textMateInstallation,"editor.foreground", brush => mainWindow.Foreground = brush))
                {
                    mainWindow.Foreground = null;
                }
            };
            
            mainWindowViewModel.PropertyChanged +=MainWindowViewModelOnPropertyChanged;
            var trayIcon = new TrayIcon();
            trayIcon.Icon = desktop.MainWindow.Icon;
            trayIcon.Clicked += (_, _) =>
            {
                if (mainWindow.WindowState == WindowState.Minimized)
                {
                    mainWindow.WindowState = WindowState.Normal;
                }
                mainWindow.BringIntoView();
                mainWindow.Activate();
            };

            mainWindow.ShowInTaskbar = Configuration.Instance.ShowOnTaskBar;

            mainWindowViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.ShowOnTaskBar))
                {
                    mainWindow.ShowInTaskbar = mainWindowViewModel.ShowOnTaskBar;
                }
            };
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void MainWindowViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not MainWindowViewModel mainWindowViewModel) return;
        if (e.PropertyName == nameof(MainWindowViewModel.BlitzThemeViewModel))
        {
            RequestedThemeVariant = mainWindowViewModel.BlitzThemeViewModel != null && mainWindowViewModel.BlitzThemeViewModel.ThemeName.ToString().ToLower().Contains("light")
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
        }
    }

    private void Application_OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel mainWindowViewModel)
        {
            mainWindowViewModel.UpdateTheme();
        }
    }
}
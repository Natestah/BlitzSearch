using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using AvaloniaEdit.TextMate;
using Blitz.AvaloniaEdit.ViewModels;
using Blitz.Search;
using Blitz.Views;
using MainWindowViewModel = Blitz.Avalonia.Controls.ViewModels.MainWindowViewModel;

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
    
    MainWindowViewModel? _mainWindowViewModel;
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mainWindowViewModel = new MainWindowViewModel(new InProcessSearchHandler());

            var mainWindow = desktop.MainWindow = new MainWindow
            {
                DataContext = _mainWindowViewModel,
            };
            _mainWindowViewModel.EditorViewModel.BackGroundForeGroundUpdate = (textMateInstallation) =>
            {
                ApplyBrushAction(textMateInstallation, "editor.background",brush => mainWindow.Background = brush);
                if (!ApplyBrushAction(textMateInstallation,"editor.foreground", brush => mainWindow.Foreground = brush))
                {
                    mainWindow.Foreground = null;
                }
            };
            
            _mainWindowViewModel.EditorViewModel.PropertyChanged +=MainWindowViewModelOnPropertyChanged;
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

            _mainWindowViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.ShowOnTaskBar))
                {
                    mainWindow.ShowInTaskbar = _mainWindowViewModel.ShowOnTaskBar;
                }
            };
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void MainWindowViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not BlitzEditorViewModel blitzEditorViewModel) return;
        if (e.PropertyName == nameof(BlitzEditorViewModel.ThemeViewModel))
        {
            RequestedThemeVariant = blitzEditorViewModel.ThemeViewModel is { Theme.AvaloniaThemeVariant: "Light" }
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
        }
    }

    private void Application_OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        _mainWindowViewModel?.UpdateTheme();
    }
}
using System.ComponentModel;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Blitz.Goto.Demo.ViewModels;
using Blitz.Goto.Demo.Views;

namespace Blitz.Goto.Demo;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindowVM = new MainWindowViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowVM,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
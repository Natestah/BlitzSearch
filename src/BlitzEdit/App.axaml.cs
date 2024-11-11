using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using BlitzEdit.ViewModels;
using BlitzEdit.Views;

namespace BlitzEdit;

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
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            BLitzEditIPC.Instance.RegisterAction("BLITZ_EDIT_GOTO", Goto);
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private async void Goto(string gotoCommand)
    {
        var splitString = gotoCommand.Split(';');
        if (splitString.Length != 3)
        {
            return;
        }

        string file = splitString[0];
        string lineStr = splitString[1];
        string columnStr = splitString[2];

        if (!int.TryParse(lineStr, out var line))
        {
            line = 1;
        }

        if (!int.TryParse(columnStr, out var column))
        {
            column = 1;
        }

        if (!File.Exists(file))
        {
            return;
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is MainWindow mainWindow)
        {
            Dispatcher.UIThread.Post(() =>
            {
                mainWindow.OpenFile(file,line,column);
            });
        }

    }
}
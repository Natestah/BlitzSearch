using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Blitz.Avalonia.Controls.ViewModels;
using Blitz.Interfacing;
using Humanizer;

namespace Blitz.Avalonia.Controls.Views;

public partial class SettingsPanel : UserControl
{
    public SettingsPanel()
    {
        InitializeComponent();
        Loaded += (sender, args) =>
        {
            _ = CalculateCacheUsage();
        };

    }

    private void CollectGarbage_OnClick(object? _, RoutedEventArgs e)
    {
        GC.Collect();
    }

    private async void CacheClean_OnClick(object? _, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
            return;
        mainWindowViewModel.CacheStatus = "Cleaning...";
        mainWindowViewModel.CacheCleaning = true;

        int count = 0;
        await Task.Run(() =>
        {
            foreach (var file in Directory.EnumerateFiles(SearchExtensionCache.CacheFolder))
            {
                try
                {
                    File.Delete(file);
                    count++;
                    Dispatcher.UIThread.Post(() => { mainWindowViewModel.CacheStatus = $"Cleaned '{count}' Files"; });
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() => { mainWindowViewModel.CacheStatus = $"failed {ex.Message}"; });
                }
            }
        });
        await CalculateCacheUsage();
        mainWindowViewModel.CacheCleaning = false;
    }

    private async Task CalculateCacheUsage()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
            return;

        long fileSizeBytes = 0;
        mainWindowViewModel.CacheStatus = $"Cache Size '{fileSizeBytes.Bytes().Humanize()}'";
        await Task.Run(() =>
        {
            if (!Directory.Exists(SearchExtensionCache.CacheFolder))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(SearchExtensionCache.CacheFolder))
            {
                try
                {
                    fileSizeBytes += new FileInfo(file).Length;
                    Dispatcher.UIThread.Post(() =>
                    {
                        mainWindowViewModel.CacheStatus = $"Cache Size '{fileSizeBytes.Bytes().Humanize()}'";
                    });
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        });
    }
}
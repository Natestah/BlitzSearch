using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Blitz.Avalonia.Controls.ViewModels;

namespace Blitz.Avalonia.Controls.Views;

public partial class BlitzStatusBar : UserControl
{
    public BlitzStatusBar()
    {
        InitializeComponent();
    }
    public Action? InstallerClick;

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void NewVersionButton_OnClick(object? o, RoutedEventArgs e)
    {
        InstallerClick?.Invoke();
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
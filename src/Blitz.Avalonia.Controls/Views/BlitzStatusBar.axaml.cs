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

    private void DonateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var processStartInfo = new ProcessStartInfo("https://pay.natestah.com")
        {
            UseShellExecute = true
        };
        Process.Start(processStartInfo); // todo landing page for new version.
    }
}
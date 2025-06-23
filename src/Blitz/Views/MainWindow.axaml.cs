using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Blitz.Avalonia.Controls.ViewModels;
using ReactiveUI;

namespace Blitz.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        if (Configuration.Instance.FromFile)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
        }

        InitializeComponent();
        Closed += OnClosed;
        PropertyChanged += OnPropertyChanged;
        BlitzMainPanel.SetRestartAction(DoUpdateAndRestart);
    }

    public void DoUpdateAndRestart()
    {
        if (!string.IsNullOrEmpty(_detectedVersion))
        {
            string exectubalePath = GetInstallerFileNameFromVersion(_detectedVersion);
            string fullPath = GetLocalInstallerPathFromExeName(exectubalePath);
            Process.Start(fullPath, "/VERYSILENT");
            Close();
            return;
        }

        var processStartInfo = new ProcessStartInfo("https://natestah.com/download")
        {
            UseShellExecute = true
        };
        Process.Start(processStartInfo);

    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case nameof(WindowState):
                if (this.WindowState != WindowState.Minimized)
                {
                    if (DataContext is not MainWindowViewModel vm)
                    {
                        return;
                    }

                    vm.LastNonMinizedState = this.WindowState;

                }

                break;
        }
    }

    private string _detectedVersion = "0.0.0.0";

    List<BlitzVersion> ParseLocalChangeLog()
    {
        var uri = new Uri(Environment.ExpandEnvironmentVariables($"%programfiles%\\blitz\\Documentation\\change_log.md"));
        if (!File.Exists(uri.LocalPath))
        {
            return [];
        }
        using var re = new StreamReader(uri.LocalPath);
        {
            return ParseChangeLog(re);
        }
    }

    List<BlitzVersion> ParseChangeLog( StreamReader re )
    {
        var versionMatch = new Regex(@"Version (\d*\.\d*\.\d*)", RegexOptions.Compiled);
        var versionBuilder = new Dictionary<string, StringBuilder>();
        string? currentVersion = null;
        while (re.Peek() != -1)
        {
            var line = re.ReadLine()!;
            var match = versionMatch.Match(line);
            if (match.Success)
            {
                currentVersion = match.Groups[1].ToString();
                versionBuilder[currentVersion] = new StringBuilder();
                continue;
            }

            if (currentVersion != null)
            {
                versionBuilder[currentVersion].AppendLine(line);
            }
        }
        var versionList = new List<BlitzVersion>();
        foreach (var kvp in versionBuilder)
        {
            versionList.Add(new BlitzVersion{ Revision = kvp.Key,Changes = kvp.Value.ToString()});
        }

        return versionList;
    }
    
    private const string ChangeLogURL = "https://raw.githubusercontent.com/Natestah/BlitzSearch/refs/heads/main/src/Blitz/Documentation/Change_Log.md";
    private async Task CheckUpdate()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        using var client = new HttpClient();
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true, MustRevalidate = true};
        try
        {
            await using var s = await client.GetStreamAsync(ChangeLogURL);
            using var sr = new StreamReader(s);
            {
                var versions = ParseChangeLog(sr);
                if (versions.Count == 0)
                {
                    vm.NewVersionAvailable = false;
                    return;
                }
                _detectedVersion = versions.First().Revision; 
                var fromChanges = versions.First().Revision + ".0"; // Blitz doesn't use this.. 

                if (fromChanges != vm.Version)
                {
                    var whatsNew = new StringBuilder();
                    foreach (var change in versions)
                    {
                        if (vm.Version.StartsWith(change.Revision))
                        {
                            break;
                        }

                        whatsNew.AppendLine($"Version {change.Revision}");
                        whatsNew.AppendLine(change.Changes);
                    }

                    var downloaded = await DownloadVersion(client, _detectedVersion);
                    if (downloaded)
                    {                    
                        vm.NewVersionAvailable = true;
                        vm.WhatsInUpdate = whatsNew.ToString();
                        vm.NewVersionString = _detectedVersion;
                    }
                }
            }
        }
        catch (Exception)
        {
            vm.NewVersionAvailable = false;
        }
    }
    
    private string GetInstallerFileNameFromVersion(string version) => $"SetupBlitz_win-x64_{version}.exe";
    private string GetInstallerUriFromExe(string version, string executableName) =>  $"https://github.com/Natestah/BlitzSearch/releases/download/v{version}/{executableName}";

    private string GetLocalInstallerPathFromExeName(string exeName)
    {
        var appdata = Environment.ExpandEnvironmentVariables("%appdata%");
        string path = Path.Combine(appdata, "NathanSilvers", "Installers");
        Directory.CreateDirectory(path);
        return Path.Combine(path, exeName);
    }

    private async Task<bool> DownloadVersion(HttpClient client, string version)
    {
        try
        {
            string executableName = GetInstallerFileNameFromVersion(version);
            string url = GetInstallerUriFromExe(version,executableName);
            string destination = GetLocalInstallerPathFromExeName(executableName);
            if (File.Exists(destination))
            {
                return true;
            }
            try
            {
                foreach (var installerFile in Directory.EnumerateFiles(Path.GetDirectoryName(destination)!))
                {
                    File.Delete(installerFile);
                }
            }
            catch (Exception)
            {
            }

            byte[] fileBytes = await client.GetByteArrayAsync(url);
            File.WriteAllBytes(destination, fileBytes);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async void HandleUpdateAvailability()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (Debugger.IsAttached) return;

        while (true)
        {
            await CheckUpdate();
            await Task.Delay(TimeSpan.FromMinutes(30));
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Configuration.Instance.WindowState = this.WindowState;
        Configuration.Instance.SaveConfig();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (Configuration.Instance.FromFile)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;

            Width = Configuration.Instance.BlitzPosWidth;
            Height = Configuration.Instance.BlitzPosHeight;

            if ((int)Configuration.Instance.BlitzPosX != -32000)
            {
                Position =
                    new PixelPoint((int)Configuration.Instance.BlitzPosX, (int)Configuration.Instance.BlitzPosY);
                
            }
            WindowState = Configuration.Instance.WindowState;
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
        }

        this.PropertyChanged+=WindowOnPropertyChanged;
        SizeChanged += OnSizeChanged;
        PositionChanged += OnPositionChanged;
        HandleUpdateAvailability();
        if (DataContext is MainWindowViewModel mainWindowViewModel)
        {
            mainWindowViewModel.PropertyChanged+=MainWindowViewModelOnPropertyChanged;
            mainWindowViewModel.RaisePropertyChanged("SearchTextBox");
        }

        SetBoarderChromeForMaximizedState();
        FancyTitleBarSizeChanged();
    }

    private void SetBoarderChromeForMaximizedState()
    {
        if (this.WindowState == WindowState.Maximized || this.WindowState == WindowState.FullScreen)
        {
            MaximizeBorder.BorderThickness = new Thickness(8);
        }
        else
        {
            MaximizeBorder.BorderThickness = new Thickness(0);
        }

    }

    private void WindowOnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(WindowState))
        {
            SetBoarderChromeForMaximizedState();
        }
    }

    private void MainWindowViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.FontSize))
        {
            this.MinWidth = 0;
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        Configuration.Instance.WindowState = this.WindowState;
        if (e.Point.X != 32000)
        {
            Configuration.Instance.BlitzPosX = e.Point.X;
            Configuration.Instance.BlitzPosY = e.Point.Y;
        }
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Configuration.Instance.WindowState = this.WindowState;
        if (WindowState != WindowState.Normal)
        {
            return;
        }
        if (!FancyTitleBarSizeChanged())
        {
            return;
        }
        Configuration.Instance.BlitzPosWidth = e.NewSize.Width;
        Configuration.Instance.BlitzPosHeight = e.NewSize.Height;
    }

    private bool FancyTitleBarSizeChanged()
    {
        var completeWidth = TitlePanel.Bounds.Width + MaximizeBorder.BorderThickness.Left +MaximizeBorder.BorderThickness.Right;
        var spaceForBothBlitzSearchLabels = completeWidth - (BlitzLogo.Bounds.Width + ButtonPanel.Bounds.Width + WindowsPanel.Bounds.Width);
        bool blitzShouldBeVisible = BlitzLabelBlitz.IsVisible = spaceForBothBlitzSearchLabels > BlitzLabelBlitz.Bounds.Width;
        bool blitzSearchShouldBeVisible = spaceForBothBlitzSearchLabels >
                                          (BlitzLabelBlitz.Bounds.Width + BlitzLabelSearch.Bounds.Width);
        bool blitzScopeNameShouldBeVisible = spaceForBothBlitzSearchLabels >
                                          (BlitzLabelBlitz.Bounds.Width + BlitzLabelSearch.Bounds.Width);
        if (!blitzShouldBeVisible)
        {
            this.MinWidth = Configuration.Instance.BlitzPosWidth;
            return false;
        }
        
        BlitzLabelSearch.IsVisible = blitzSearchShouldBeVisible;
        BlitzLabelBlitz.IsVisible = blitzShouldBeVisible;

        return true;
    }

    private void Button_MinimizeWindow(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Button_MaximizeWindow(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
    }

    private void Button_CloseWindow(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (sender is ToggleButton tb && tb.IsChecked == false)
        {
            mainWindowViewModel.SplitPane = false;
        }
        else
        {
            mainWindowViewModel.SplitPane = true;
        }
    }
}
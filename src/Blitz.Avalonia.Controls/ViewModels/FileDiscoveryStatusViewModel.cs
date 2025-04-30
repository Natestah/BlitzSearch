using System;
using System.ComponentModel;
using Avalonia.Media;
using Blitz.Interfacing;
using Humanizer;
using ReactiveUI;
namespace Blitz.Avalonia.Controls.ViewModels;

public class FileDiscoveryStatusViewModel : ViewModelBase
{

    public MainWindowViewModel MainWindowViewModel { get; }

    private bool _isIndeterminate;
    private bool _working;
    private bool _isDiscovering;
    private int _filesProcessed;
    private int _filesDiscoveredCount;
    private int _runningTimeMs;
    private int _lastResultTimeMs;
    private double _progressOpacity;

    public FileDiscoveryStatusViewModel(MainWindowViewModel mainWindowViewModel)
    {
        MainWindowViewModel = mainWindowViewModel;
        mainWindowViewModel.PropertyChanged+=MainWindowViewModelOnPropertyChanged;
        mainWindowViewModel.EditorViewModel.PropertyChanged+=EditorViewModelOnPropertyChanged;
    }

    private void EditorViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.EditorViewModel.StatusBarForeground))
        {
            this.RaisePropertyChanged(nameof(StatusBarForeground));
        }
        
        if (e.PropertyName == nameof(MainWindowViewModel.EditorViewModel.StatusBarBackground))
        {
            this.RaisePropertyChanged(nameof(StatusBarBackground));
        }
    }
    
    private void MainWindowViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
       if (e.PropertyName == nameof(MainWindowViewModel.TimerDisplayTotalSearchTIme))
       {
           this.RaisePropertyChanged(nameof(TimerDisplayTotalSearchTIme));
       }
    }
    public bool TimerDisplayTotalSearchTIme => Configuration.Instance.ShowTotalSearchTime;
    public IBrush? StatusBarForeground => MainWindowViewModel.EditorViewModel.StatusBarForeground;
    public IBrush? StatusBarBackground => MainWindowViewModel.EditorViewModel.StatusBarBackground;
    public bool IsIndeterminate => _isIndeterminate;

    public bool Working => _working;

    public bool IsDiscovering => _isDiscovering;

    public int FilesDiscoveredCount => _filesDiscoveredCount;
    public string FilesDiscoveredHumanized => FormatLargerNumbers(_filesDiscoveredCount);
    public static string FormatLargerNumbers(int number)
    {
        if (number == 0)
        {
            return "0";
        }
        string[] prefix = { string.Empty, "K", "M", "B" };

        var absnum = Math.Abs((double)number);

        double add;
        if (absnum < 1)
        {
            add = (int)Math.Floor(Math.Floor(Math.Log10(absnum)) / 3);
        }
        else
        {
            add = (int)(Math.Floor(Math.Log10(absnum)) / 3);
        }

        var shortNumber = number / Math.Pow(10, add * 3);

        return string.Format("{0}{1}", shortNumber.ToString("0.#"), 
            (prefix[Convert.ToInt32(add)]));
    }
    public int FilesProcessedCount => _filesProcessed;

    public string RunningTimeHumanized => TimeSpan.FromMilliseconds(_runningTimeMs).Humanize(2);
    public string LastResultTimeHumanized => TimeSpan.FromMilliseconds(_lastResultTimeMs).Humanize(2);

    public double ProgressOpacity
    {
        get => _progressOpacity;
        set => this.RaiseAndSetIfChanged(ref _progressOpacity, value);
    }

    /// <summary>
    /// Given a FileDiscoveryStatus, update the view
    /// </summary>
    /// <param name="currentStatus"></param>
    public void UpdateStatus(FileSearchStatus currentStatus)
    {
        this.RaiseAndSetIfChanged(ref _isIndeterminate, currentStatus.Discovering, nameof(IsIndeterminate));
        this.RaiseAndSetIfChanged(ref _working, currentStatus.Working, nameof(Working));
        this.RaiseAndSetIfChanged(ref _isDiscovering, currentStatus.Discovering, nameof(IsDiscovering));
        this.RaiseAndSetIfChanged(ref _filesProcessed, currentStatus.FilesProcessed, nameof(FilesProcessedCount));
        if (_filesDiscoveredCount != currentStatus.DiscoveredCount)
        {
            _filesDiscoveredCount = currentStatus.DiscoveredCount;
            this.RaisePropertyChanged(nameof(FilesDiscoveredHumanized));
            this.RaisePropertyChanged(nameof(FilesDiscoveredCount));
        }
        this.RaiseAndSetIfChanged(ref _runningTimeMs, (int)currentStatus.RunningTime.TotalMilliseconds, nameof(RunningTimeHumanized));
        this.RaiseAndSetIfChanged(ref _lastResultTimeMs, (int)currentStatus.LastResultTime.TotalMilliseconds, nameof(LastResultTimeHumanized));
    }
}
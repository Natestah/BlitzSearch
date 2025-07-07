using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Mime;
using Blitz.Interfacing;
using ReactiveUI;

namespace Blitz.Avalonia.Controls.ViewModels;

public class ReplaceTextViewModel : ViewModelBase
{
    private int _count;
    public string TextSummary { get; }
    
    public bool PerforceReplaced { get; set; }

    public int Count
    {
        get => _count;
        set => this.RaiseAndSetIfChanged(ref _count, value);
    }

    public int Total { get; }

    public ObservableCollection<ReplaceFailureReport> ReplaceFileNameResultFailures { get; set; } = [];

    public ReplaceTextViewModel(string textSummary,int count, int total)
    {
        TextSummary = textSummary;
        Count = count;
        Total = total;
    }
}

public class ReplaceFailureReport
{
    public FileNameResult FilenameResult { get; set; }
    public ExceptionViewModel ExceptionViewModel { get; set; }
}
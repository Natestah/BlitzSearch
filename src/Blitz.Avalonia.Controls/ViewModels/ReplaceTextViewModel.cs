using System.Net.Mime;

namespace Blitz.Avalonia.Controls.ViewModels;

public class ReplaceTextViewModel : ViewModelBase
{
    public string TextSummary { get; }
    public int Count { get; }
    public int Total { get; }

    public ReplaceTextViewModel(string textSummary,int count, int total)
    {
        TextSummary = textSummary;
        Count = count;
        Total = total;
    }
    
}
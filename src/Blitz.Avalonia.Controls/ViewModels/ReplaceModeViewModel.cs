using Material.Icons;
using Material.Icons.Avalonia;

namespace Blitz.Avalonia.Controls.ViewModels;

public class ReplaceModeViewModel(MainWindowViewModel mainWindowViewModel, MaterialIconKind iconKind, string title, string hint) : ViewModelBase
{
    public MainWindowViewModel MainWindowViewModel => mainWindowViewModel;
    public MaterialIconKind IconKind { get; } = iconKind;
    public string Title { get; } = title;
    public string Hint { get; } = hint;
}
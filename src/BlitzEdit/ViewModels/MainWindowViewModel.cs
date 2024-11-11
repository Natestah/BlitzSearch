using Blitz.AvaloniaEdit.ViewModels;

namespace BlitzEdit.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public BlitzEditorViewModel EditorViewModel { get; set; } = new();
}
using System.Collections.ObjectModel;

namespace Blitz.Goto.Demo.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ObservableCollection<GotoEditorViewModel> GotoEditorCollection { get; } = [];

    public GotoEditorViewModel ManualEditorEntry { get; } = new(new GotoEditor());
}
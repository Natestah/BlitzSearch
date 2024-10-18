using ReactiveUI;

namespace Blitz.Avalonia.Controls.ViewModels;

public class ViewModelBase : ReactiveObject
{
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }
}
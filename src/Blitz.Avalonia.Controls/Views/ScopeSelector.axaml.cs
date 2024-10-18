using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Blitz.Avalonia.Controls.ViewModels;

namespace Blitz.Avalonia.Controls.Views;

public partial class ScopeSelector : UserControl
{
    public ScopeSelector()
    {
        InitializeComponent();
    }
    
    private void ComboboxItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return; 
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is ScopeViewModel scopeViewModel)
        {
            mainWindowViewModel.WorkingScope = scopeViewModel;
        }
    }
}
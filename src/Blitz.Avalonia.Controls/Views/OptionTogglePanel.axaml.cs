using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Blitz.Avalonia.Controls.ViewModels;

namespace Blitz.Avalonia.Controls.Views;

public partial class OptionTogglePanel : UserControl
{
    public OptionTogglePanel()
    {
        InitializeComponent();
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
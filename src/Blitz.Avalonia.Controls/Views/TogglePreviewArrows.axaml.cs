using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Blitz.Avalonia.Controls.ViewModels;

namespace Blitz.Avalonia.Controls.Views;

public partial class TogglePreviewArrows : UserControl
{
    public TogglePreviewArrows()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        if (mainWindowViewModel.SplitPane)
        {
            mainWindowViewModel.EnableTextPane = true;
            mainWindowViewModel.ShowPaneIfSelection();
        }
    }
}
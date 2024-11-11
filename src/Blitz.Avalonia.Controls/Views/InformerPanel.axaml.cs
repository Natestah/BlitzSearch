using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Blitz.Avalonia.Controls.ViewModels;

namespace Blitz.Avalonia.Controls.Views;

public partial class InformerPanel : UserControl
{
    public InformerPanel()
    {
        InitializeComponent();
    }

    private void SmartCaseOffButton(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.SearchTextBox =
            mainWindowViewModel.SearchTextBox.ToLower();
    }

    private void SmartCaseLiteralOffButton(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.LiteralSearchTextBox = mainWindowViewModel.LiteralSearchTextBox.ToLower();
    }

    private void SmartCaseRegexOffButton(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.RegexSearchTextBox = mainWindowViewModel.RegexSearchTextBox.ToLower();
    }

    private void SmartCaseReplaceOffButton(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.ReplaceBoxText = mainWindowViewModel.ReplaceBoxText.ToLower();
    }

}
using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Blitz.Avalonia.Controls.ViewModels;

namespace Blitz.Avalonia.Controls.Views;

public partial class RegularExpressionPanel : UserControl
{
    public RegularExpressionPanel()
    {
        InitializeComponent();
    }

    private void RegexSearchControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not string itemName) return;
        mainWindowViewModel.RegexSearchTextBox = itemName;
    }

    private void RegexSearchCloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }
        mainWindowViewModel.RegexSearchEnabled = false;
    }
    
    private void RegexSearchTextBox_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            throw new Exception("NoTextBox");
        }

        if (DataContext is MainWindowViewModel mainWindowViewModel)
            mainWindowViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.RegexSearchEnabled) &&
                    mainWindowViewModel.RegexSearchEnabled)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            };
    }

    private void RegexSearchField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.UpdateHistoryForColllection(mainWindowViewModel.RegexSearchTextHistory, mainWindowViewModel.RegexSearchTextBox);
    }

}
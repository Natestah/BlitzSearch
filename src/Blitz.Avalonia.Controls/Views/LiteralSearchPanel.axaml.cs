using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Blitz.Avalonia.Controls.ViewModels;

namespace Blitz.Avalonia.Controls.Views;

public partial class LiteralSearchPanel : UserControl
{
    public LiteralSearchPanel()
    {
        InitializeComponent();
    }
    
    
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void LiteralSearchControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not string itemName) return;
        mainWindowViewModel.LiteralSearchTextBox = itemName;
    }
    
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void LiteralSearchCloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.LiteralSearchEnabled = false;
    }

    private void LiterSearchTextBox_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            throw new Exception("NoTextBox");
        }

        if (DataContext is MainWindowViewModel mainWindowViewModel)
            mainWindowViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.LiteralSearchEnabled) &&
                    mainWindowViewModel.LiteralSearchEnabled)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            };
    }
    
    
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void LiteralSearchField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }
        mainWindowViewModel.UpdateHistoryForColllection(mainWindowViewModel.LiteralSearchTextHistory,
            mainWindowViewModel.LiteralSearchTextBox);
    }
}
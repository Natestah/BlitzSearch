using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Blitz.Avalonia.Controls.ViewModels;

namespace Blitz.Avalonia.Controls.Views;

public partial class SearchPanel : UserControl
{
    public Action<object,KeyEventArgs>? KeyDownAction { get; set; }
    
    public SearchPanel()
    {
        InitializeComponent();
    }
    
    private void MainSearchField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.UpdateHistoryForColllection(mainWindowViewModel.SearchTextHistory, mainWindowViewModel.SearchTextBox);
    }

    private void MainSearchField_OnKeyDown(object? sender, KeyEventArgs e)
    {
        KeyDownAction?.Invoke(sender!, e);
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not string itemName) return;
        mainWindowViewModel.SearchTextBox = itemName;
    }
}
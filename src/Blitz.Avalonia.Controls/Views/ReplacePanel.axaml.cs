using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Blitz.Avalonia.Controls.ViewModels;

namespace Blitz.Avalonia.Controls.Views;

public partial class ReplacePanel : UserControl
{
    public ReplacePanel()
    {
        InitializeComponent();
    }
    
    private void ReplaceWithField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.UpdateHistoryForColllection(mainWindowViewModel.ReplaceWithHistory, mainWindowViewModel.ReplaceWithBoxText);
    }

    private void ReplaceHistoryItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not string itemName) return;
        mainWindowViewModel.ReplaceBoxText = itemName;
    }

    private void ReplaceWithHistoryItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not string itemName) return;
        mainWindowViewModel.ReplaceWithBoxText = itemName;
    }

    private void ReplaceField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.UpdateHistoryForColllection(mainWindowViewModel.SearchFileHistory, mainWindowViewModel.ReplaceBoxText);
    }

    private void ReplaceWithBox_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            throw new Exception("NoTextBox");
        }

        if (DataContext is MainWindowViewModel mainWindowViewModel)
            mainWindowViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.ReplaceInFileEnabled) &&
                    mainWindowViewModel.ReplaceInFileEnabled)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            };
    }
    
    private void CloseReplaceBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.ReplaceInFileEnabled = false;
    }


}
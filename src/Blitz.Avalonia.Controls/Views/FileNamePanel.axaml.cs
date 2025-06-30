using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Blitz.Avalonia.Controls.ViewModels;

namespace Blitz.Avalonia.Controls.Views;

public partial class FileNamePanel : UserControl
{
    public FileNamePanel()
    {
        InitializeComponent();
    }

    private void SelectingFileItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel || e.AddedItems.Count == 0 ||
            e.AddedItems[0] is not string itemName) return;
        mainWindowViewModel.FileNameSearchTextBox = itemName;
    }

    private void FileName_Close_Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        mainWindowViewModel.FileNameDebugQueryEnabled = false;
    }

    private void FileNameFilterBox_OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            throw new Exception("NoTextBox");
        }

        if (DataContext is MainWindowViewModel mainWindowViewModel)
            mainWindowViewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.FileNameDebugQueryEnabled) &&
                    mainWindowViewModel.FileNameDebugQueryEnabled)
                {
                    textBox.Focus();
                    textBox.SelectAll();
                }
            };
    }
    
    private void FileNameSearchField_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.UpdateHistoryForColllection(mainWindowViewModel.SearchFileHistory, mainWindowViewModel.FileNameSearchTextBox);
    }


}
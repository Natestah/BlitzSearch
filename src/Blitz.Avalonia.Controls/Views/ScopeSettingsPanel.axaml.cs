using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Blitz.Avalonia.Controls.ViewModels;
using DynamicData;

namespace Blitz.Avalonia.Controls.Views;

public partial class ScopeSettingsPanel : UserControl
{
    public ScopeSettingsPanel()
    {
        InitializeComponent();
        AddHandler(DragDrop.DragOverEvent, DragOverHandler);
        AddHandler(DragDrop.DropEvent, DropHandler);
    }

    private void DropHandler(object? sender, DragEventArgs e)
    {
        if (DataContext is not ScopeViewModel scopeSettingsVm) return;

        var file = e.Data?.GetFiles()?.FirstOrDefault()?.Path;
        if(file is null)
            return;
        scopeSettingsVm.ScopeImage = file.LocalPath;
    }

    private void DragOverHandler(object? sender, DragEventArgs e)
    {
        e.DragEffects = DragDropEffects.Link;
    }

    private async void OpenFolder_Button_OnClick(object? sender, RoutedEventArgs _)
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(this);

        if (topLevel == null)
        {
            return;
        }

        // Start async operation to open the dialog.
        var files = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Open File Folder",
            AllowMultiple = false,
        });

        if (files.Count < 1)
        {
            return;
        }

        if ((sender as Control)?.DataContext is SearchPathViewModel vm)
        {
            vm.SearchPath = files[0].Path.IsAbsoluteUri ? files[0].Path.LocalPath : files[0].Path.ToString();
        }
    }

    private void SearchPathViewmodelHistoryFolder_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lb) return;
        if (lb.DataContext is not SearchPathViewModel searchPathViewModel) return;
        if (e.AddedItems.Count == 0 || e.AddedItems[0] is not string itemName) return;
        searchPathViewModel.SearchPath = itemName;
    }

    private const int MaxHistoryEntries = 15;

    private void SearchPath_OnLostFocus(object? sender, RoutedEventArgs e)
    {
        if ((sender as AutoCompleteBox)?.DataContext is not SearchPathViewModel searchPathViewModel) return;
        string text = searchPathViewModel.SearchPath!;
        if (!Directory.Exists(text))
        {
            return;
        }

        var historyItems = searchPathViewModel.ParentModel.PathFolderHistory;
        var first = historyItems.FirstOrDefault();
        if (text == first || string.IsNullOrEmpty(text)) return;
        historyItems.Remove(text);
        historyItems.Insert(0, text);
        while (historyItems.Count > MaxHistoryEntries)
        {
            historyItems.RemoveAt(historyItems.Count - 1);
        }
    }

    async Task OpenBitmapPicker()
    {
        if (DataContext is not ScopeViewModel scopeViewModel) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
        {
            return;
        }
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open Bitmap",
            AllowMultiple = false,
        });

        if (files.Count < 1)
        {
            return;
        }

        scopeViewModel.ScopeImage = files[0].Path.LocalPath;
    }

    private async void DropBox_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        await OpenBitmapPicker();
    }

    private async void OpenBitmap_OnClick(object? sender, RoutedEventArgs e)
    {
        await OpenBitmapPicker();
    }

    private int _scopeIndex = 0;
    private void AddNewButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ScopeViewModel scopeViewModel) return;
        var scope = new ScopeConfig(){ScopeTitle = $"Untitled{++_scopeIndex}"};
        Configuration.Instance.ScopeConfigs.Add(scope);
        Configuration.Instance.SelectedScope = scope.ScopeTitle;
        scopeViewModel.MainWindowViewModel.BuildScopesViewModelsFromConfig();
    }

    private void DeleteButton_OnClick0(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ScopeViewModel scopeViewModel) return;
        scopeViewModel.RemoveMe();
        scopeViewModel.MainWindowViewModel.BuildScopesViewModelsFromConfig();
    }

    private void ClearScopeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ScopeViewModel scopeViewModel) return;
        scopeViewModel.ScopeImage = string.Empty;
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }
}
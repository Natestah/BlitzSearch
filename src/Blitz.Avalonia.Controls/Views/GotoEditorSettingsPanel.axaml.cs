using Avalonia.Controls;
using Avalonia.Interactivity;
using Blitz.Avalonia.Controls.ViewModels;

namespace Blitz.Avalonia.Controls.Views;

public partial class GotoEditorSettingsPanel : UserControl
{
    public GotoEditorSettingsPanel()
    {
        InitializeComponent();
    }
    

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is GotoEditorViewModel gotoEditorViewModel)
        {
            SetManualEditorTo((DataContext as MainWindowViewModel)!, gotoEditorViewModel);
        }
    }
    
    private static void SetManualEditorTo(MainWindowViewModel vm, GotoEditorViewModel selectedGotoEditor)
    {
        var manualEditor = vm.SelectedEditorViewModel;
        if (manualEditor == null)
        {
            return;
        }
        manualEditor.Title = selectedGotoEditor.Title;
        manualEditor.GotoEditor.CodeExecute = selectedGotoEditor.GotoEditor.CodeExecute;
        manualEditor.CommandLine = selectedGotoEditor.CommandLine;
        manualEditor.ExecutableWorkingDirectory = selectedGotoEditor.ExecutableWorkingDirectory;
        manualEditor.Executable = selectedGotoEditor.Executable;
        manualEditor.Notes = selectedGotoEditor.Notes;
    }

    private void SuggestedNames_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ArgumentAliasViewModel argumentAliasViewModel })
        {
            return;
        }
        var boxText = CommandLineBox.Text ?? string.Empty;
        var insertText = argumentAliasViewModel.Alias;
        var insertAt = CommandLineBox.CaretIndex;
        var replaceBoxText = boxText.Insert(insertAt, insertText);
        CommandLineBox.Text = replaceBoxText;
        CommandLineBox.CaretIndex = insertAt + insertText.Length;
    }
}
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Blitz.Avalonia.Controls.ViewModels;
using Blitz.Goto;

namespace Blitz.Avalonia.Controls.Views;

public partial class GotoEditorSettingsPanel : UserControl
{
    public GotoEditorSettingsPanel()
    {
        InitializeComponent();
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

    private void CreateCustomEditor_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel mainWindowViewModel)
        {
            var newEditor = new GotoEditorViewModel(mainWindowViewModel,new GotoEditor());
            mainWindowViewModel.GotoEditorCollection.Add(newEditor);
            mainWindowViewModel.SelectedEditorViewModel = newEditor;;
            mainWindowViewModel.RebuildCustomEditorList();
        }
    }
    private void DuplicateEditor_OnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel { SelectedEditorViewModel.GotoEditor: not null } mainWindowViewModel)
        {
            return;
        }
        var fileContents =  JsonSerializer.Serialize(mainWindowViewModel.SelectedEditorViewModel.GotoEditor, JsonContext.Default.Configuration);
        var copy = JsonSerializer.Deserialize<GotoEditor>(fileContents, JsonContext.Default.GotoEditor);
        if (copy == null)
        {
            return;
        }
        copy.Title = $"{copy.Title}_copy";
        var newEditor = new GotoEditorViewModel(mainWindowViewModel,copy);
        mainWindowViewModel.GotoEditorCollection.Add(newEditor);
        mainWindowViewModel.RebuildCustomEditorList();
    }
}
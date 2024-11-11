using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Blitz.Goto.Demo.ViewModels;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace Blitz.Goto.Demo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        InstallEditor();
        ApplyGotoFromJsonResource(vm);
        Console.WriteLine("test");
    }

    private static void SetManualEditorTo(MainWindowViewModel vm, GotoEditorViewModel selectedGotoEditor)
    {
        var manualEditor = vm.ManualEditorEntry;
        manualEditor.Title = selectedGotoEditor.Title;
        manualEditor.CommandLine = selectedGotoEditor.CommandLine;
        manualEditor.CodeExecute = selectedGotoEditor.CodeExecute;
        manualEditor.RunningProcessName = selectedGotoEditor.RunningProcessName;
        manualEditor.ExecutableWorkingDirectory = selectedGotoEditor.ExecutableWorkingDirectory;
        manualEditor.Executable = selectedGotoEditor.Executable;
        manualEditor.Notes = selectedGotoEditor.Notes;
    }

    private void ApplyGotoFromJsonResource(MainWindowViewModel vm)
    {
        vm.ManualEditorEntry.PropertyChanged += ManualEditorEntryOnPropertyChanged;

        foreach (var gotoEditor in new GotoDefinitions().GetBuiltInEditors())
        {
            vm.GotoEditorCollection.Add(new GotoEditorViewModel(gotoEditor));
        }
    }

    private void ManualEditorEntryOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel mv)
        {
            var currentObject = mv.ManualEditorEntry.GotoEditor;
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            var text = JsonSerializer.Serialize(currentObject, jsonOptions);
            var language = _registryOptions.GetLanguageByExtension(".json");
            if (language == null)
            {
                throw new NullReferenceException();
            }

            _textMateInstallation?.SetGrammar(_registryOptions.GetScopeByLanguageId(language.Id));
            AvaloniaTextEditor.Text = text;
        }
    }

    private bool _editorConfigured;
    readonly RegistryOptions _registryOptions = new(ThemeName.DarkPlus);
    private TextMate.Installation? _textMateInstallation;

    private void InstallEditor()
    {
        if (_editorConfigured)
        {
            return;
        }

        _textMateInstallation = AvaloniaTextEditor.InstallTextMate(_registryOptions);
        _editorConfigured = true;
        AvaloniaTextEditor.Options.HighlightCurrentLine = true;
    }

    private void TestGotoButton(object? sender, RoutedEventArgs e)
    {
        GotoCurrentManualGotoEditor();
    }
    private void TestGotoPreviewButton(object? sender, RoutedEventArgs e)
    {
        GotoCurrentManualGotoEditor(true);
    }

    private void GotoCurrentManualGotoEditor(bool preview = false)
    {
        // Define what you want to go to.  CreateAndTestFile will create a temp file path that you can test Goto Functionality with.
        new GotoTestFile().CreateAndTestFile(out var testFile,out var lineTest, out var testColumn );
                 
        var gotoDirective = new GotoDirective(testFile, lineTest, testColumn);

        if (DataContext is not MainWindowViewModel mv)
        {
            return;
        }
        var gotoAction = new GotoAction(mv.ManualEditorEntry.GotoEditor);
        try
        {
            gotoAction.ExecuteGoto(gotoDirective, preview);
        }
        catch (Exception exception)
        {
            AvaloniaTextEditor.Text = exception.ToString();
        }
    }
    

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is GotoEditorViewModel gotoEditorViewModel)
        {
            SetManualEditorTo((DataContext as MainWindowViewModel)!, gotoEditorViewModel);
        }
    }

    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs _)
    {
        GotoCurrentManualGotoEditor();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
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
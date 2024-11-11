using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using Blitz.AvaloniaEdit.ViewModels;
using DynamicData.Binding;
using ReactiveUI;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace Blitz.AvaloniaEdit.Views;

public partial class BlitzFileView : UserControl
{
    private CancellationTokenSource? _currentToken = null;
    private BlitzDocument? _currentDocument = null;

    public TextEditor TextEditor => AvaloniaTextEditor;
    
    public BlitzFileView()
    {
        InitializeComponent();
        Loaded+=OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        AvaloniaTextEditor.Options.HighlightCurrentLine = true;
        if (DataContext is not BlitzEditorViewModel editorViewModel)
        {
            return;
        }

        editorViewModel.PopulateThemeModels();
        editorViewModel.PropertyChanged+=EditorViewModelOnPropertyChanged;
        editorViewModel.SelectedFiles.CollectionChanged += (o, args) => UpdateViewToSelection();
        AvaloniaTextEditor.TextChanged+=AvaloniaTextEditorOnTextChanged;
    }

    public void SaveCurrentDocument()
    {
        if (_currentDocument is not { IsDirty: true, Type:BlitzDocument.DocumentType.File } ) return;

        try
        {
            File.WriteAllText(_currentDocument.FileNameOrTitle, AvaloniaTextEditor.Text);
            _currentDocument.IsDirty = false;
        }
        catch (Exception)
        {
            //Need a box for the message,  https://github.com/Natestah/BlitzSearch/issues/85
        }
    }

    private void AvaloniaTextEditorOnTextChanged(object? sender, EventArgs e)
    {
        if (_currentDocument != null)
        {
            _currentDocument.IsDirty = true;
        }
    }


    private async void UpdateViewToSelection()
    {
        if (DataContext is not BlitzEditorViewModel editorViewModel)
        {
            return;
        }
        if (editorViewModel.SelectedFiles.FirstOrDefault() is BlitzDocument blitzDocument)
        {
           await AddFileToView(blitzDocument);

           //Scroll to specific offset, instead of caret position line -> https://github.com/Natestah/BlitzSearch/issues/89
        
           await ScrollToPosition(blitzDocument.AlignViewLine, blitzDocument.AlignViewColumn);
        }
    }
    
    private void EditorViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(BlitzEditorViewModel.ThemeViewModel):
                ReApplyTheme();
                break;
            case nameof(BlitzEditorViewModel.SelectedFiles):
                UpdateViewToSelection();
                break;
        }
    }

    private async Task AddFileToView(BlitzDocument file)
    {
        if (DataContext is not BlitzEditorViewModel editorViewModel) return;
        

        //It's already the active in view.
        if (_currentDocument == file)
        {
            return;
        }
        
        if (_currentToken != null)
        {
            await _currentToken.CancelAsync();
        }
        _currentToken = new CancellationTokenSource();
        
        bool needsTextMateInstallation = !(_currentDocument is { Type: BlitzDocument.DocumentType.File } &&
                                           file.Type == BlitzDocument.DocumentType.File && file.Extension == _currentDocument.Extension);
        
        _currentDocument = file;
        string? filePreviewText = null;

        if (file.Type == BlitzDocument.DocumentType.File)
        {
            DateTime startTime = DateTime.Now;
            do
            {
                try
                {
                    filePreviewText = await File.ReadAllTextAsync(file.FileNameOrTitle, _currentToken.Token);

                }
                catch (IOException e)
                {
                    if (_currentToken.IsCancellationRequested) return;
                    await Task.Delay(50,_currentToken.Token);
                    continue;
                }

                if (_currentToken.IsCancellationRequested) return;
                break;
            } while (DateTime.Now - startTime < TimeSpan.FromSeconds(1));
            
        }
        else if (file.Type == BlitzDocument.DocumentType.Untitled)
        {
            filePreviewText = file.DirtyText;
        }
        else
        {
            filePreviewText = "";
        }

        AvaloniaTextEditor.Document = new TextDocument(filePreviewText) ;

        if (needsTextMateInstallation)
        {
            ReApplyTheme();
        }
        _currentDocument.IsDirty = false;
    }

    public async void ReApplyTheme()
    {
        if (DataContext is not BlitzEditorViewModel editorViewModel) return;

        if (editorViewModel.ThemeViewModel != null)
        {
            var baseOptions = editorViewModel.ThemeViewModel.RegistryOptions.BaseOptions;
            var file = editorViewModel.SelectedFiles.FirstOrDefault() as BlitzDocument;
            
            string extension = file?.Extension ?? ".txt";
            
            var language =  baseOptions.GetLanguageByExtension(extension) 
                            ?? baseOptions.GetAvailableLanguages().FirstOrDefault();
            if (language == null)
            {
                throw new NullReferenceException();
            }

            if (editorViewModel.TextMateInstallation != null)
            {
                editorViewModel.TextMateInstallation.AppliedTheme -= editorViewModel.TextMateInstallationOnAppliedTheme;
                editorViewModel.TextMateInstallation.Dispose();
            }

            var newInstallation = AvaloniaTextEditor.InstallTextMate(editorViewModel.ThemeViewModel.RegistryOptions);
            newInstallation.AppliedTheme += editorViewModel.TextMateInstallationOnAppliedTheme;
            editorViewModel.TextMateInstallation = newInstallation;
            editorViewModel.TextMateInstallationOnAppliedTheme(this,newInstallation);

            editorViewModel.TextMateInstallation?.SetGrammar(
                editorViewModel.ThemeViewModel.RegistryOptions.BaseOptions.GetScopeByLanguageId(language.Id));
        }

        await Task.Delay(0);
        AvaloniaTextEditor.TextArea.TextView.Redraw();
        AvaloniaTextEditor.InvalidateArrange();
    }
    
    
    private TextMate.Installation InstallTextMate(IRegistryOptions options) => AvaloniaTextEditor.InstallTextMate(options);


    public async void ScrollToLineColumn(BlitzDocument document)
    {
        if (_currentDocument == document)
        {
            await ScrollToPosition(document.AlignViewLine, document.AlignViewColumn);
        }
    }
    
    private async Task ScrollToPosition( int lineNumer, int column)
    {
        //I don't like this, Maybe we can work it into AvaloniaEdit itself "Load a document and center it on this line when things are finished"
        // Bugged -> https://github.com/AvaloniaUI/AvaloniaEdit/issues/469
        for (int i = 0; i < 2; i++)
        {
            AvaloniaTextEditor.ScrollTo(lineNumer, 1);

            try
            {
                var line = AvaloniaTextEditor.Document.GetLineByNumber(lineNumer);
                AvaloniaTextEditor.CaretOffset = line.Offset + column;
            }
            catch (ArgumentOutOfRangeException e)
            {
                Console.WriteLine(e);
                break;
            }

            try
            {
                await Task.Delay(50,_currentToken.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            if (_currentToken.IsCancellationRequested)
            {
                break;
            }
        }
        AvaloniaTextEditor.TextArea.TextView.Redraw();
    }
}
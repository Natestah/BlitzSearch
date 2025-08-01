using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaEdit;
using Blitz.Avalonia.Controls.ViewModels;
using Blitz.AvaloniaEdit.ViewModels;
using Blitz.Interfacing;
using MainWindowViewModel = Blitz.Avalonia.Controls.ViewModels.MainWindowViewModel;
using MouseButton = Avalonia.Input.MouseButton;

namespace Blitz.Avalonia.Controls.Views;

public partial class BlitzSecondary : UserControl
{
    public BlitzSecondary()
    {
        InitializeComponent();
        
        Loaded += (sender, args) =>
        {
            
            if (DataContext is MainWindowViewModel mainWindowViewModel)
            {
                mainWindowViewModel.ShowPreview = ShowPreview;
            }

            FileView.TextEditor.TextArea.TextView.ContextRequested +=AvaloniaTextEditorOnContextRequested;
            FileView.TextEditor.TextArea.TextView.PointerMoved+=TextViewOnPointerHover;
        };
    }


    public TextEditor AvaloniaTextEditor => FileView.TextEditor;

    public void GotoPreviewLineRun()
    {
        var docLine = AvaloniaTextEditor.Document.GetLineByOffset(AvaloniaTextEditor.CaretOffset); 
        int line = docLine.LineNumber;
        int column = AvaloniaTextEditor.CaretOffset - docLine.Offset;
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        
        var selectedModel = mainWindowViewModel.SelectedEditorViewModel;
        var selectedDocument = mainWindowViewModel.EditorViewModel.SelectedFiles.FirstOrDefault() as BlitzDocument;
        
        if (selectedModel != null && selectedDocument is { Type: BlitzDocument.DocumentType.File })
        {
            selectedModel.RunGoto(false, selectedDocument.FileNameOrTitle, line, column, out var _);
        }
    }

    private PointerEventArgs? _lastHoverEvent = null;
    private void TextViewOnPointerHover(object? sender, PointerEventArgs e)
    {
        _lastHoverEvent = e;
    }

    private void RightClickUpdatesCaret(object? sender, ContextRequestedEventArgs e)
    {
        if (!AvaloniaTextEditor.TextArea.Selection.IsEmpty) return;
        // Todo.. Figure out how to do this correctly.  Want to translate mouse position here to Document offset
        if (sender is not Visual visual || visual.GetVisualRoot() is not Visual visualRoot)
        {
            return;
        }

        if (!e.TryGetPosition(sender as Control, out Point point))
        {
            return;
        }
        var position = AvaloniaTextEditor.TextArea.TextView.GetPosition(point);
        if (_lastHoverEvent == null)
        {
            return;
        }
        var pointerPressedProperies =
            new PointerPointProperties(RawInputModifiers.LeftMouseButton,
                PointerUpdateKind.LeftButtonPressed);
        var pointerEvent = new PointerPressedEventArgs(AvaloniaTextEditor.TextArea.TextView, _lastHoverEvent.Pointer, AvaloniaTextEditor.TextArea.TextView, point, 0,
            pointerPressedProperies, KeyModifiers.None);
        AvaloniaTextEditor.TextArea.TextView.RaiseEvent(pointerEvent);
        var pointerReleasedProperies =
            new PointerPointProperties(RawInputModifiers.LeftMouseButton,
                PointerUpdateKind.LeftButtonReleased);
        var pointerReleasedEvent = new PointerReleasedEventArgs(AvaloniaTextEditor.TextArea.TextView, _lastHoverEvent.Pointer,  AvaloniaTextEditor.TextArea.TextView,
            point, 0,
            pointerReleasedProperies, KeyModifiers.None, MouseButton.Left);
        AvaloniaTextEditor.TextArea.TextView.RaiseEvent(pointerReleasedEvent);
    }

    public void SearchThisGitHubAction()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        string term = UpdateSearchThisPreview();
        if (term == null)
        {
            return;}
        {
            if (term.StartsWith("@"))
            {
                term = $"\"{term.Substring(1)}\"";
            }
            var processStartInfo = new ProcessStartInfo($"https://github.com/search?q={ term }&type=code")
            {
                UseShellExecute = true
            };
            Process.Start(processStartInfo); // todo landing page for new version.
        }
    }
    
    public string UpdateSearchThisPreview()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return string.Empty;
        }
        if (!AvaloniaTextEditor.TextArea.Selection.IsEmpty)
        {
            return AvaloniaTextEditor.TextArea.Selection.GetText();
        }
        

        int offset = AvaloniaTextEditor.CaretOffset;
        
        int startoffset = offset;
        while (startoffset > 0)
        {
            startoffset--;
            char charAt = AvaloniaTextEditor.Document.GetCharAt(startoffset);
            if (!char.IsLetterOrDigit(charAt) && charAt != '_')
            {
                startoffset++;
                break;
            }
        }
        
        int endOffSet = offset;
        while (endOffSet < AvaloniaTextEditor.Document.TextLength)
        {
            endOffSet++;
            char charAt = AvaloniaTextEditor.Document.GetCharAt(endOffSet);
            if (!char.IsLetterOrDigit(charAt) && charAt != '_')
            {
                break;
            }
        }
        
        string selectedText = AvaloniaTextEditor.Document.GetText(startoffset, endOffSet - startoffset);
        
        return $"@{selectedText}";
    }

    private void AvaloniaTextEditorOnContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        // Make the right Click place the caret, so that we can work with caret now.
        RightClickUpdatesCaret(sender,e);
        UpdateSearchThisPreview();
    }
    

    public void ShowPreview(object previewing)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }
        var editorViewModel = mainWindowViewModel.EditorViewModel;

        BlitzDocument? gotoDocument = null;
        FileNameResult? fileNameResult = null;


        if (previewing is ReloadPreviewRequest reloadPreviewRequest)
        {

            var documentLine = AvaloniaTextEditor.Document.GetLineByOffset(AvaloniaTextEditor.CaretOffset);
            int column = AvaloniaTextEditor.CaretOffset - documentLine.Offset;
            int fileLine = documentLine.LineNumber;
            gotoDocument = editorViewModel.GetOpenedOrCreateFile(reloadPreviewRequest.FileName, true, fileLine, column, mainWindowViewModel.GetRelativePathForFileName(reloadPreviewRequest.FileName));

        }
        else if (previewing is ContentResultViewModel contentResultViewModel)
        {
            fileNameResult = contentResultViewModel.FileNameResult;
            int fileLine = contentResultViewModel.FileContentResult.LineNumber;
            var firstMatch = contentResultViewModel.FileContentResult.BlitzMatches.FirstOrDefault();
            int column = firstMatch?.MatchIndex ?? 1;
            string relativePath = mainWindowViewModel.GetRelativePathForFileName(fileNameResult.FileName);
            gotoDocument = editorViewModel.GetOpenedOrCreateFile(fileNameResult.FileName,true , fileLine, column,relativePath);

        }
        else if (previewing is FileNameResultViewModel fileNameResultViewModel)
        {
            if (fileNameResultViewModel.GetDebugMessage(out var message))
            {
                gotoDocument = new BlitzDocument(BlitzDocument.DocumentType.Untitled, "Debug Message")
                    {
                        DirtyText = message
                    };
                mainWindowViewModel.EnableTextPane = true;
            }
            else
            {
                fileNameResult = fileNameResultViewModel.FileNameResult;
                
                string relativePath = mainWindowViewModel.GetRelativePathForFileName(fileNameResult.FileName);

                gotoDocument = editorViewModel.GetOpenedOrCreateFile(fileNameResult.FileName,true, 1, 1, relativePath);
            }
        }
        else  if (previewing is ExceptionViewModel exceptionViewModel)
        {
            gotoDocument = new BlitzDocument(BlitzDocument.DocumentType.Untitled, "Exception")
            {
                DirtyText = exceptionViewModel.StackInfo
            };
            mainWindowViewModel.EnableTextPane = true;
            return;
        }
        else if (previewing is MissingRequirementsViewModel missingRequirementResultViewModel) 
        {
            var missingRequirementResult = missingRequirementResultViewModel.Requirement;
            if (missingRequirementResult is MissingRequirementResult.Requirement.FileDirectory or MissingRequirementResult.Requirement.FileExtension)
            {
                mainWindowViewModel.EnableScopePane = true;
                mainWindowViewModel.ShowPreferences();
                return;
            }
        }
        else if (previewing is RobotFileSummaryViewModel robotFileSummaryViewModel)
        {
            gotoDocument = new BlitzDocument(BlitzDocument.DocumentType.Untitled, "RoboFiles")
            {
                DirtyText = robotFileSummaryViewModel.GetCsvReport()
            };
        }
        else if (previewing is ReplaceTextViewModel replaceTextViewModel)
        {
            //This seems weird, can't remember why I don't have the extension available here..   
            var baseOptions = mainWindowViewModel.EditorViewModel.ThemeViewModel?.RegistryOptions.BaseOptions;

            if (baseOptions == null)
            {
                throw new NullReferenceException();
            }
            var language =  baseOptions.GetLanguageByExtension(".txt") 
                            ?? baseOptions.GetAvailableLanguages().FirstOrDefault();
            if (language == null)
            {
                throw new NullReferenceException();
            }
            mainWindowViewModel.EditorViewModel.TextMateInstallation?.SetGrammar(baseOptions.GetScopeByLanguageId(language.Id));
            gotoDocument = new BlitzDocument(BlitzDocument.DocumentType.Untitled, "Replace Preview")
            {
                DirtyText = replaceTextViewModel.TextSummary
            };
        }
        else if (previewing is string asString)
        {
            gotoDocument = new BlitzDocument(BlitzDocument.DocumentType.Untitled, "Message")
            {
                DirtyText = asString
            };
        }

        if (gotoDocument == null)
        {
            return;
        }

        if (!mainWindowViewModel.EditorViewModel.OpenedFiles.Any(a => a == gotoDocument))
        {
            mainWindowViewModel.EditorViewModel.OpenedFiles.Add(gotoDocument);
        }

        var isCurrentSelection = mainWindowViewModel.EditorViewModel.SelectedFiles.Count == 1 &&
                                 mainWindowViewModel.EditorViewModel.SelectedFiles[0] == gotoDocument;

        if (!isCurrentSelection)
        {
            mainWindowViewModel.EditorViewModel.SelectedFiles.Clear();
            mainWindowViewModel.EditorViewModel.SelectedFiles.Add(gotoDocument);
        }
        else
        {
            FileView.ScrollToLineColumn(gotoDocument);
        }
    }
    public void ShowHelp()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }
        mainWindowViewModel.EnableHelpPane = true;
        
    }
}
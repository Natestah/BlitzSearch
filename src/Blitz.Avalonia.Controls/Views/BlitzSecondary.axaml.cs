using System.IO;
using Avalonia.Controls;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using Blitz.Avalonia.Controls.ViewModels;
using Blitz.AvaloniaEdit.ViewModels;
using Blitz.Interfacing;
using Humanizer;
using MainWindowViewModel = Blitz.Avalonia.Controls.ViewModels.MainWindowViewModel;
using MouseButton = Avalonia.Input.MouseButton;

namespace Blitz.Avalonia.Controls.Views;

public partial class BlitzSecondary : UserControl
{
    public BlitzSecondary()
    {
        InitializeComponent();
        ShowHelpMd();
        Loaded += (sender, args) =>
        {
            
            if (DataContext is MainWindowViewModel mainWindowViewModel)
            {
                mainWindowViewModel.ShowPreview = ShowPreview;
            }
            _ = CalculateCacheUsage();

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
        bool loadFile = false;
        FileNameResult? fileNameResult = null;


        if (previewing is ReloadPreviewRequest reloadPreviewRequest)
        {

            var documentLine = AvaloniaTextEditor.Document.GetLineByOffset(AvaloniaTextEditor.CaretOffset);
            int column = AvaloniaTextEditor.CaretOffset - documentLine.Offset;
            int fileLine = documentLine.LineNumber;
            gotoDocument = editorViewModel.GetOpenedOrCreateFile(reloadPreviewRequest.FileName, true, fileLine, column);

        }
        else if (previewing is ContentResultViewModel contentResultViewModel)
        {
            fileNameResult = contentResultViewModel.FileNameResult;
            int fileLine = contentResultViewModel.FileContentResult.LineNumber;
            var firstMatch = contentResultViewModel.FileContentResult.BlitzMatches.FirstOrDefault();
            int column = firstMatch?.MatchIndex ?? 1;
            gotoDocument = editorViewModel.GetOpenedOrCreateFile(fileNameResult.FileName,true , fileLine, column);

        }
        else if (previewing is FileNameResultViewModel fileNameResultViewModel)
        {
            if (fileNameResultViewModel.GetDebugMessage(out var message))
            {
                gotoDocument = new BlitzDocument(BlitzDocument.DocumentType.Untitled, "Debug Message")
                    {
                        DirtyText = message
                    };
                mainWindowViewModel.SplitPane = true;
                mainWindowViewModel.EnableTextPane = true;
                return;
            }

            fileNameResult = fileNameResultViewModel.FileNameResult;
            gotoDocument = editorViewModel.GetOpenedOrCreateFile(fileNameResult.FileName,true, 1, 1);

        }
        else  if (previewing is ExceptionViewModel exceptionViewModel)
        {
            gotoDocument = new BlitzDocument(BlitzDocument.DocumentType.Untitled, "Exception")
            {
                DirtyText = exceptionViewModel.StackInfo
            };
            mainWindowViewModel.SplitPane = true;
            mainWindowViewModel.EnableTextPane = true;
            return;
        }
        else if (previewing is MissingRequirementsViewModel missingRequirementResultViewModel) 
        {
            var missingRequirementResult = missingRequirementResultViewModel.Requirement;
            if (missingRequirementResult is MissingRequirementResult.Requirement.FileDirectory or MissingRequirementResult.Requirement.FileExtension)
            {
                mainWindowViewModel.SplitPane = true;
                mainWindowViewModel.EnableScopePane = true;
                return;
            }
        }
        else if (previewing is RobotFileSummaryViewModel robotFileSummaryViewModel)
        {
            gotoDocument = new BlitzDocument(BlitzDocument.DocumentType.Untitled, "RoboFiles")
            {
                DirtyText = robotFileSummaryViewModel.GetCsvReport()
            };
            if (mainWindowViewModel.SplitPane)
            {
                mainWindowViewModel.EnableTextPane = true;
            }
        }
        else if (previewing is ReplaceTextViewModel replaceTextViewModel)
        {
            //Todo: This seems weird, can't remember why I don't have the extension available here..   
            var baseOptions = mainWindowViewModel.EditorViewModel.ThemeViewModel.RegistryOptions.BaseOptions;
            
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
            
            if (mainWindowViewModel.SplitPane)
            {
                mainWindowViewModel.EnableTextPane = true;
            }
        }
        else if (previewing is string asString)
        {
            gotoDocument = new BlitzDocument(BlitzDocument.DocumentType.Untitled, "Message")
            {
                DirtyText = asString
            };
            if (mainWindowViewModel.SplitPane)
            {
                mainWindowViewModel.EnableTextPane = true;
            }
        }
        if (mainWindowViewModel.SplitPane)
        {
            mainWindowViewModel.EnableTextPane = true;
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
    
    private void ShowHelpMd()
    {
        HelpBox.SelectedItem = null;
        if (!Configuration.Instance.IsWelcomed)
        {
            HelpBox.SelectedItem = HelpBox.Items[0]; // first is welcome
            Configuration.Instance.IsWelcomed = true;
        }
        else
        {
            foreach (var item in HelpBox.Items.OfType<ListBoxItem>())
            {
                if (item.Content is not "Change Log") continue;
                HelpBox.SelectedItem = item;
                break;
            }
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

    private void CollectGarbage_OnClick(object? _, RoutedEventArgs e)
    {
        GC.Collect();
    }
    private async void CacheClean_OnClick(object? _, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
            return;
        mainWindowViewModel.CacheStatus = "Cleaning...";
        mainWindowViewModel.CacheCleaning = true;

        int count = 0;
        await Task.Run(() => { 
            foreach (var file in Directory.EnumerateFiles(SearchExtensionCache.CacheFolder))
            {
                try
                {
                    File.Delete(file);
                    count++;
                    Dispatcher.UIThread.Post(() => { mainWindowViewModel.CacheStatus = $"Cleaned '{count}' Files"; });
                }
                catch( Exception ex)
                {
                    Dispatcher.UIThread.Post(() => { mainWindowViewModel.CacheStatus = $"failed {ex.Message}"; });
                }
            }
        });
        await CalculateCacheUsage();
        mainWindowViewModel.CacheCleaning = false;
    }

    private async Task CalculateCacheUsage()
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
            return;

        long fileSizeBytes = 0;
        mainWindowViewModel.CacheStatus = $"Cache Size '{fileSizeBytes.Bytes().Humanize()}'";
        await Task.Run(() => { 
            if(!Directory.Exists(SearchExtensionCache.CacheFolder))
            {
                return;
            }
            foreach (var file in Directory.EnumerateFiles(SearchExtensionCache.CacheFolder))
            {
                try
                {
                    fileSizeBytes += new FileInfo(file).Length;
                    Dispatcher.UIThread.Post(() => { mainWindowViewModel.CacheStatus = $"Cache Size '{fileSizeBytes.Bytes().Humanize()}'"; });
                }
                catch( Exception )
                {
                    // ignore
                }
            }
        });
        
    }

    private void HelpBoxItemChanged(object? sender, SelectionChangedEventArgs e)
    {
        if ((e.AddedItems[0] as ListBoxItem)?.Content is not string text) return;
        text = text.Replace(" ", "_");
        var uri = new Uri(Path.GetFullPath($"Documentation/{text}.md"));

        if (!File.Exists(uri.LocalPath))
        {
            // Deployed and Working dir didn't resolve.. 
            uri = new Uri(Environment.ExpandEnvironmentVariables($"%programfiles%\\blitz\\Documentation\\{text}.md"));
        }
        
        if (Path.Exists(uri.LocalPath))
        {
            MarkdownScrollViewer.AssetPathRoot = Path.GetDirectoryName(uri.LocalPath);
            MarkdownScrollViewer.Source = uri;
        }
        else
        {
            MarkdownScrollViewer.Markdown = uri.LocalPath;
        }
            
    }
}
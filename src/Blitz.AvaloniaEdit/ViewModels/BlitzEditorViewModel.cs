using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Dialogs.Internal;
using AvaloniaEdit.TextMate;
using Avalonia.Media;
using Blitz.AvaloniaEdit.Models;
using TextMateSharp.Grammars;
using ReactiveUI;
using TextMateSharp.Registry;

namespace Blitz.AvaloniaEdit.ViewModels;


/// <summary>
/// View Model For hosted editors, hold list of opened files ( file tabs ) and a selection list (Typically one)
/// </summary>
public class BlitzEditorViewModel : ViewModelBase
{
    private TextMate.Installation? _textMateInstallation;
    private IBrush? _statusBarForeground;
    private IBrush? _statusBarBackground;
    private IBrush? _titleBarBackground = Brushes.Transparent;
    private IBrush? _textForeground;
    private string? _searchThisPreviewText;

    public BlitzEditorViewModel()
    {
        _selectedFiles.CollectionChanged+=SelectedFilesOnCollectionChanged;
    }

    private void SelectedFilesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.RaisePropertyChanged(nameof(CurrentDocument));
    }

    public Action<TextMate.Installation>? BackGroundForeGroundUpdate;

    private ThemeViewModel? _blitzThemeViewModel;
    public ThemeViewModel? ThemeViewModel
    {
        get => _blitzThemeViewModel;
        set
        {
            if (value == null)
            {
                return;
            }

            if (!AllThemeViewModels.Contains(value))
            {
                AllThemeViewModels.Add(value);
            }
            _blitzThemeViewModel = value;
            this.RaisePropertyChanged();
        }
    }
    private ObservableCollection<object> _selectedFiles = [];

    /// <summary>
    /// All the files that are opened ( file tabs )
    /// </summary>
    public ObservableCollection<BlitzDocument> OpenedFiles { get; set; } = [];

    /// <summary>
    /// Selected file(s)
    /// </summary>
    public ObservableCollection<object> SelectedFiles
    {
        get => _selectedFiles;
    }

    public BlitzDocument? CurrentDocument
    {
        get => SelectedFiles.FirstOrDefault() as BlitzDocument;
    }


    /// <summary>
    /// Gets the current opened file, if one isn't in the collection a new one 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="openInPreview"></param>
    /// <param name="lineNumber"></param>
    /// <param name="columnNumber"></param>
    /// <returns></returns>
    public BlitzDocument GetOpenedOrCreateFile(string fileName, bool openInPreview = false, int lineNumber = 1, int columnNumber = 1)
    {
        int afterSelectedIndex = 0;
        if (SelectedFiles.FirstOrDefault() is BlitzDocument selected)
        {
            afterSelectedIndex = OpenedFiles.IndexOf(selected) + 1;
        }

        foreach (var item in OpenedFiles.OfType<BlitzDocument>())
        {
            var isRequestedFile = item.FileNameOrTitle == fileName;
            if (openInPreview && item.IsPreviewing && !isRequestedFile)
            {
                var index = OpenedFiles.IndexOf(item);
                OpenedFiles.Remove(item);
                var updatedPreview = new BlitzDocument(BlitzDocument.DocumentType.File, fileName){AlignViewLine = lineNumber, AlignViewColumn = columnNumber, IsPreviewing = openInPreview};
                OpenedFiles.Insert(index, updatedPreview);
                SelectDocument(updatedPreview);
                return updatedPreview;
            }
            
            if (item.Type != BlitzDocument.DocumentType.File
                || !isRequestedFile)
            {
                continue;
            }
            item.AlignViewLine = lineNumber;
            item.AlignViewColumn = columnNumber;
            SelectDocument(item);
            return item;
        }

        
        var returnDocument = new BlitzDocument(BlitzDocument.DocumentType.File, fileName){AlignViewLine = lineNumber, AlignViewColumn = columnNumber, IsPreviewing = openInPreview};
        OpenedFiles.Insert(afterSelectedIndex, returnDocument);
        SelectDocument(returnDocument);
        return returnDocument;
    }

    private void SelectDocument(BlitzDocument document)
    {
        SelectedFiles.Clear();
        SelectedFiles.Add(document);
    }
    
    
    public string? SearchThisPreviewText
    {
        get => _searchThisPreviewText;
        set => this.RaiseAndSetIfChanged(ref _searchThisPreviewText,value); 
    }


    /// <summary>
    /// Request Line when setting selection, this will goto, once the file is loaded.
    /// </summary>
    public int RequestedGotoLine { get; set; } = -1;
    
    /// <summary>
    /// Request Line when setting selection. This will goto, once the file is loaded.
    /// </summary>
    public int RequestedGotoColumn { get; set; } = -1;
    
    
    public ObservableCollection<ThemeViewModel> AllThemeViewModels { get; } = [];

    private bool _populated = false;
    public void PopulateThemeModels()
    {
        if (_populated)
        {
            return;
        }
        _populated = true;
        foreach (ThemeName themeName in Enum.GetValuesAsUnderlyingType(typeof(TextMateSharp.Grammars.ThemeName)))
        {
            var newBlitzTHeme = themeName.ToString().ToLower().Contains("light") ? FromBase(BlitzTheme.Light, themeName) : FromBase(BlitzTheme.Dark, themeName);
            AllThemeViewModels.Add( new ThemeViewModel(newBlitzTHeme));
        }
        //this.ThemeViewModel = AllThemeViewModels.FirstOrDefault(model => model.ThemeName == ThemeName.Monokai);
    }
    
    public TextMate.Installation? TextMateInstallation
    {
        get => _textMateInstallation;
        set => this.RaiseAndSetIfChanged(ref _textMateInstallation, value);
    }

    private FontFamily _selectedFontFamily = FontFamily.Default;
    private BlitzDocument? _currentDocument;


    bool ApplyBrushAction(TextMate.Installation e, string colorKeyNameFromJson, Action<IBrush> applyColorAction)
    {
        if (!e.TryGetThemeColor(colorKeyNameFromJson, out var colorString))
            return false;

        if (!Color.TryParse(colorString, out Color color))
            return false;

        var colorBrush = new SolidColorBrush(color);
        applyColorAction(colorBrush);
        return true;
    }


    public IBrush? StatusBarForeground
    {
        get => _statusBarForeground;
        set => this.RaiseAndSetIfChanged(ref _statusBarForeground, value);
    }

    public IBrush? StatusBarBackground
    {
        get => _statusBarBackground;
        set => this.RaiseAndSetIfChanged(ref _statusBarBackground, value);
    }

    public IBrush? TitleBarBackground
    {
        get => _titleBarBackground;
        set => this.RaiseAndSetIfChanged(ref _titleBarBackground, value);
    }

    public IBrush? TextForeground
    {
        get => _textForeground;
        set => this.RaiseAndSetIfChanged(ref _textForeground, value);
    }

    public FontFamily SelectedFontFamily
    {
        get => _selectedFontFamily;
        set => this.RaiseAndSetIfChanged(ref _selectedFontFamily, value);
    }

    public void TextMateInstallationOnAppliedTheme(object? sender, TextMate.Installation e)
    {
        if (!ApplyBrushAction(e,"statusBar.background", brush => StatusBarBackground = brush))
        {
            StatusBarBackground = Brushes.Transparent;
        }

        if (!ApplyBrushAction(e, "titleBar.activeBackground", brush => TitleBarBackground = brush))
        {
            TitleBarBackground = Brushes.Transparent;
        }

        if (!ApplyBrushAction(e,"statusBar.foreground", brush => StatusBarForeground = brush))
        {
            ApplyBrushAction(e,"editor.foreground", brush => StatusBarForeground = brush);
        }

        ApplyBrushAction(e,"editor.foreground", brush => TextForeground = brush);
        
        //Applying the Editor background to the whole window for demo sake.
        BackGroundForeGroundUpdate?.Invoke(e);
    }


    public BlitzTheme FromBase(BlitzTheme baseTheme,ThemeName themeName)
    {
        return FromBase(baseTheme, themeName.ToString());
    }
    public BlitzTheme FromBase(BlitzTheme baseTheme,string themeName)
    {
        return new BlitzTheme
        {
            TextForeground = baseTheme.TextForeground,
            WindowBackground = baseTheme.WindowBackground,
            PassiveIcon = baseTheme.PassiveIcon,
            ContentHighlightBackground = baseTheme.ContentHighlightBackground,
            ContentHighlightBorder = baseTheme.ContentHighlightBorder,
            ContentHighlightReplaceBackground = baseTheme.ContentHighlightReplaceBackground,
            ContentHighlightReplaceBorder = baseTheme.ContentHighlightReplaceBorder,
            SelectedItemBackground =baseTheme.SelectedItemBackground,
            AvaloniaThemeVariant = baseTheme.AvaloniaThemeVariant,
            ThemeName = themeName.ToString()
        };
    }

}
using System;
using System.Text;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using AvaloniaEdit.TextMate.Grammars;
using Blitz.AvaloniaEdit.Models;
using ReactiveUI;
using TextMateSharp.Registry;
using ThemeName = TextMateSharp.Grammars.ThemeName;
namespace Blitz.Avalonia.Controls.ViewModels;

public class BlitzThemeViewModel : ViewModelBase
{
    private readonly BlitzTheme _blitzTheme;

    public BlitzTheme Theme => _blitzTheme;
    TextMateSharp.Grammars.RegistryOptions? _options;
    Registry? _registry;
    private MainWindowViewModel _mainWindowViewModel;

    private bool _freeToPlay;
    public BlitzThemeViewModel(MainWindowViewModel mainWindowViewModel, BlitzTheme blitzTheme, bool isFreeToPlay)
    {
        _blitzTheme = blitzTheme;
        _freeToPlay = isFreeToPlay;
        _mainWindowViewModel = mainWindowViewModel;
        string themeString = Theme.ThemeName;
        if (!Enum.TryParse(themeString, out TextMateSharp.Grammars.ThemeName themeName))
        {
            themeName = ThemeName.DarkPlus;
        }
        _options = new TextMateSharp.Grammars.RegistryOptions(themeName);
        _registry = new Registry(_options);
        var theme = _registry.GetTheme();
        if (theme.GetGuiColorDictionary().TryGetValue("editor.background", out var colorHexString))
        {
            BackGroundBrush = new SolidColorBrush(Color.Parse(colorHexString));
        }
        else
        {
            BackGroundBrush = Brushes.Transparent;
        }

    }

    
    public bool IsPremiumOnly =>  !_freeToPlay;
    public IBrush BackGroundBrush { get; }

    public ThemeName ThemeName => Enum.TryParse(_blitzTheme.ThemeName, out ThemeName parsedName ) ? parsedName : ThemeName.DarkPlus;

    private string GetDisplayText()
    {
        StringBuilder b = new();
        b.AppendLine($"{_blitzTheme.ThemeName}");
        b.AppendLine();
        b.AppendLine($"// Comment..");
        b.AppendLine("object.functionCall(\"sample string\")");
        b.AppendLine("for(int i = 0; i < 33; i++)");
        return b.ToString();
    } 
    
    public InlineCollection ContentWithHighlights
    {
        get
        {
            string sampleText = GetDisplayText();
            
            string fauxFileName = "c:\\sample.cs";
            // given the lines contents and the filename.. come up with some highlights.
            if (_options != null)
            {
                if (_registry != null)
                {
                    var inlineCollection = ResultsHighlighting.GetInlinesFromTextMateSharp(_options,_registry, sampleText, fauxFileName);

                    // // Highlight the matches.
                    // var matchHighlighter = new InlineMatchHighlighter(inlineCollection, fileContentResultResult.BlitzMatches, fileContentResultResult.CapturedContents);
                    // inlineCollection = matchHighlighter.HighlightInlines();
                    // if (largeLine)
                    // {
                    //     inlineCollection.Add( new Run("[Truncated]"){Foreground = Brushes.Yellow,Background = Brushes.DarkSlateGray});
                    // }
                    return  inlineCollection;
                }
            }

            return [];
        }
    }

    public void RefreshHighlights()
    {
        this.RaisePropertyChanged(nameof(ContentWithHighlights));
    }

}
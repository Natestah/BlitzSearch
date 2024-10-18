using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using ThemeName = TextMateSharp.Grammars.ThemeName;

namespace Blitz.Avalonia.Controls;

/// <summary>
/// Helper for TextmateSharp.
/// </summary>
public class ResultsHighlighting
{
    private ResultsHighlighting()
    {
        InstallHighlighting();
    }

    public void InstallHighlighting()
    {
        string themeString = Configuration.Instance.CurrentTheme.ThemeName;
        if (!Enum.TryParse(themeString, out ThemeName themeName))
        {
            themeName = ThemeName.DarkPlus;
        }
        _options = new RegistryOptions(themeName);
        _registry = new Registry(_options);
    }


    private static ResultsHighlighting? _instance;
    public static ResultsHighlighting Instance => _instance ??= new ResultsHighlighting();

    RegistryOptions? _options;
    Registry? _registry;


    private IBrush BrushFromColorWithOpacity(Color color, double opacity)
    {
        
        return new SolidColorBrush(color, opacity);
    }
    
    public static InlineCollection GetInlinesFromTextMateSharp(RegistryOptions options, Registry registry,string text, string fileNameForContext, double opacity = 1)
    {
        var inlineCollection = new InlineCollection();
        var extension = Path.GetExtension(fileNameForContext);
        Debug.Assert(options != null, nameof(_options) + " != null");
        var language = options.GetLanguageByExtension(extension)
                       ?? options.GetAvailableLanguages().FirstOrDefault();

        if (language == null)
        {
            throw new NullReferenceException();
        }

        Debug.Assert(registry != null, nameof(registry) + " != null");
        var grammar = registry.LoadGrammar(options.GetScopeByLanguageId(language.Id));
        if (grammar == null)
        {
            return [new Run(text) { Foreground = new SolidColorBrush(Colors.Azure, opacity) , BaselineAlignment = BaselineAlignment.Center }];
        }

        using var re = new StringReader(text);
        while (re.Peek() != -1)
        {
            string line = re.ReadLine()!;
            var result = grammar.TokenizeLine(line, null, TimeSpan.MaxValue);
            var theme = registry.GetTheme();
            foreach (var token in result.Tokens)
            {
                int startIndex = (token.StartIndex > line.Length) ? line.Length : token.StartIndex;
                int endIndex = (token.EndIndex > line.Length) ? line.Length : token.EndIndex;

                int foreground = 0;
                int background = 0;
                FontStyle fontStyle = FontStyle.Normal;

                foreach (var themeRule in  theme.Match(token.Scopes))
                {
                    if (foreground == 0 && themeRule.foreground > 0)
                        foreground = themeRule.foreground;

                    if (background == 0 && themeRule.background > 0)
                        background = themeRule.background;

                    if (fontStyle == 0 && themeRule.fontStyle > 0)
                        fontStyle = (FontStyle)themeRule.fontStyle;
                }

                string runText = line.Substring(startIndex, endIndex - startIndex);

                var converter = new BrushConverter();

                if (foreground == 0)
                {
                    if (theme.GetGuiColorDictionary().TryGetValue("editor.foreground", out var stringHex))
                    {
                        var thisRun = new Run(runText) { Foreground = new SolidColorBrush(Color.Parse(stringHex), opacity), BaselineAlignment = BaselineAlignment.Center };
                        inlineCollection.Add(thisRun);
                    }
                    else
                    {
                        var thisRun = new Run(runText) { Foreground = new SolidColorBrush(Configuration.Instance.CurrentTheme.TextForeground, opacity), BaselineAlignment = BaselineAlignment.Center  };
                        inlineCollection.Add(thisRun);
                    }
                    
                }
                else
                {
                    var brushImutable = (ImmutableSolidColorBrush)converter.ConvertFrom(theme.GetColor(foreground))!;
                    var asIBrush = brushImutable as IBrush;
                    if (1-opacity > Double.Epsilon)
                    {
                        asIBrush = new SolidColorBrush(brushImutable.Color, opacity);
                    }
                    var thisRun = new Run(runText) { Foreground = asIBrush, BaselineAlignment = BaselineAlignment.Center  };
                    inlineCollection.Add(thisRun);
                }
            }

            if (re.Peek() != -1)
            {
                inlineCollection.Add(new LineBreak());
            }
        }
        

        return inlineCollection;
    }

    public CharState[] GetCharStatesFromInlines(  InlineCollection inlineCollection, string displayContents )
    {
        var states = new CharState[displayContents.Length];
        int currentIndex = 0;
        foreach (var inline in inlineCollection)
        {
            if (inline is not Run run) continue;
            if (run.Text == null) continue;
            for (int i = 0; i < run.Text.Length; i++)
            {
                int stateIndex = currentIndex + i;
                var state = states[stateIndex] =  new CharState();
                state.Index = stateIndex;
                state.Foreground = run.Foreground ?? new SolidColorBrush(Colors.Black);
            }
            currentIndex+=run.Text.Length;
        }

        return states;
    }

    public InlineCollection GetInlinesFromTextMateSharp(string line, string fileNameForContext, double opacity = 1)
    {
        if (_options == null) return [];
        return _registry != null ? GetInlinesFromTextMateSharp(_options, _registry, line, fileNameForContext, opacity) : [];
    }
}
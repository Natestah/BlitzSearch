using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Blitz.AvaloniaEdit.Models;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using FontStyle = Avalonia.Media.FontStyle;

using Blitz.Interfacing;
namespace Blitz.AvaloniaEdit;

public static class TextMateInlineGenerator
{
    
    public static InlineCollection GetInlinesFromTextMateSharp(BlitzTheme currentTheme, RegistryOptions options, Registry registry,string text, string fileNameForContext, double opacity = 1)
    {
        var extension = Path.GetExtension(fileNameForContext);

        //I'd like to put this in the hands of the user - https://github.com/Natestah/BlitzSearch/issues/107
        extension = ExtensionAsMap.Get(extension);
        
        var language = options.GetLanguageByExtension(extension)
                       ?? options.GetAvailableLanguages().FirstOrDefault();

        if (language == null)
        {
            throw new NullReferenceException();
        }

        Debug.Assert(registry != null, nameof(registry) + " != null");
        IGrammar? grammar = registry.LoadGrammar(options.GetScopeByLanguageId(language.Id));
        if (grammar == null)
        {
            return [new Run(text) { Foreground = new SolidColorBrush(Colors.Azure, opacity) , BaselineAlignment = BaselineAlignment.Center }];
        }
        var theme = registry.GetTheme();

        return GetInlinesFromTextMateSharp(currentTheme, grammar, theme, text, opacity);
       
    }
    public static InlineCollection GetInlinesFromTextMateSharp(BlitzTheme currentTheme, IGrammar? grammar, Theme theme, string text, double opacity = 1)
    {
        var inlineCollection = new InlineCollection();

        if (grammar == null)
        {
            return [new Run(text) { Foreground = new SolidColorBrush(Colors.Azure, opacity) , BaselineAlignment = BaselineAlignment.Center }];
        }

        using var re = new StringReader(text);
        while (re.Peek() != -1)
        {
            string line = re.ReadLine()!;
            var result = grammar.TokenizeLine(line, null, TimeSpan.MaxValue);
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
                        var thisRun = new Run(runText) { Foreground = new SolidColorBrush(currentTheme.TextForeground, opacity), BaselineAlignment = BaselineAlignment.Center  };
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
}
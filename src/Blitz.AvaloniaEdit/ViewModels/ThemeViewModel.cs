using System;
using System.IO;
using System.Text;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Blitz.AvaloniaEdit.Models;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;

namespace Blitz.AvaloniaEdit.ViewModels;

public class ThemeViewModel : ViewModelBase
{
     private readonly BlitzTheme _blitzTheme;

    public BlitzTheme Theme => _blitzTheme;
    private readonly BlitzTextMateGrammarsRegistryOptions _options;

    public Registry Registry { get; }
    
    public BlitzTextMateGrammarsRegistryOptions RegistryOptions => _options;
    
    public bool ThemeIsFromFile { get; }
    
    public ThemeViewModel(BlitzTheme blitzTheme)
    {
        _blitzTheme = blitzTheme;
        string themeString = Theme.ThemeName;
        if (!Enum.TryParse(themeString, out TextMateSharp.Grammars.ThemeName themeName))
        {
            themeName = ThemeName.DarkPlus;
        }
        
        var options = new TextMateSharp.Grammars.RegistryOptions(themeName);

        string? themeAsFile = null;
        try
        {
            if (File.Exists(themeString))
            {
                themeAsFile = themeString;
            }
        }
        catch (Exception)
        {
            //it's not a file.
            themeAsFile = null;
        }
        ThemeIsFromFile  = themeAsFile != null;
        _options = new BlitzTextMateGrammarsRegistryOptions(options, themeAsFile);
        Registry = new Registry(_options);
        
        var colorDictionary = Registry.GetTheme().GetGuiColorDictionary();
        if (colorDictionary.TryGetValue("editor.background", out var colorHexString))
        {
            BackGroundBrush = new SolidColorBrush(Color.Parse(colorHexString));
        }
        else
        {
            BackGroundBrush = Brushes.Transparent;
        }
        
        if (colorDictionary.TryGetValue("sideBar.background", out var sideBarBackground)
            ||colorDictionary.TryGetValue("menu.background", out sideBarBackground))
        {
            ResultsBackGroundBrush = new SolidColorBrush(Color.Parse(sideBarBackground));
        }
        else
        {
            // Todo: try and simply blend background with forground to differentiate automatically
            ResultsBackGroundBrush = BackGroundBrush; 
        }
    }
    
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
                var inlineCollection = TextMateInlineGenerator.GetInlinesFromTextMateSharp(Theme,RegistryOptions.BaseOptions,Registry, sampleText, fauxFileName);
                return  inlineCollection;
            }
            return [];
        }
    }
    
    
    public IBrush BackGroundBrush { get; }
    public IBrush ResultsBackGroundBrush { get; }

    public ThemeName ThemeName => Enum.TryParse(_blitzTheme.ThemeName, out ThemeName parsedName ) ? parsedName : ThemeName.DarkPlus;

}
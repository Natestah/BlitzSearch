using System.Collections.Generic;
using System.IO;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Themes.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace Blitz.AvaloniaEdit;

/// <summary>
/// Blitz TextmateGrammars Implements IRegistryOptions in order to provide File based Theme Selection. Construct with a themefileName that is a json file an
/// </summary>
/// <param name="options"></param>
/// <param name="themeFileName"></param>
public class BlitzTextMateGrammarsRegistryOptions(RegistryOptions options, string? themeFileName=null) : IRegistryOptions
{
    public RegistryOptions BaseOptions { get; } = options;

    public IRawTheme GetTheme(string scopeName) => BaseOptions.GetTheme(scopeName);

    public IRawGrammar GetGrammar(string scopeName) => BaseOptions.GetGrammar(scopeName);

    public ICollection<string> GetInjections(string scopeName) => BaseOptions.GetInjections(scopeName);


    private IRawTheme? _cachedTheme = null;
    
    public IRawTheme GetDefaultTheme()
    {
        if(_cachedTheme != null) return _cachedTheme;
        if (!string.IsNullOrEmpty(themeFileName) && File.Exists(themeFileName))
        {
            using var stream = new FileStream(themeFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream);
            return _cachedTheme = ThemeReader.ReadThemeSync(reader);
        }
        return _cachedTheme = BaseOptions.GetDefaultTheme();
    }
}

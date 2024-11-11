using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Material.Icons;

namespace Blitz.AvaloniaEdit.Converters;

public class FileTypeIconConverter : IValueConverter
{
    private static readonly Dictionary<string, MaterialIconKind> _iconKinds = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".cpp", MaterialIconKind.LanguageCpp },
        { ".c", MaterialIconKind.LanguageC },
        { ".csproj", MaterialIconKind.Visualstudio },
        { ".js", MaterialIconKind.LanguageJavascript },
        { ".java", MaterialIconKind.LanguageJava },
        { ".ts", MaterialIconKind.LanguageTypescript },
        { ".css", MaterialIconKind.LanguageCss3 },
        { ".for", MaterialIconKind.LanguageFortran },
        { ".f90", MaterialIconKind.LanguageFortran },
        { ".f", MaterialIconKind.LanguageFortran },
        { ".cs", MaterialIconKind.LanguageCsharp },
        { ".go", MaterialIconKind.LanguageGo },
        { ".lua", MaterialIconKind.LanguageLua },
        { ".py", MaterialIconKind.LanguagePython },
        { ".xaml", MaterialIconKind.LanguageXaml },
        { ".axaml", MaterialIconKind.LanguageXaml },
        { ".html", MaterialIconKind.LanguageHtml5 },
        { ".htm", MaterialIconKind.LanguageHtml5 },
        { ".md", MaterialIconKind.LanguageMarkdown },
        { ".kt", MaterialIconKind.LanguageKotlin },
        { ".php", MaterialIconKind.LanguagePhp },
        { ".r", MaterialIconKind.LanguageR },
        { ".rb", MaterialIconKind.LanguageRuby },
        { ".rs", MaterialIconKind.LanguageRust },
        { ".swift", MaterialIconKind.LanguageSwift },
        { ".txt", MaterialIconKind.Text },
    };

    private static readonly MaterialIconKind DefaultKind = MaterialIconKind.FileDocument;
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string asString) return DefaultKind;
        if (!File.Exists(asString)) return DefaultKind;
        var extension = Path.GetExtension(asString);
        return _iconKinds.GetValueOrDefault(extension, DefaultKind);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
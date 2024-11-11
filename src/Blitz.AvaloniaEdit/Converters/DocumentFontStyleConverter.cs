using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Blitz.AvaloniaEdit.Converters;

/// <summary>
/// Font Style Converter for Preview tabs.
/// </summary>
public class DocumentFontStyleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? FontStyle.Italic : FontStyle.Normal;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
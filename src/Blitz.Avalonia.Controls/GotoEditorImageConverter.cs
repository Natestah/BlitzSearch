using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Blitz.Avalonia.Controls;

public class GotoEditorImageConverter : IMultiValueConverter
{
    private static readonly Dictionary<string, string> ExeNameToResourceLocator = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        {"Rider64.exe", "Rider_icon.png"},
        {"idea64.exe", "intellij_idea.png"},
        {"pycharm64.exe", "pycharm.png"},
        {"goland64.exe", "goland.png"},
        {"rustrover64.exe", "rustrover.png"},
        {"clion64.exe", "clion.png"},
        {"phpstorm64.exe", "phpstorm.png"},
        {"webstorm64.exe", "webstorm.png"},
        {"rubymine64.exe", "rubymine.png"},
        {"notepad++.exe", "Notepad_plus_plus.png"},
        {"Code.cmd", "vscode.png"},
        {"uedit64.exe", "uelogo.png"},
        {"fleet.exe", "Fleet.png"},
        {"devenv.exe", "VisualStudio2022PNG.png"},
        {"notepad.exe", "Notepad_Win11PNG.png"},
        {"subl.exe", "sublime_text.png"},
    };
    
    
    private Bitmap? CreateBitmap(IBrush brush)
    {
        var internalImage = new DrawingImage
        {
            Drawing = new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(new Size(24,24))),
                Brush = brush
            }
        };
        
        var pixelSize = new PixelSize((int)internalImage.Size.Width, (int)internalImage.Size.Height);
        Bitmap? returnImage = null;
        using (MemoryStream memoryStream = new())
        {
            using (RenderTargetBitmap bitmap = new(pixelSize, new Vector(72, 72)))
            {
                using (DrawingContext ctx = bitmap.CreateDrawingContext())
                {
                    internalImage.Drawing.Draw(ctx);
                }
                bitmap.Save(memoryStream);
            }
            memoryStream.Seek(0, SeekOrigin.Begin);
            returnImage = new Bitmap(memoryStream);
        }
        return returnImage;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private bool TryFromResource(string? exe, out Bitmap? bitmap)
    {
        if (string.IsNullOrEmpty(exe))
        {
            bitmap = null;
            return false;
        }

        if (ExeNameToResourceLocator.TryGetValue(exe, out var iconName))
        {
           var stream = AssetLoader.Open(new Uri($"avares://Blitz.Avalonia.Controls/Resources/{iconName}"));
           bitmap = new Bitmap(stream);
           return true;
        }

        bitmap = null;
        return false;
    }
    

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2)
        {
            return CreateBitmap(Brushes.Red);
        }

        var exe = values[0] as string;
        var exeHint = values[1] as string;


        if (TryFromResource(exeHint, out var bitmap)
            || TryFromResource(exe, out bitmap))
        {
            return bitmap ?? CreateBitmap(Brushes.Chartreuse);
        }

        return CreateBitmap(Brushes.DarkSlateGray);
    }
}
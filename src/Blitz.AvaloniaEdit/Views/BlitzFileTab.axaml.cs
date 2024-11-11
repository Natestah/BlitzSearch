using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Blitz.AvaloniaEdit.ViewModels;

namespace Blitz.AvaloniaEdit.Views;

public partial class BlitzFileTab : UserControl
{
    public BlitzFileTab()
    {
        InitializeComponent();
    }

    private void Tab_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Middle)
        {
            if ((sender as Control)?.DataContext is not BlitzDocument document) return;
            if (DataContext is not BlitzEditorViewModel blitzEditorViewModel) return;
            var preferredIndex = blitzEditorViewModel.OpenedFiles.IndexOf(document);
            
            blitzEditorViewModel.OpenedFiles.Remove( document );
            blitzEditorViewModel.SelectedFiles.Remove(document);
            
            
            if (blitzEditorViewModel.OpenedFiles.Count == 0)
            {
                var untitledFile = new BlitzDocument(BlitzDocument.DocumentType.Untitled, "Untitled");
                blitzEditorViewModel.OpenedFiles.Add(untitledFile);
                blitzEditorViewModel.SelectedFiles.Add(untitledFile);
            }
            else
            {
                preferredIndex = Math.Clamp(preferredIndex, 0, blitzEditorViewModel.OpenedFiles.Count-1);
                if (blitzEditorViewModel.SelectedFiles.Count == 0)
                {
                    blitzEditorViewModel.SelectedFiles.Add(blitzEditorViewModel.OpenedFiles[preferredIndex]);
                }
            }
        }
    }
}
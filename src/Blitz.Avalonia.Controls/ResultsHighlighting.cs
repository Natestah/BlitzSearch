using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using Blitz.Avalonia.Controls.ViewModels;
using Blitz.AvaloniaEdit;
using TextMateSharp.Themes;
using FontStyle = Avalonia.Media.FontStyle;

namespace Blitz.Avalonia.Controls;

/// <summary>
/// Helper for TextmateSharp.
/// </summary>
public class ResultsHighlighting
{
    private  MainWindowViewModel _mainWindowViewModel;
    public ResultsHighlighting(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
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
        if (_mainWindowViewModel == null) return [];
        //This needs work in EditorViewModel..
        var options = _mainWindowViewModel.EditorViewModel.ThemeViewModel.RegistryOptions.BaseOptions;
        var themeVM = _mainWindowViewModel.EditorViewModel.ThemeViewModel;
        return options != null ? TextMateInlineGenerator.GetInlinesFromTextMateSharp( themeVM.Theme, themeVM.RegistryOptions.BaseOptions,themeVM.Registry, line, fileNameForContext, opacity) : [];
    }
}
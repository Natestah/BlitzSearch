using System;
using System.Linq;
using System.Collections.ObjectModel;
using AvaloniaEdit.TextMate;
using Avalonia.Media;
using Blitz.AvaloniaEdit.Models;
using TextMateSharp.Grammars;
using ReactiveUI;



namespace Blitz.AvaloniaEdit.ViewModels;

public class MainWindowViewModel : ViewModelBase
{

    public BlitzEditorViewModel EditorViewModel { get; }= new BlitzEditorViewModel();

  
}

using System.IO;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Blitz.Interfacing;
using ReactiveUI;
using Brushes = Avalonia.Media.Brushes;

namespace Blitz.Avalonia.Controls.ViewModels;

public class FileNameResultViewModel : ViewModelBase, IResultCopiable
{
    public string FileName => _fileNameResult.FileName;

    //private InlineCollection? _fileNameWithHighlights;
    private readonly FileNameResult _fileNameResult;
    private bool _isUpdated;

    private MainWindowViewModel _mainWindowViewModel;

    public FileNameResult FileNameResult => _fileNameResult;
    public FileNameResultViewModel(MainWindowViewModel mainWindowViewModel, FileNameResult fileNameResult)
    {
        _fileNameResult = fileNameResult;
        _mainWindowViewModel = mainWindowViewModel;
    }

    public MainWindowViewModel MainWindowViewModel => _mainWindowViewModel;

    public bool GetDebugMessage(out string message)
    {
        if (string.IsNullOrEmpty(_fileNameResult.DebugInformation))
        {
            message = null!;
            return false;
        }

        message = _fileNameResult.DebugInformation;
        return true;
    }

    public InlineCollection FileNameWithHighlights
    {
        get
        {
            // if (_fileNameWithHighlights != null)
            // {
            //     return _fileNameWithHighlights;
            // }

            var inlineCollection = new InlineCollection();

            // per https://learn.microsoft.com/en-us/dotnet/api/system.io.path.directoryseparatorchar?view=net-8.0
            // The example displays the following output when run on a Windows system:
            //    Path.DirectorySeparatorChar: '\'
            //    Path.AltDirectorySeparatorChar: '/'
            //    Path.PathSeparator: ';'
            //    Path.VolumeSeparatorChar: ':'
            //    Path.GetInvalidPathChars:
    
            // The example displays the following output when run on a Linux system:
            //    Path.DirectorySeparatorChar: '/'
            //    Path.AltDirectorySeparatorChar: '/'
            //    Path.PathSeparator: ':'
            //    Path.VolumeSeparatorChar: '/'
            //    Path.GetInvalidPathChars:

            //given "c:\directory\files.txt
            string fileName = FileName;
            int startingIndex = 0;

            var foreground = Brushes.White;
            
            for (int workingIndex = 0; workingIndex < fileName.Length; workingIndex++)
            {
                var character = fileName[workingIndex];
                if (character == Path.VolumeSeparatorChar)
                {
                    //Fist C in c:\directory\files.txt
                    var runText = fileName.Substring(startingIndex, workingIndex - startingIndex);
                    var run = new Run(runText){Foreground = foreground};
                    inlineCollection.Add(run);

                    // this character (Path.VolumeSeparatorChar)
                    inlineCollection.Add(new Run(character.ToString()){Foreground = foreground});
                    workingIndex++;
                    startingIndex = workingIndex;
                }
                else if (character == Path.DirectorySeparatorChar || character == Path.AltDirectorySeparatorChar)
                {
                    //files in c:\directory\files.txt
                    var runText = fileName.Substring(startingIndex, workingIndex - startingIndex);
                    inlineCollection.Add(new Run(runText){Foreground = foreground});

                    // this character
                    inlineCollection.Add(new Run(character.ToString()){Foreground = foreground});
                    startingIndex = workingIndex + 1;
                }
            }

            //files.txt in c:\directory\files.txt gets different color
            var fileNameText = fileName.Substring(startingIndex, fileName.Length - startingIndex);
            inlineCollection.Add(new Run(fileNameText) { Foreground = Brushes.Yellow,FontWeight = FontWeight.Bold});

            if (_fileNameResult?.BlitzMatches == null)
            {
                return inlineCollection;
            }
            var states = _mainWindowViewModel.ResultsHighlighting.GetCharStatesFromInlines(inlineCollection, _fileNameResult.FileName);
            var matchHighlighter = new MatchHighlighter(states,_fileNameResult.BlitzMatches, _fileNameResult.FileName, false);
            return matchHighlighter.GetInlines();
        }

    }

    public string CopyText => _fileNameResult.FileName;

    public bool IsUpdated
    {
        get => _isUpdated;
        set => this.RaiseAndSetIfChanged( ref _isUpdated, value);
    }
}
using Avalonia.Controls.Documents;
using Blitz.Interfacing;
using ReactiveUI;

namespace Blitz.Avalonia.Controls.ViewModels;

public class ContentResultViewModel(MainWindowViewModel mainWindowViewModel, FileContentResult fileContentResultResult, FileNameResult fileNameResult)
    :  ViewModelBase,IResultCopiable
{
    public FileContentResult FileContentResult => fileContentResultResult;
    public FileNameResult FileNameResult => fileNameResult;

    public MainWindowViewModel MainWindowViewModel => mainWindowViewModel;

    public double LineHeight => Configuration.Instance.EditorConfig.LineSpacing;
  
    public InlineCollection ContentWithHighlights
    {
        get
        {
            if (fileContentResultResult.CapturedContents == null)
            {
                return [new Run("<NULL FILE CONTENTS>")];
            }

            bool replacing = fileContentResultResult.Replacing;
            string renderedContents = fileContentResultResult.CapturedContents;
            
            //Sometimes files can show up in their entirety on one line.
            //this can over-burden things. (huge performance hitch and horizontal scrolling).  simply truncate here
            const int maxLineDisplayChars = 1024; //Todo: Optional?
            
            
            if (!string.IsNullOrEmpty(fileContentResultResult.ReplacedContents))
            {
                renderedContents = fileContentResultResult.ReplacedContents;
            }
            
            bool largeLine = renderedContents.Length > maxLineDisplayChars;
            
            
            string displayContents = largeLine ? renderedContents.Substring(0, maxLineDisplayChars): renderedContents;

            // given the lines contents and the filename.. come up with some highlights.
            InlineCollection inlineCollection = ResultsHighlighting.Instance.GetInlinesFromTextMateSharp(displayContents, fileNameResult.FileName);


            var states = ResultsHighlighting.Instance.GetCharStatesFromInlines(inlineCollection, displayContents);
            
            var matchHighlighter = new MatchHighlighter(states,fileContentResultResult.BlitzMatches, displayContents, replacing);

            return matchHighlighter.GetInlines();
        }
    }


    public void RefreshPropertyVisuals()
    {
        this.RaisePropertyChanged(nameof(ContentWithHighlights));
        this.RaisePropertyChanged(nameof(LineHeight));
    }

    public string CopyText => fileContentResultResult.CapturedContents ?? string.Empty;
}
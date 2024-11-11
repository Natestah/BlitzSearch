using System.Collections.ObjectModel;
using ReactiveUI;

namespace Blitz.Goto.Demo.ViewModels;

public class GotoEditorViewModel(GotoEditor gotoEditor) : ViewModelBase
{
    public GotoEditor GotoEditor => gotoEditor;

    public string Title
    {
        get => gotoEditor.Title;
        set
        {
            gotoEditor.Title = value;
            this.RaisePropertyChanged();
        }
    }

    public string Executable
    {
        get => gotoEditor.Executable;
        set
        {
            gotoEditor.Executable = value;
            this.RaisePropertyChanged();
        }
    }

    public string ExecutableWorkingDirectory
    {
        get => gotoEditor.ExecutableWorkingDirectory;
        set
        {
            gotoEditor.ExecutableWorkingDirectory = value;
            this.RaisePropertyChanged();
        }
    }

    public string CodeExecute
    {
        get => gotoEditor.CodeExecute;
        set
        {
            gotoEditor.CodeExecute = value;
            this.RaisePropertyChanged();
        }
    }
    
    public string RunningProcessName
    {
        get => gotoEditor.RunningProcessName;
        set
        {
            gotoEditor.RunningProcessName = value;
            this.RaisePropertyChanged();
        }
    }

    public string CommandLine
    {
        get => gotoEditor.Arguments;
        set
        {
            gotoEditor.Arguments = value;
            this.RaisePropertyChanged();
        }
    }

    public string Notes
    {
        get => gotoEditor.Notes;
        set
        {
            gotoEditor.Notes = value;
            this.RaisePropertyChanged();
        }
    }

    private readonly ObservableCollection<ArgumentAliasViewModel> _suggestedNames = [];

    public ObservableCollection<ArgumentAliasViewModel> SuggestedNames
    {
        get
        {
            if (_suggestedNames.Count != 0)
            {
                return _suggestedNames;
            }
            foreach (var argumentAlias in GotoArgumentConverter.GetArgumentAliases(null))
            {
                _suggestedNames.Add(new ArgumentAliasViewModel(argumentAlias));
            }
            return _suggestedNames;
        }
    }
}
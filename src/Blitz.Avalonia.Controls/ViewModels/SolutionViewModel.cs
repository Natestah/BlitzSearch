using System;
using System.Collections.ObjectModel;
using ReactiveUI;

namespace Blitz.Avalonia.Controls.ViewModels;
using Interfacing;

public class SolutionViewModel : ViewModelBase
{
    private SolutionExport _export;
    private ProjectViewModel? _selectedProject;
    private MainWindowViewModel _mainWindowViewModel;
    private ObservableCollection<string> _activeFiles = [];
    public MainWindowViewModel MainWindowVM => _mainWindowViewModel;

    public ObservableCollection<ProjectViewModel> Projects { get; } = [];

    public ProjectViewModel? SelectedProject
    {
        get => _selectedProject;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedProject, value);
            _mainWindowViewModel.RaiseSolutionPropertyChanged();
        }
    }
    
    public ObservableCollection<string> ActiveFiles
    {
        get => _activeFiles;
        set => this.RaiseAndSetIfChanged(ref _activeFiles, value);
    }

    public string Title => _export?.Name?? "Untitled";

    
    /// <summary>
    /// Try to Translate Long paths to something shorter and presentable.
    /// </summary>
    public string DisplayTitle
    {
        get
        {
            try
            {
                string path = System.IO.Path.GetFileName(Title);
                if (path.Length == 0)
                {
                    return System.IO.Path.GetDirectoryName(Title) ?? Title;
                }
                return path;
            }
            catch (Exception)
            {
                //Any problems just return the title..
                return Title;
            }
        }
    }


    public SolutionExport Export => _export;
    public bool ISVSCodeSolution { get; set; } 

    public SolutionViewModel(SolutionExport export, MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _export = export;
        foreach (var project in export.Projects)
        {
            Projects.Add(new ProjectViewModel(project));
        }
        if (Projects.Count > 0)
        {
            SelectedProject = Projects[0];
        }
    }
}

public class ProjectViewModel : ViewModelBase
{
    private Project _project;
    public ProjectViewModel(Project project)
    {
        _project = project;
    }
    public string Name => _project.Name ?? "Untitled";
    
    /// <summary>
    /// Try to Translate Long paths to something shorter and presentable.
    /// </summary>
    public string DisplayTitle
    {
        get
        {
            try
            {
                string path = System.IO.Path.GetFileName(Name);
                if (path.Length == 0)
                {
                    return System.IO.Path.GetDirectoryName(Name) ?? Name;
                }
                return path;
            }
            catch (Exception)
            {
                //Any problems just return the Name..
                return Name;
            }
        }
    }

}

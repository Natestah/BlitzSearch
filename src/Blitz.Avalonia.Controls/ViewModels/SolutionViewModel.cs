using System;
using System.Threading;
using System.IO;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AvaloniaEdit.Utils;
using ReactiveUI;

namespace Blitz.Avalonia.Controls.ViewModels;
using Interfacing;

public class SolutionViewModel : ViewModelBase
{
    private SolutionExport? _export;
    private ProjectViewModel? _selectedProject;
    private MainWindowViewModel _mainWindowViewModel;
    private ObservableCollection<string> _activeFiles = [];
    private readonly ObservableCollection<ProjectViewModel> _projects = [];
    public MainWindowViewModel MainWindowVM => _mainWindowViewModel;

    public ObservableCollection<ProjectViewModel> Projects
    {
        get
        {
            InitExport();
            return _projects;
        }
    }
    
    

    public ProjectViewModel? SelectedProject
    {
        get
        {
            InitExport();
            return _selectedProject;
        }
        set
        {
            InitExport();
            if (value != null)
            {
                Configuration.Instance.SolutionProjectSelection[SolutionIdentity.Identity] = value.Name; ;
            }
            this.RaiseAndSetIfChanged(ref _selectedProject, value);
            _mainWindowViewModel.RaiseSolutionPropertyChanged();
        }
    }

    public void RestoreActiveFilesFromIPC()
    {
        ActiveFiles.Clear();
        if (!PoorMansIPC.Instance.GetCommandPathFromSolutionID(this.SolutionIdentity,"VS_ACTIVE_FILES", out var path))
        {
            return;
        }
        var text = File.ReadAllText(path);
        var activeFileList = JsonSerializer.Deserialize(text,JsonContext.Default.ActiveFilesList);
        if (activeFileList != null)
        {
           
            ActiveFiles.AddRange(activeFileList.ActiveFiles);
        }

    }

    public ObservableCollection<string> ActiveFiles
    {
        get
        {
            if (_activeFiles == null || _activeFiles.Count == 0)
            {
                
            }
            return _activeFiles;
        }
        set => this.RaiseAndSetIfChanged(ref _activeFiles, value);
    }

    public string Title => SolutionIdentity.Title;


    /// <summary>
    /// Try to Translate Long paths to something shorter and presentable.
    /// </summary>
    public string DisplayTitle => $"{Title}.sln";
    
    
    private static string GetSolutionCacheFile(string solutionName)
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var specificFolder = Path.Combine(folder, "NathanSilvers", "SolutionCache");
        Directory.CreateDirectory(specificFolder);
        return Path.Combine(specificFolder, $"{solutionName}.txt"); 
    }

    private SolutionExport? GetSolutionExport()
    {
        return !PoorMansIPC.Instance.GetSolutionRecord(SolutionIdentity, out var cacheFile) 
            ? null 
            : JsonSerializer.Deserialize(File.ReadAllText(cacheFile), JsonContext.Default.SolutionExport);
    }


    public void RestoreSelectionFromConfiguration()
    {
        bool hasProject = Configuration.Instance.SolutionProjectSelection.TryGetValue(SolutionIdentity.Identity, out var selectedProject);
        bool didSelect = false;
        foreach (var projectVM in Projects)
        {
            if (hasProject && projectVM.Name.Equals(selectedProject, StringComparison.OrdinalIgnoreCase))
            {
                SelectedProject = projectVM;
                didSelect = true;
            }
        }
        if (!didSelect && Projects.Count > 0)
        {
            SelectedProject = Projects[0];
        }

    }
    

    private void UpdateProjectViewModels(SolutionExport export)
    {
        foreach (var project in export.Projects)
        {
            var projectVM =new ProjectViewModel(project);
            Projects.Add(projectVM);
        }
        RestoreSelectionFromConfiguration();
    }

    private void InitExport()
    {
        if (_export != null)
        {
            return;
        }
        
        _export = GetSolutionExport();
        if (_export != null)
        {
            UpdateProjectViewModels(_export);
        }
    }

    public SolutionExport? Export
    {
        get
        {
            InitExport();
            return _export;
        }
        set
        {
            _export = value;
            if (_export != null)
            {
                UpdateProjectViewModels(_export);
            }
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(SelectedProject));
        }
    }
    
    public SolutionID SolutionIdentity { get; }

    public SolutionViewModel(SolutionID solutionId, MainWindowViewModel mainWindowViewModel)
    {
        SolutionIdentity = solutionId;
        _mainWindowViewModel = mainWindowViewModel;
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

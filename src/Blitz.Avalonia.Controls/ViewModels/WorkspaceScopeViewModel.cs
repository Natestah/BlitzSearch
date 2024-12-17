using System;
using System.IO;
using System.Text.Json;
using Blitz.Interfacing;
using ReactiveUI;

namespace Blitz.Avalonia.Controls.ViewModels;

public class WorkspaceScopeViewModel : ViewModelBase
{

    private FolderWorkspace? _workspace;
    public MainWindowViewModel MainWindowVM { get; }

    public FolderWorkspace? WorkspaceExport
    {
        get
        {
            InitExport();
            return _workspace;
        }
        set
        {
            _workspace = value;
            this.RaisePropertyChanged();
        }
    }

    void InitExport()
    {
        if (_workspace != null)
        {
            return;
        }

        _workspace = GetWorkspaceExportFromLastPluginCommand();
        
    }
    
    public SolutionID SolutionIdentity { get; }

    
    private FolderWorkspace? GetWorkspaceExportFromLastPluginCommand()
    {
        if (IsSublimeTextUnifiedExport)
        {
            //Sublime Text operates under one executable so it's export is unified.. 
            throw new NotImplementedException("Sublime Text GetWorkspaceExportFromLastPluginCommand is unsupported.");
        }
        
        return !PluginCommands.Instance.GetCommandPathFromSolutionId(SolutionIdentity, PluginCommands.VisualStudioCodeWorkspaceUpdate, out var cacheFile) 
            ? null 
            : JsonSerializer.Deserialize(File.ReadAllText(cacheFile), JsonContext.Default.FolderWorkspace);
    }
    
    public bool IsSublimeTextUnifiedExport { get; }

    public bool IsWorkspace => WorkspaceExport?.Name != null;
    public string Title => WorkspaceExport?.Name ?? "Untitled Workspace";

    public string ExecutableIconHint => WorkspaceExport == null ? "" : WorkspaceExport.ExeForIcon;

    public WorkspaceScopeViewModel(MainWindowViewModel mainWindowViewModel, SolutionID solutionId, bool isSublimeTextUnifiedExport= false)
    {
        SolutionIdentity = solutionId;
        MainWindowVM = mainWindowViewModel;
        IsSublimeTextUnifiedExport = isSublimeTextUnifiedExport;
    }
    
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

}
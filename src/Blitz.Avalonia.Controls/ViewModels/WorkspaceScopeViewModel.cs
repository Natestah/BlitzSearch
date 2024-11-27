using System;
using Blitz.Interfacing;

namespace Blitz.Avalonia.Controls.ViewModels;

public class WorkspaceScopeViewModel : ViewModelBase
{
    public MainWindowViewModel MainWindowVM { get; }
    
    public WorkspaceExport WorkspaceExport { get; set; }

    public string Title => WorkspaceExport.Name;

    public string ExecutableIconHint { get; set; } = "";
    public WorkspaceScopeViewModel(MainWindowViewModel mainWindowViewModel, WorkspaceExport workspaceExport)
    {
        MainWindowVM = mainWindowViewModel;
        WorkspaceExport = workspaceExport;
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
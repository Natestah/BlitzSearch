using System;
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Blitz.Goto;
using ReactiveUI;
using System.IO;
using System.Linq;
namespace Blitz.Avalonia.Controls.ViewModels;

public class GotoEditorViewModel(GotoEditor gotoEditor) : ViewModelBase
{
    public GotoEditor GotoEditor => gotoEditor;

    public bool ReadOnly
    {
        get => _isReadonly;
        set
        {
            _isReadonly = value;
            this.RaisePropertyChanged();
        }
    }

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
            UpdatePreview();
            this.RaisePropertyChanged();
        }
    }

    public string ExecutableIconHint
    {
        get => gotoEditor.ExecutableIconHint;
        set
        {
            gotoEditor.ExecutableIconHint = value;
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
    public string ExecutableWorkingDirectory
    {
        get => gotoEditor.ExecutableWorkingDirectory;
        set
        {
            gotoEditor.ExecutableWorkingDirectory = value;
            UpdatePreview();
            this.RaisePropertyChanged();
        }
    }

    public string CommandLine
    {
        get => gotoEditor.Arguments;
        set
        {
            gotoEditor.Arguments = value;
            UpdatePreview();
            this.RaisePropertyChanged();
        }
    }

    public string Notes
    {
        get => gotoEditor.Notes;
        set
        {
            gotoEditor.Notes = value;
            UpdatePreview();
            this.RaisePropertyChanged();
        }
    }

    private void UpdatePreview()
    {
        this.RaisePropertyChanged(nameof(PreviewCommandLine));
    }

    public string PreviewCommandLine
    {
        get
        {
            var gotoAction = new GotoAction(gotoEditor);
            var directive = new GotoDirective(@"c:\sample.txt", 3, 1);
            var startInfo =  gotoAction.GetStartinfoForDirective(directive, true);
            return $"{startInfo.FileName} {startInfo.Arguments}";
        }
    }

    private readonly ObservableCollection<ArgumentAliasViewModel> _suggestedNames = [];
    private bool _isReadonly;

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

    public bool RunOpenExplorerOnObject(object? controlDataContext)
    {
        if (!GetFileGotoInfo(controlDataContext, out var fileToGoto, out _, out _))
        {
            return false;
        }

        try
        {
            Process.Start("explorer.exe", $"/select,\"{fileToGoto}\"");
            
        }
        catch (Exception)
        {
            //It's ok.. 
        }

        return true;
    }
    public bool RunOpenCmdOnObject(object? controlDataContext)
    {
        if (!GetFileGotoInfo(controlDataContext, out var fileToGoto, out _, out _))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo("cmd.exe")
            {
                WorkingDirectory = Path.GetDirectoryName(fileToGoto),
                UseShellExecute = true,
                CreateNoWindow = false
            };

            Process.Start(startInfo);
            
        }
        catch (Exception)
        {
            //It's ok.. 
        }

        return true;
    }

    private bool GetFileGotoInfo(object? controlDataContext, out string fileToGoto, out int line, out int column)
    {
        fileToGoto = string.Empty;
        line = 1;
        column = 1;

        if (controlDataContext is ContentResultViewModel contentResultViewModel)
        {
            fileToGoto = contentResultViewModel.FileNameResult.FileName;
            line = contentResultViewModel.FileContentResult.LineNumber;
            if (contentResultViewModel.FileContentResult.BlitzMatches.FirstOrDefault() is { } blitzMatch)
            {
                column = blitzMatch.MatchIndex;
            }
        }
        else if (controlDataContext is FileNameResultViewModel fileNameResultView)
        {
            fileToGoto = fileNameResultView.FileName;
        }
        else if (controlDataContext is RobotFileSummaryViewModel robotFileSummaryViewModel)
        {
            string fileText = robotFileSummaryViewModel.GetCsvReport();
            string pathToWrite = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NathanSilvers",$"RoboFileSummary{DateTime.Now:yy-MM-dd-hh-mm-ss}.csv");

            try
            {
                File.WriteAllText(pathToWrite,fileText);
            }
            catch (Exception)
            {
                return false;
            }
            
            new Process
            {
                StartInfo = new ProcessStartInfo(pathToWrite)
                {
                    UseShellExecute = true
                }
            }.Start();
            return true;
        }
        return true;
    }

    public bool RunGoto(bool preview, string fileToGoto, int line, int column, out string errorMessage)
    {
        var gotoAction = new GotoAction(GotoEditor);
        var directive = new GotoDirective(fileToGoto, line, column);
        errorMessage = string.Empty;
        try
        {
            gotoAction.ExecuteGoto(directive, preview);
        }
        catch (FileNotFoundException)
        {
            var builder = new StringBuilder();
            builder.AppendLine("**********************");
            builder.AppendLine("***   Goto Failure ***");
            builder.AppendLine("**********************");
            builder.AppendLine("Can't find file: ");
            builder.AppendLine(GotoEditor.Executable);
            builder.AppendLine("check the working directory in the goto editor settings:");
            builder.AppendLine(string.IsNullOrEmpty(GotoEditor.ExecutableWorkingDirectory)
                ? "Not found in %path% environment"
                : GotoEditor.ExecutableWorkingDirectory);

            errorMessage = builder.ToString();
            return false;
        }

        return true;

    }
    
    public bool RunTotoOnObjectGoto(object? controlDataContext, bool preview, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (!GetFileGotoInfo(controlDataContext, out var fileToGoto, out var line ,out var column))
        {
            return false;
        }
        return RunGoto(preview, fileToGoto, line, column, out errorMessage);
    }

    public bool EditorExists()
    {
        return new GotoAction(GotoEditor).CanDoAction();
    }
}
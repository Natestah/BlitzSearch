using System.Diagnostics;
using System.Text;

namespace Blitz.Goto;

public class GotoAction(GotoEditor gotoEditor)
{
    
    private bool LocateDirectoryFromSystemPath(string inputWorkingDirectory, string exeName, out string workingDirectory)
    {
        workingDirectory = null;
        
        if (!string.IsNullOrEmpty(inputWorkingDirectory))
        {
            return false;
        }

        string[]? pathVars;
        try
        {
            pathVars = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.TrimEntries);
        }
        catch
        {
            return false;
        }
        // search for it in %path% environment variable.
        foreach (var path in pathVars)
        {
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            try
            {
                var test = Path.Combine(path, exeName);
                if (!File.Exists(test))
                {
                    continue;
                }
            }
            catch (Exception ex)
            {
                continue;
            }
            workingDirectory = path;
            return true;
        }
        return false;
    }


    private void executeGotoByPoorMansIPC( GotoDirective gotoDirective, string ipcIdentity, string pathSeperator =";", bool preview = true)
    {
        var appdata = Environment.ExpandEnvironmentVariables("%appdata%");
        string path = Path.Combine(appdata, "NathanSilvers", "POORMANS_IPC");
        Directory.CreateDirectory(path);
        string previewSuffix = preview ? "_PREVIEW": string.Empty;
        string file = Path.Combine(path, $"{ipcIdentity}{previewSuffix}.txt");
        File.WriteAllText(file, $"{gotoDirective.FileName}{pathSeperator}{gotoDirective.Line}{pathSeperator}{gotoDirective.Column}");
    }

    bool ExecutableBootRequired()
    {
        if (!string.IsNullOrEmpty(gotoEditor.RunningProcessName))
        {
            return Process.GetProcessesByName(gotoEditor.RunningProcessName.Replace(".exe","")).Length == 0;
        }
        
        if (!string.IsNullOrEmpty(gotoEditor.Executable)  )
        {
            if (gotoEditor.Executable.ToLower().Contains(".exe"))
            {
                return Process.GetProcessesByName(gotoEditor.Executable.Replace(".exe","")).Length == 0;
            }
        }

        return true;
    }
    
    public void ExecuteGoto( GotoDirective gotoDirective, bool preview = false)
    {
        bool runExecutable = true;
        if (!string.IsNullOrEmpty(gotoEditor.CodeExecute))
        {
            switch (gotoEditor.CodeExecute)
            {
                case "VsCodeGoto":
                    executeGotoByPoorMansIPC(gotoDirective, "VS_CODE_GOTO", ";", preview);
                    runExecutable = ExecutableBootRequired();
                    break;
                case "VisualStudioPlugin":
                    executeGotoByPoorMansIPC(gotoDirective, "VISUAL_STUDIO_GOTO", ",", preview);
                    runExecutable = false;
                    break;
                case "BlitzEdit":
                    executeGotoByPoorMansIPC(gotoDirective, "BLITZ_EDIT_GOTO", ";", preview);
                    runExecutable = false;
                    break;
            }
            
        }

        if (!runExecutable)
        {
            return;
        }
        var startInfo = GetStartinfoForDirective(gotoDirective);
        Process.Start(startInfo);
    }

    private bool AnySystemPathContains(string containsCheck)
    {
        try
        {
            // search for it in %path% environment variable.
            foreach (var path in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';',
                         StringSplitOptions.TrimEntries))
            {
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (path.Contains(containsCheck))
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
    
    public bool CanDoAction()
    {
        switch (gotoEditor.CodeExecute)
        {
            case "VisualStudioPlugin":
                return AnySystemPathContains("Visual Studio");
            case "VsCodeGoto":
                return AnySystemPathContains("Microsoft VS Code");
        }
        return LocateExecutable(out _, out _);
    }

    private bool LocateExecutable(out string workingDirectory, out string fileName)
    {
        workingDirectory = Environment.ExpandEnvironmentVariables(gotoEditor.ExecutableWorkingDirectory);
        if (LocateDirectoryFromSystemPath(workingDirectory,gotoEditor.Executable, out var foundPath))
        {
            workingDirectory = foundPath;
        }

        if (GotoJetbrainsIDE.Instance.IsMatchForWorkingDirectory(workingDirectory)
            &&  GotoJetbrainsIDE.Instance.GetWorkingDirectory(workingDirectory, gotoEditor.Executable, out var matched))
        {
            workingDirectory = matched!;
        }
        
        fileName = Path.Combine(workingDirectory, gotoEditor.Executable);
        return File.Exists(fileName);
    }

    public ProcessStartInfo GetStartinfoForDirective(GotoDirective gotoDirective, bool forPreview = false)
    {
        if(!LocateExecutable(out var workingDirectory, out var fileName)&& !forPreview)
        {
            throw new FileNotFoundException("Goto Editor not found.", fileName);
        }
        
        return new ProcessStartInfo(fileName)
        {
            CreateNoWindow = true,
            WorkingDirectory =  workingDirectory,
            Arguments = new GotoArgumentConverter(gotoDirective).ConvertArguments(gotoEditor.Arguments)
        };
    }

}
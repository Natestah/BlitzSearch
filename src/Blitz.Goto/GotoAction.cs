using System.Diagnostics;
using System.Text;

namespace Blitz.Goto;

public class GotoAction(GotoEditor gotoEditor)
{
    
    private bool LocateDirectoryFromSystemPath(string inputWorkingDirectory, string exeName, out string workingDirectory)
    {
        workingDirectory = string.Empty;
        
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
            catch (Exception)
            {
                continue;
            }
            workingDirectory = path;
            return true;
        }
        return false;
    }

    public static string GetFolder()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var specificFolder = Path.Combine(folder, "NathanSilvers", "POORMANS_IPC");
        Directory.CreateDirectory(specificFolder);
        return specificFolder;
    }
    
    private void executeLabeledGotoCommand( GotoDirective gotoDirective, string ipcIdentity, string pathSeperator =";", bool preview = true)
    {
        string path = GetFolder();
        string previewSuffix = preview ? "_PREVIEW": string.Empty;
        string file = Path.Combine(path, $"{ipcIdentity}{previewSuffix}.txt");
        File.WriteAllText(file, $"{gotoDirective.FileName}{pathSeperator}{gotoDirective.Line}{pathSeperator}{gotoDirective.Column}");
    }


    private void executeGotoWithJson( GotoDirective gotoDirective, string ipcIdentity, bool preview = true)
    {
        string path = GetFolder();
        string previewSuffix = preview ? "_PREVIEW_JSON": "_JSON";
        string file = Path.Combine(path, $"{ipcIdentity}{previewSuffix}.txt");
        string contents = System.Text.Json.JsonSerializer.Serialize(gotoDirective, JsonContext.Default.GotoDirective);
        for (int i = 0; i < 3; i++)
        {
            try
            {
                File.WriteAllText(file, contents);
                break;
            }
            catch (Exception)
            {
                Thread.Sleep(50);
            }
        }
    }

    private void executeGotoWithJsonAndNamedParameters( GotoDirective gotoDirective, string ipcIdentity, string slnParameter, bool preview = true)
    {
        string path = GetFolder();
        string previewSuffix = preview ? "_PREVIEW_JSON": "_JSON";
        string file = Path.Combine(path, $"{ipcIdentity}{previewSuffix},{slnParameter}.txt");
        string contents = System.Text.Json.JsonSerializer.Serialize(gotoDirective, JsonContext.Default.GotoDirective);
        for (int i = 0; i < 3; i++)
        {
            try
            {
                File.WriteAllText(file, contents);
                break;
            }
            catch (Exception)
            {
                Thread.Sleep(50);
            }
        }
    }
    private void executeGotoWithSolutionId( GotoDirective gotoDirective, string commandIdentity, bool preview = true)
    {
        string title = gotoDirective.Title ?? string.Empty;
        string identity = gotoDirective.SolutionId ?? string.Empty;
        string path = GetFolder();
        string previewSuffix = preview ? "_PREVIEW_JSON": "_JSON";
        string file = Path.Combine(path, $"{commandIdentity}{previewSuffix},{title},{identity}.txt");
        string contents = System.Text.Json.JsonSerializer.Serialize(gotoDirective, JsonContext.Default.GotoDirective);
        for (int i = 0; i < 3; i++)
        {
            try
            {
                File.WriteAllText(file, contents);
                break;
            }
            catch (Exception)
            {
                Thread.Sleep(50);
            }
        }
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
                case CodeExecuteNames.NVim:
                    executeGotoWithSolutionId(gotoDirective, "NVIM_GOTO", preview);
                    break;
                case CodeExecuteNames.VSCode:
                case CodeExecuteNames.Cursor:
                case CodeExecuteNames.Windsurf:
                    executeGotoWithSolutionId(gotoDirective, "VS_CODE_GOTO", preview);
                    runExecutable = ExecutableBootRequired();
                    break;
                case CodeExecuteNames.VisualStudio:
                    //Todo: visit Visual Studio Plugin and use executeGotoWithSolutionId, not super important but would like to reduce edge case file contention.
                    executeGotoWithJsonAndNamedParameters(gotoDirective, "VISUAL_STUDIO_GOTO",  Path.GetFileNameWithoutExtension(gotoDirective.SolutionName) ?? string.Empty, preview);
                    runExecutable = false;
                    break;
                case CodeExecuteNames.BlitzEdit:
                    executeLabeledGotoCommand(gotoDirective, "BLITZ_EDIT_GOTO", ";", preview);
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
            case CodeExecuteNames.NVim:
                return AnySystemPathContains("Neovim");
            case CodeExecuteNames.VisualStudio:
                return AnySystemPathContains("Visual Studio");
            case CodeExecuteNames.VSCode:
                return AnySystemPathContains("Microsoft VS Code");
            case CodeExecuteNames.Cursor:
                string environmentVariable = Environment.ExpandEnvironmentVariables("%appdata%\\..\\Local\\Programs\\cursor\\Cursor.exe");
                return Path.Exists(environmentVariable);
            case CodeExecuteNames.Windsurf:
                string windsurfEnv = Environment.ExpandEnvironmentVariables("%appdata%\\..\\Local\\Programs\\windsurf\\Windsurf.exe");
                return Path.Exists(windsurfEnv);
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
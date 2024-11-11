namespace Blitz.Goto;


/// <summary>
/// JetBrains Rider installs post a significant challenge to the simple list of Editors and their installs.
/// where path looks like 'C:\Program Files\jetbrains\JetBrains Rider 2023.3.3\bin' We simply need some code to discover the latest and greatest.
/// </summary>
public class GotoJetbrainsIDE
{
    public static GotoJetbrainsIDE Instance = new GotoJetbrainsIDE();

    private const string JetBrainsPrefix = "%JETBRAINS_WORKING_DIR%";
    public bool IsMatchForWorkingDirectory(string workingDirectory) => workingDirectory.StartsWith(JetBrainsPrefix);

    public bool GetWorkingDirectory(string workingInput, string exe, out string? workingDirectory)
    {
        string subDir = workingInput.Replace(JetBrainsPrefix, "");
        Jetbrains.GetWorkingDirectory(subDir, out workingDirectory, exe);
        return workingDirectory != null;
    }
}
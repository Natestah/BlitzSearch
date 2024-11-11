namespace Blitz.Goto;

public static class Jetbrains
{
    
    public static void GetWorkingDirectory(string path_prefix, out string? workingDirectory, string binName)
    {
        workingDirectory = null;
        var environments = new[] { "%programfiles%\\jetbrains", "%localappdata%\\Programs" };
        foreach (var envVar in environments)
        {
            string path = Environment.ExpandEnvironmentVariables(@$"{envVar}");
            if (!Directory.Exists(path))
            {
                continue;
            }
            DateTime latestAndGreatest = DateTime.MinValue;
            foreach (var subDirectory in Directory.EnumerateDirectories(path).Where((p)=>p.Contains(path_prefix) ))
            {
                string? searchDirectory = Path.Combine(subDirectory, "bin"); 
                string searchPath = Path.Combine(searchDirectory, binName);
                if (File.Exists(searchPath))
                {
                    var age = File.GetLastWriteTimeUtc(searchPath);
                    if (age > latestAndGreatest)
                    {
                        workingDirectory = searchDirectory;
                        latestAndGreatest = age;
                    }
                }
            }
        }
        
    }
}
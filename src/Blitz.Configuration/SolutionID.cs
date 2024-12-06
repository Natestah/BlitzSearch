using Blitz.Interfacing;

namespace Blitz;


/// <summary>
/// Identify a Solution by its short title (GetFileNameWithoutExtension) and it's MD5 Identity. 
/// </summary>
public class SolutionID: IEquatable<SolutionID>
{
    public string Title { get; set; } = string.Empty;
    public string Identity { get; set; } = string.Empty;

    public static SolutionID CreateFromSolutionPath(string solutionPath) =>
        new()
        {
            Title = Path.GetFileNameWithoutExtension(solutionPath) ,
            Identity = SearchExtensionCache.Md5(solutionPath)
        };
    
    public static SolutionID None = new SolutionID{Title = "None", Identity = "None"};
    public bool Equals(SolutionID? other)
    {
        return other?.Title == Title && other.Identity == Identity;
    }
}
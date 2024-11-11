namespace Blitz.Interfacing;
using MessagePack;


/// <summary>
/// General Payload for a search task, Typically would be a single file, with Content results.
/// Can come with Exceptions ( hopefully NOT ) and can comeback with helpful "Missing Requirements"
/// Suggestions
/// </summary>
[MessagePackObject]
public class SearchTaskResult : MessageWithIdentity
{
    /// <summary>
    /// Name of file with Results
    /// </summary>
    [Key(nameof(FileNames))]
    public List<FileNameResult> FileNames { get; set; } = [];
    
    [Key( nameof(ChangedFileNames))]
    public List<FileNameResult> ChangedFileNames { get; set; } = [];
    
    

    [Key(nameof(RobotFileDetectionSummary))]
    public RobotFileDetectionSummary RobotFileDetectionSummary { get; set; } = new();
    
    /// <summary>
    /// Exceptions during search.
    /// </summary>
    [Key(nameof(Exceptions))]
    public List<ExceptionResult> Exceptions { get; set; }= [];

    /// <summary>
    /// MissingRequirements can help guide the user to update settings.
    /// </summary>
    [Key(nameof(MissingRequirements))]
    public List<MissingRequirementResult> MissingRequirements { get; set; } = [];

    /// <summary>
    /// Status for File System Enumeration, Seeing the number of files.
    /// This important step informs the user of how many files were found.
    /// It can also be used to reflect state for 'IsIndeterminate' on the progress bar
    /// </summary>
    [Key(nameof(FileSearchStatus))]
    public FileSearchStatus FileSearchStatus { get; } = new FileSearchStatus();
    
    [Key(nameof(ScheduledClear))]
    public bool ScheduledClear { get; set; }

}
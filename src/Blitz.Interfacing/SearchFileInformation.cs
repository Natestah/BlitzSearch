namespace Blitz.Interfacing;
using MessagePack;


/// <summary>
/// Simple tracking of Unique words-in-file to aid early out Performance in searching large numbers of files.
/// </summary>
[MessagePackObject]
public class SearchFileInformation
{
    public enum ReadState
    {
        Unknown,
        /// <summary>
        /// This gets flagged on FIleWatcher Error, for every file.
        /// </summary>
        NeedsUpdateDueToFileWatcherOverFLow,
        Read,
        Skipped
        //Todo:
        //LiveAndDirty // reserved for editor plugin to be able to inform about Dirty files
    }
    
    /// <summary>
    /// Indicates the last Time the file was parsed for Unique Words
    /// It is used to validate caches in memory.
    /// </summary>
    [Key(nameof(LastModifiedTime))]
    public DateTime LastModifiedTime { get; set; }

    /// <summary>
    /// Unique words is a collection of words stored as Ints, actual collection of words is shared.
    /// </summary>
    [Key(nameof(UniqueWords))]
    public int[] UniqueWords { get; set; } = [];
    
    /// <summary>
    /// Used to track the Current state of the file in memory..
    /// this is observed so that we don't have to ask the files system all the time
    /// </summary>
    [Key(nameof(FileState))]
    public ReadState FileState { get; set; }
    
    /// <summary>
    /// Keep track of filesize for stats reporting
    /// </summary>
    [Key(nameof(FileSize))]
    public long FileSize { get; set; }

    [Key(nameof(RobotState))]
    public RobotFileState RobotState { get; set; }
}

public enum RobotFileState
{
    Unknown,
    LooksHuman,
    FileToLarge,
    LongReadLines,
    LooksLikeBinary,
}


using MessagePack;

namespace Blitz.Interfacing;

[MessagePackObject]
public class MissingRequirementResult
{
    public enum Requirement
    {
        /// <summary>
        /// We need to know the extensions that the user intends to search.
        /// </summary>
        FileExtension,
        
        /// <summary>
        /// Need to have a directory to search
        /// </summary>
        FileDirectory,
        
        /// <summary>
        /// Need words
        /// </summary>
        SearchWords,

        /// <summary>
        /// Need words
        /// </summary>
        ReplaceWords,
        
        /// <summary>
        /// Need to be more specific as there are too many results
        /// </summary>
        NeedsRefinement
    }

    [IgnoreMemberAttribute]
    private readonly Dictionary<Requirement, string> _messages = new()
    {
        { Requirement.FileExtension, "Extensions are needed to search." },
        { Requirement.FileDirectory, "Enter a directory to search" },
        { Requirement.SearchWords, "Type words to search for words" },
    };

    [Key(nameof(MissingRequirement))]
    public Requirement MissingRequirement { get; set; }

    [Key(nameof(CustomMessage))]
    public string CustomMessage { get; set; } = string.Empty;

    [IgnoreMember]
    public string Message => string.IsNullOrEmpty(CustomMessage) ? _messages[MissingRequirement] : CustomMessage;
}
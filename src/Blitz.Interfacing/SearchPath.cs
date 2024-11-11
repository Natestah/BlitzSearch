using MessagePack;

namespace Blitz.Interfacing;

[MessagePackObject]
public class SearchPath
{
    [Key(nameof(Folder))] public string? Folder;

    [Key(nameof(TopLevelOnly))] public bool TopLevelOnly;

    public bool Equals(SearchPath? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Folder == other.Folder && TopLevelOnly == other.TopLevelOnly;
    }
}
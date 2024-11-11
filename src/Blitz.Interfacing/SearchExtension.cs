namespace Blitz.Interfacing;
using MessagePack;

[MessagePackObject]
public class SearchExtension
{
    [Key("Extension")]
    public string? Extension { get; set; }
}
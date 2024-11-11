using MessagePack;

namespace Blitz.Interfacing.QueryProcessing;

[MessagePackObject]
public class BlitzMatch
{
    [Key(nameof(MatchIndex))]
    public int MatchIndex;
    
    [Key(nameof(MatchLength))]
    public int MatchLength;

    [Key(nameof(Replacement))] 
    public string? Replacement;

    [Key(nameof(IsRegexSubgroup))] 
    public bool IsRegexSubgroup;
    
    public enum TouchType
    {
        None,
        Engulfed,
        LeftAndRight,
        Left,
        Right,
        RightExact,
        Exact
    }
    
    
    public bool Touches(int currentIndex, int lengthOfInline, out TouchType type)
    {
        bool startsInside = Touches(currentIndex);
        bool endsInside = Touches(currentIndex + lengthOfInline); 
        //starts and ends inside the match
        if (startsInside
            && endsInside)
        {
            type = TouchType.Engulfed;
            return true;
        }

        //starts before and Intersects
        bool startsBefore = currentIndex < MatchIndex && currentIndex + lengthOfInline > MatchIndex;
        bool endsAfter = currentIndex + lengthOfInline > MatchIndex + MatchLength;

        if (startsBefore && endsAfter)
        {
            type = TouchType.LeftAndRight;
            return true;
        }

        if (startsBefore)
        {
            type = TouchType.Left;
            return true;
        }
        
        if (endsAfter && startsInside)
        {
            type = TouchType.Right;
            return true;
        }

        if (endsAfter && MatchIndex + MatchLength == currentIndex)
        {
            type = TouchType.RightExact;
            return true;
        }

        if (startsInside)
        {
            type = TouchType.Exact;
            return true;
        }
        type = TouchType.None;
        return false;
    }

    private bool Touches(int index)
    {
        return index >= MatchIndex && index < MatchIndex + MatchLength;
    }

}
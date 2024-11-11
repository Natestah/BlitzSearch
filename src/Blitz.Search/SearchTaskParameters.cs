using System.Text.RegularExpressions;
using Blitz.Interfacing.QueryProcessing;

namespace Blitz.Search;

//Todo: this is a bit messy, I would like for maybe Regex, and Literal Searches to be a part of Blitz Query and stacked into "textBoxQuery"
//Todo: which is just "BlitzAndQuery", would make for a lot cleaner code througout and Enable potentially stacking multiples of those too! 

/// <summary>
/// SearchTaskParameters takes care of translating all the UI generated parameters into something we can pass around the search functions. 
/// </summary>
/// <param name="textBoxQuery"></param>
/// <param name="fileNameQuery"></param>
/// <param name="debugFileNameQuery"></param>
/// <param name="replaceQuery"></param>
/// <param name="replaceRegex"></param>
/// <param name="replaceLiteral"></param>
/// <param name="replaceWith"></param>
/// <param name="literal"></param>
/// <param name="regex"></param>
/// <param name="literalCaseSensitive"></param>
/// <param name="regexCaseSensitive"></param>
/// <param name="replaceCaseSensitive"></param>
public class SearchTaskParameters(BlitzAndQuery textBoxQuery,
    BlitzAndQuery? fileNameQuery = null,
    BlitzAndQuery? debugFileNameQuery = null,
    IBlitzMatchingQuery? replaceQuery = null,
    Regex? replaceRegex = null,
    string? replaceLiteral = null,
    string? replaceWith = null,
    string? literal = null,
    Regex? regex = null,
    bool literalCaseSensitive = false,
    bool regexCaseSensitive = false,
    bool replaceCaseSensitive = false
    )
{
    public BlitzAndQuery TextBoxQuery => textBoxQuery;
    public BlitzAndQuery? FileNameQuery => fileNameQuery;
    public BlitzAndQuery? DebugFileNameQuery => debugFileNameQuery;
    public IBlitzMatchingQuery? ReplaceQuery => replaceQuery;
    public Regex? ReplaceRegex => replaceRegex;
    
    public string? ReplaceLiteralSearch => replaceLiteral;
    public string? LiteralSearch => literal;
    public string? ReplaceWith => replaceWith;
    public Regex? RegexSearch => regex;
    
    public bool LiteralCaseSensitive => literalCaseSensitive;
    public bool RegexCaseSensitive => regexCaseSensitive;
    public bool ReplaceCaseSensitive => replaceCaseSensitive;
}

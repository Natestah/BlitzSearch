using Blitz.Interfacing.QueryProcessing;
using MessagePack;
namespace Blitz.Interfacing;

[MessagePack.Union(0, typeof(BlitzAndQuery))]
[MessagePack.Union(1, typeof(BlitzOrQuery))]
[MessagePack.Union(2, typeof(BlitzWordInQuery))]
public interface IBlitzMatchingQuery
{
    /// <summary>
    /// Word used to search
    /// </summary>
    string SearchWord { get; set; }

    /// <summary>
    /// Some Queries, like the Root AND query and the OR query can contain subQueries to be processed.
    /// </summary>
    List<IBlitzMatchingQuery> SubQueries { get; set; }

    /// <summary>
    /// Matches against lines of text in a document..
    /// </summary>
    /// <param name="lineText"> Full text of the line in document</param>
    /// <param name="caseSensitive"></param>
    /// <param name="matches"></param>
    /// <returns></returns>
    bool LineMatches(string lineText, bool caseSensitive, out List<BlitzMatch> matches);

    /// <summary>
    /// Returns false only when this matching query can be determined by the unique words for a file.  
    /// </summary>
    /// <param name="uniqueWords"></param>
    /// <returns></returns>
    bool SearchIndexValidFor(IEnumerable<string> uniqueWords);

}
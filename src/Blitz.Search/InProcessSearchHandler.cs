namespace Blitz.Search;

/// <summary>
/// Handles search messages In process, ISearchingClient
/// Todo: ISearchingClient has intents for being a client to a separate process that would do the work. 
/// </summary>
public class InProcessSearchHandler : ISearchingClient
{
    private int _messageId;
    private readonly Searching _searching = new Searching();

    public void PostSearchRequest(SearchQuery query, bool needsFileSystemRestart)
    {
        query.ProcessIdentity = Environment.ProcessId;
        query.InstanceIdentity = query.ProcessIdentity;
        query.MessageIdentity = _messageId;
        _messageId++;
        _messageId %= int.MaxValue;
        _searching.ProcessSearchingRequest(query, needsFileSystemRestart);
    }

    public event EventHandler<SearchTaskResult>? ReceivedFileResultEventHandler
    {
        add => _searching.ReceivedFileResultEventHandler += value;
        remove => _searching.ReceivedFileResultEventHandler -= value;
    }
}
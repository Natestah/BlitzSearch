namespace Blitz.Interfacing;

public interface ISearchingClient
{
    void PostSearchRequest(SearchQuery query, bool needsFileSystemRestart);
    event EventHandler<SearchTaskResult>? ReceivedFileResultEventHandler;
}
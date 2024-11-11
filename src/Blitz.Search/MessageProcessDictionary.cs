using System.Collections.Concurrent;

namespace Blitz.Search;

public class MessageProcessDictionary : ConcurrentDictionary<int,MessageInstanceDictionary>;
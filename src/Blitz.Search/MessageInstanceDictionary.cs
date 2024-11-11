using System.Collections.Concurrent;

namespace Blitz.Search;

public class MessageInstanceDictionary : ConcurrentDictionary<int,SearchTask>;
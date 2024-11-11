using System.Collections.Specialized;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace Blitz.Interfacing;

public static class MessagePackSetup
{
    
    public static void Init()
    {
        StaticCompositeResolver.Instance.Register(new IMessagePackFormatter[]{
                new ListFormatter<int>(),
                new LinkedListFormatter<int>(),
                new QueueFormatter<int>(),
                new HashSetFormatter<int>(),
                new ReadOnlyCollectionFormatter<int>(),
                new ObservableCollectionFormatter<int>(),
                new ReadOnlyObservableCollectionFormatter<int>(),
                new InterfaceListFormatter2<int>(),
                new InterfaceCollectionFormatter2<int>(),
                new InterfaceReadOnlyListFormatter<int>(),
                new InterfaceReadOnlyCollectionFormatter<int>(),
                new InterfaceSetFormatter<int>(),
                new InterfaceLookupFormatter<bool, int>(),
                new StackFormatter<int>(),
                new ConcurrentQueueFormatter<int>(),
                new ConcurrentStackFormatter<int>(),
                new ConcurrentBagFormatter<int>(),
                new ArraySegmentFormatter<int>(),
                new NullableFormatter<ArraySegment<int>>(),
                new InterfaceEnumerableFormatter<int>(),
                new InterfaceGroupingFormatter<bool, int>(),
                new DictionaryFormatter<int,int>(),
                new ReadOnlyDictionaryFormatter<int,int>(),
                new SortedListFormatter<int,int>(),
                new SortedDictionaryFormatter<int,int>(),
                new InterfaceDictionaryFormatter<int,int>(),
                new ConcurrentDictionaryFormatter<int,int>(),
                new ConcurrentDictionaryFormatter<string,SearchFileInformation>(),
                new ConcurrentDictionaryFormatter<string,FilesByExtension>(),
                new ConcurrentDictionaryFormatter<string,byte>(),
                new InterfaceReadOnlyDictionaryFormatter<int,int>(),

               new LazyFormatter<int>(),

                new KeyValuePairFormatter<int,string>(),
                new NullableFormatter<KeyValuePair<int,string>>(),
                new ValueTupleFormatter<int,int>(),
                new ValueTupleFormatter<int,int,int>(),
                new ValueTupleFormatter<int,int,int,int>(),
                new ArrayFormatter<ValueTuple<int,int>>(),
                new ArrayFormatter<ValueTuple<int,int,int>>(),
                new ArrayFormatter<ValueTuple<int,int,int,int>>(),
                new TwoDimensionalArrayFormatter<int>(),
                new ThreeDimensionalArrayFormatter<int>(),
                new FourDimensionalArrayFormatter<int>(),
                new TwoDimensionalArrayFormatter<(int,int)>(),
                new ThreeDimensionalArrayFormatter<(int,int,int)>(),
                new FourDimensionalArrayFormatter<(int,int,int,int)>(),

                NonGenericInterfaceListFormatter.Instance,
                NonGenericInterfaceDictionaryFormatter.Instance,
        },
            new IFormatterResolver[]{
                MessagePack.Resolvers.GeneratedResolver.Instance,
                MessagePack.Resolvers.StandardResolver.Instance
            });

            var resolver = StaticCompositeResolver.Instance;

            MessagePackSerializer.DefaultOptions = MessagePackSerializerOptions.Standard.WithResolver(resolver);
    }
}
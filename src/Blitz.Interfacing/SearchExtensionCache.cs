using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Text;
using MessagePack;
using System.Security.Cryptography;

namespace Blitz.Interfacing;


[MessagePackObject]
public class FilesByExtension //: ConcurrentDictionary<string, SearchFileInformation>
{

    [Key(nameof(Words))] 
    public List<string> Words { get; set; } = new();
    
    [IgnoreMember]
    public Dictionary<string,int> WordsToIntMap { get; set; } = new();
    
    
    [Key(nameof(FileInformations))]
    public ConcurrentDictionary<string, SearchFileInformation> FileInformations { get; set; }=
        new ConcurrentDictionary<string, SearchFileInformation>();

    [IgnoreMember]
    public IEnumerable<string> Keys => FileInformations.Keys;

    public bool TryGetValue(string key, out SearchFileInformation? fileInformation) =>
        FileInformations.TryGetValue(key, out fileInformation);

    public bool TryRemove(string key, out SearchFileInformation? fileInformation) =>
        FileInformations.TryGetValue(key, out fileInformation);

    public void Clear() => FileInformations.Clear();

    public int[] GetWordPositions(HashSet<string> collectionOfWords)
    {
        lock (Words)
        {
            var returnList = new int[collectionOfWords.Count];
            if (WordsToIntMap.Count == 0)
            {
                for (int i = 0; i < Words.Count; i++)
                {
                    WordsToIntMap[Words[i]] = i;
                }
            }

            int index = 0;
            foreach (var word in collectionOfWords)
            {
                var freshCount = Words.Count;
                if( !WordsToIntMap.TryGetValue(word, out var wordInt))
                {
                    returnList[index] = freshCount;
                    Words.Add(word);
                    WordsToIntMap[word] = freshCount;
                }
                else
                {
                    returnList[index] = wordInt;
                }
                index++;
            }
            return returnList;
        }
    }
    public string[] GetWordStrings(int[] intWords)
    {
        lock (Words)
        {
            var wordsArray = new string[intWords.Length];
            for (var index = 0; index < intWords.Length; index++)
            {
                var word = intWords[index];
                if (word < Words.Count)
                {
                    wordsArray[index] = Words[word];
                }
            }
            return wordsArray;
        }
    }

    public void TryAdd(string file, SearchFileInformation info)
    {
        FileInformations.TryAdd(file, info);

    }

    public void SetFile(string file, SearchFileInformation info)
    {
        FileInformations[file] = info;
    }

    public SearchFileInformation GetOrAdd(string file, Func<object, SearchFileInformation> func)
    {
        return FileInformations.GetOrAdd(file, func);
    }

    public IEnumerable<string> GetAllWords()
    {
        lock (Words)
        {
            return Words.ToArray();
        }
    }
}

/// <summary>
/// Represents a Concurrent Dictionary with Key Value Pair of 'Extension'->'Dictionary of files,SearchFileInformation
/// Extensions are seperated to aid in prioritization.
/// </summary>
public class SearchExtensionCache : ConcurrentDictionary<string, FilesByExtension>
{

    private ConcurrentDictionary<string, bool> _dirtyCaches = [];
    private ConcurrentDictionary<string, bool> _initiallySaved = [];

    private ConcurrentDictionary<string, byte> _extensionsEverKnown = [];

    private void SetToDefaultAllKnownTypes(IEnumerable<string> builtInTextTypes, IEnumerable<string> builtinBinarytypes)
    {
        foreach (var typeName in builtInTextTypes)
        {
            _extensionsEverKnown[typeName] = 1;
        }
        foreach (var typeName in builtinBinarytypes)
        {
            _extensionsEverKnown[typeName] = 1;
        }
    }

    public ConcurrentDictionary<string, byte> ExtensionsEverKnown => _extensionsEverKnown;
    private string AllKnownCacheTypesFileName => Path.Combine(CacheFolder, "ALLExtensions.blitzCache");
    
    public void RestoreExtensionsEverKnown( IEnumerable<string> builtInTextTypes, IEnumerable<string> builtinBinarytypes)
    {
        if (_extensionsEverKnown.Count > 0)
        {
            return;
        }
        Directory.CreateDirectory(CacheFolder);
        string cachefile = AllKnownCacheTypesFileName;
        if (File.Exists(cachefile))
        {
            try
            {
                var bytes = File.ReadAllBytes(cachefile);
                _extensionsEverKnown = MessagePackSerializer.Deserialize<ConcurrentDictionary<string, byte>>(bytes);
            }
            catch (Exception )
            {
                //
                SetToDefaultAllKnownTypes(builtInTextTypes,builtinBinarytypes);
            }
        }
        else
        {
              SetToDefaultAllKnownTypes(builtInTextTypes,builtinBinarytypes);
        }
    }

    public void SaveAllKnownCacheTypes()
    {
        Directory.CreateDirectory(CacheFolder);
        File.WriteAllBytes( AllKnownCacheTypesFileName, MessagePackSerializer.Serialize(_extensionsEverKnown));
    }

    
    

    public void SaveCache(IEnumerable<string> paths,  bool ignoreHeavies, double robotMaxBytesMb, int maxLineChars , bool useGitIgnore, string extension)
    {
        if (!_initiallySaved.TryAdd(extension, true))
        {
            if (!_dirtyCaches.ContainsKey(extension))
            {
                return;
            }
            if (!_dirtyCaches.TryRemove(extension, out _))
            {
                return;
            }
        }
        
        string fileName = GetCacheFileName(paths, ignoreHeavies, robotMaxBytesMb, maxLineChars, useGitIgnore, extension);
        
        
        if (TryGetValue(extension, out var fileDictionary))
        {
            byte[] bytes = MessagePackSerializer.Serialize(fileDictionary);
            File.WriteAllBytes(fileName, bytes);
        }
    }


    private static ConcurrentDictionary<string, byte> AllKnownExtensions = [];

    public IEnumerable<string> EnumCacheFileExtensions(string extensions, CancellationTokenSource cancellationTokenSource)
    {
        if (!TryGetValue(extensions, out var dictionary)) yield break; 
        foreach (var key in dictionary.Keys)
        {
            if (cancellationTokenSource.IsCancellationRequested) yield break;
            if (File.Exists(key))
            {
                yield return key;
            }
        }
    }

    public void RestoreCache(IEnumerable<string> paths, bool ignoreHeavies, double robotMaxBytesMb, int maxLineChars, bool useGitIgnore, string extension, CancellationToken token)
    {
        AllKnownExtensions[extension] = 1;
        if (ContainsKey(extension))
        {
            return;
        }
        string fileName = GetCacheFileName(paths, ignoreHeavies,robotMaxBytesMb,maxLineChars, useGitIgnore, extension);
        if (!File.Exists(fileName))
        {
            return;
        }

        try
        {
            var bytes = File.ReadAllBytes(fileName);
            var fileDictionary =
                MessagePackSerializer.Deserialize<FilesByExtension>(bytes);
            TryAdd(extension, fileDictionary);
        }
        catch (Exception ex) when (ex is MessagePackSerializationException)
        {
            if (ex.InnerException is OperationCanceledException)
            {
                TryRemove(extension, out _);
            }
            else
            {
                //it's ok, I'll likely change the format of SearchFileInformation.
                this[extension] = new FilesByExtension();

            }
        }
        catch (TaskCanceledException)
        {
            TryRemove(extension, out _);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        
    }
    
    public static string CacheFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NathanSilvers","CacheContents");
    public static string Md5(string input, bool isLowercase = false)
    {
        using (var md5 = MD5.Create())
        {
            var byteHash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            var hash = BitConverter.ToString(byteHash).Replace("-", "");
            return (isLowercase) ? hash.ToLower() : hash;
        }
    }
    /// <summary>
    /// Caches can be different, based on things like 'UseGitIgnore' and other Optional Optimizations
    /// </summary>
    /// <returns></returns>
    private string GetCacheFileName(IEnumerable<string> paths, bool ignoringHeavies, double maxmb, int maxCols, bool useGitIgnore, string extension)
    {
        var name = new StringBuilder();
        var hashPrefix = Md5(string.Concat(paths));
        name.Append(extension.TrimStart('.'));
        name.Append("_");
        name.Append(hashPrefix);
        name.Append("_");
        if (useGitIgnore)
        {
            name.Append("GI");
        }

        if (ignoringHeavies)
        {
            name.Append("_IH_");
        }
        name.Append(maxmb.ToString().Replace(".", "-"));
        name.Append("_");
        name.Append(maxCols.ToString());

        name.Append("_v5");
        name.Append(".bc");
        Directory.CreateDirectory(CacheFolder);
        return Path.Combine(CacheFolder,name.ToString());
    }

    public void DirtyCache(string extension)
    {
        _dirtyCaches[extension] = true;
    }


    
    
}
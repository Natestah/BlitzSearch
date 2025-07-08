using System.Diagnostics;
using System.Text;

namespace Blitz.Files;
using Ignore;

public class IgnorePath
{
    private Ignore _ignore;
    private DateTime _lastModified = DateTime.MinValue;
    private readonly StringBuilder _errorLog = new StringBuilder();
    private readonly string _gitRelativePath;
    private readonly string _directory;

    /// <summary>
    /// Retains ignore for directory.
    /// </summary>
    /// <param name="ignoreFile"></param>
    /// <param name="rootDirectory">Override for relative path of this IgnorePath, used for global ignores</param>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public IgnorePath(string ignoreFile, string? rootDirectory = null)
    {
        _ignore = new Ignore();
        if (string.IsNullOrEmpty(ignoreFile))
        {
            throw new NullReferenceException("Null or empty ignore file..");
        }
        
        IgnoreFileName = ignoreFile;
        _directory = rootDirectory ?? Path.GetDirectoryName(ignoreFile) ?? string.Empty;
        _gitRelativePath = _directory.Replace("\\","/");
    }
    
    public string IgnoreFileName { get; }


    private static readonly TimeSpan OneMicroSecond = TimeSpan.FromMicroseconds(1);
    /// <summary>
    /// Passively parse the .gitIgnore file
    /// </summary>
    /// <param name="ignoreFile"></param>
    /// <returns></returns>
    public bool ParseIgnore()
    {
        if (!File.Exists(IgnoreFileName))
        {
            return false;
        }
        lock (this)
        {
            try
            {
                var lastWriteTimeNow = File.GetLastWriteTimeUtc(IgnoreFileName);
                if (lastWriteTimeNow-_lastModified > OneMicroSecond)
                {
                    var freshIgnore = new Ignore();
                    using var file = new FileStream(IgnoreFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var streamReader = new StreamReader(IgnoreFileName);
                    while (streamReader.Peek() != -1)
                    {
                        string line = streamReader.ReadLine() ?? throw new ArgumentNullException("streamReader.ReadLine()");
                        try
                        {
                            freshIgnore.Add(line);
                        }
                        catch (Exception e)
                        {
                            _errorLog.AppendLine(e.Message);
                        }
                    }
                    _lastModified = lastWriteTimeNow;
                    _ignore = freshIgnore;
                }
            }
            catch (Exception e)
            {
                //Todo, this was throwing exception on regex.
                //should be part of the Exception report that only happens when users opt in to Really verbose report
                Console.WriteLine(e);
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Returns true if this file is ignored.
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public bool IsIgnored(string filename)
    {
        if (!filename.StartsWith(_directory, StringComparison.OrdinalIgnoreCase))
        {
             return false;
        }

        var relativePath = Path.GetRelativePath(_gitRelativePath, filename).Replace("\\","/");
        try
        {
            return _ignore.IsIgnored(relativePath);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
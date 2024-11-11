using System.Diagnostics;
using System.Text;

namespace Blitz.Files;
using Ignore;

public class IgnorePath
{
    private readonly string _directory;
    private Ignore _ignore;
    private DateTime _lastModified = DateTime.MinValue;
    private StringBuilder _errorLog = new StringBuilder();

    /// <summary>
    /// Retains ignore for directory.
    /// </summary>
    /// <param name="ignoreFile"></param>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public IgnorePath(string ignoreFile)
    {
        _ignore = new Ignore();
        if (!File.Exists(ignoreFile))
        {
            throw new FileNotFoundException(ignoreFile);
        }
        _directory = Path.GetDirectoryName(ignoreFile) ?? throw new InvalidOperationException();
    }


    private static TimeSpan OneMicroSecond = TimeSpan.FromMicroseconds(1);
    /// <summary>
    /// Passively parse the .gitIgnore file
    /// </summary>
    /// <param name="ignoreFile"></param>
    /// <returns></returns>
    public bool ParseIgnore(string ignoreFile)
    {
        lock (this)
        {
            try
            {
                var lastWriteTimeNow = File.GetLastWriteTimeUtc(ignoreFile);
                if (lastWriteTimeNow-_lastModified > OneMicroSecond)
                {
                    var freshIgnore = new Ignore();
                    using var file = new FileStream(ignoreFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var streamReader = new StreamReader(ignoreFile);
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
        if (!filename.StartsWith(_directory))
        {
            return false;
        }

        var relativePath = Path.GetRelativePath(_directory, filename).Replace("\\","/");
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
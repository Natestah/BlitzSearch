using System.Diagnostics;
using Blitz.Interfacing;

namespace Blitz.Files.Tests;

public class FileDiscoveryTests
{
    private const string GitIgnoreFile = @"ignore\test\file.cs";
    private void PrepareTestFiles(out List<SearchPath> searchPaths, out List<string> files, out HashSet<string> exclusiveExtensions)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "blitz_files_tests");
        if (!Directory.Exists(tempPath))
        {
            Directory.CreateDirectory(tempPath);
        }
        var gitIgnoreFile = Path.Combine(tempPath, ".gitignore");
        
        searchPaths =
        [
            new SearchPath{Folder = tempPath}
        ];
        files =
        [
            Path.Combine(tempPath, ".gitignore"),
            Path.Combine(tempPath, GitIgnoreFile),
            Path.Combine(tempPath, "testNotIgnored","test","file.cs"),
            Path.Combine(tempPath, "file.cs"),
        ];

        exclusiveExtensions = [];
        foreach (var file in files)
        {
            string extension = Path.GetExtension(file);
            exclusiveExtensions.Add(extension);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, "");
        }
        
        File.WriteAllText(gitIgnoreFile,"ignore/" + Environment.NewLine );
    }

    private void CleanupTestFiles(List<string> files)
    {
        var dirs = new HashSet<string>();
        foreach (var file in files)
        {
            dirs.Add(Path.GetDirectoryName(file)!);
            File.Delete(file);
        }

        foreach (var dir in dirs)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Fact]
    public void Test_Files_Found()
    {
        PrepareTestFiles(out var searchPaths, out var testingFiles, out var extensionSet);
        
        var lookingForFiles = new List<string>(testingFiles);

        
        var fileDiscovery = new FileDiscovery(searchPaths, useGitIgnore: false);
        foreach (var file in fileDiscovery.EnumerateAllFiles(new  CancellationTokenSource()))
        {
            Trace.WriteLine(file);
            Assert.True(lookingForFiles.Remove(file));
        }

        Assert.Empty(lookingForFiles);
        CleanupTestFiles(testingFiles);

    }
    
    [Fact]
    public void Test_Files_Found_WithoutRetention()
    {
        PrepareTestFiles(out var searchPaths, out var testingFiles, out var extensionSet);
        
        var lookingForFiles = new List<string>(testingFiles);

        
        var fileDiscovery = new FileDiscovery(searchPaths, useGitIgnore: false);
        foreach (var file in fileDiscovery.EnumerateAllFiles(new CancellationTokenSource()))
        {
            Trace.WriteLine(file);
            Assert.True(lookingForFiles.Remove(file));
        }

        Assert.Empty(lookingForFiles);
    }

    private void GitIgnoreFiles(bool retention)
    {
        PrepareTestFiles(out var searchPaths, out var testingFiles, out var extensionSet);
        
        var lookingForFiles = new List<string>(testingFiles);
        var fileDiscovery = new FileDiscovery(searchPaths, useGitIgnore: true);
        foreach (var file in fileDiscovery.EnumerateAllFiles(new CancellationTokenSource()))
        {
            Trace.WriteLine(file);
            Assert.True(lookingForFiles.Remove(file));
        }

        Assert.Single(lookingForFiles);
        Assert.EndsWith(lookingForFiles[0],GitIgnoreFile);
        CleanupTestFiles(testingFiles);
        
    }
    [Fact]
    public void Test_GitIgnoreFile_Ignored()
    {
        PrepareTestFiles(out var searchPaths, out var testingFiles, out var extensionSet);
        
        var lookingForFiles = new List<string>(testingFiles);
        var fileDiscovery = new FileDiscovery(searchPaths, useGitIgnore: true);
        foreach (var file in fileDiscovery.EnumerateAllFiles(new CancellationTokenSource()))
        {
            Trace.WriteLine(file);
            Assert.True(lookingForFiles.Remove(file));
        }

        Assert.Single(lookingForFiles);
        Assert.EndsWith(GitIgnoreFile,lookingForFiles[0]);
        CleanupTestFiles(testingFiles);
    }
    
    
    [Fact]
    public void Test_GitIgnoreFile_Ignored_WithoutRetention()
    {
        PrepareTestFiles(out var searchPaths, out var testingFiles, out var extensionSet);
        
        var lookingForFiles = new List<string>(testingFiles);
        var fileDiscovery = new FileDiscovery(searchPaths, useGitIgnore: true);
        foreach (var file in fileDiscovery.EnumerateAllFiles(new CancellationTokenSource()))
        {
            Trace.WriteLine(file);
            Assert.True(lookingForFiles.Remove(file));
        }

        Assert.Single(lookingForFiles);
        Assert.EndsWith(GitIgnoreFile,lookingForFiles[0]);
        CleanupTestFiles(testingFiles);
    }
}
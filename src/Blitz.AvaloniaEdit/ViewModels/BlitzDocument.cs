using System;
using System.IO;
using Blitz.Interfacing;
using ReactiveUI;

namespace Blitz.AvaloniaEdit.ViewModels;

/// <summary>
/// Tab View Model For Documents, Tab items only retain Dirty Text Information,  There is only one Avalonia document loaded this way.
/// </summary>
public class BlitzDocument : ViewModelBase
{
    private bool _isDirty = false;
    private int _alignViewLine;
    private int _alignViewColumn;
    private bool _isPreviewing;
    public string FileNameOrTitle { get; set; }
    
    public string RelativeDirectoryPath { get; set; }

    //ScrollViewer, when sleeping files needs to go back to the last scroll position. -> https://github.com/Natestah/BlitzSearch/issues/89

    public int AlignViewLine
    {
        get => _alignViewLine;
        set => this.RaiseAndSetIfChanged(ref _alignViewLine, value);
    }

    public int AlignViewColumn
    {
        get => _alignViewColumn;
        set => this.RaiseAndSetIfChanged(ref _alignViewColumn, value);
    }

    public bool IsPreviewing
    {
        get => _isPreviewing;
        set => this.RaiseAndSetIfChanged(ref _isPreviewing, value);
    }

    public string TabTitle
    {
        get
        {
            if (Type == DocumentType.File)
            {
                return Path.GetFileName(FileNameOrTitle);
            }
            
            //todo, multiple files with same name.. show more of the path to distinguish.
            return FileNameOrTitle;
        }
    }

    public DocumentType Type { get; }

    public enum DocumentType
    {
        Untitled,
        File
    }
    
    public string? ExtensionOverride { get; set; } = null;
    
    public string Extension
    {
        get
        {
            var extension = ExtensionOverride ?? (Type == DocumentType.File ?  Path.GetExtension(FileNameOrTitle) : ".txt");
            return ExtensionAsMap.Get(extension);
        }
    }

    public DateTime LastModified { get; set; } = DateTime.MinValue;

    public bool IsDirty
    {
        get => _isDirty;
        set => this.RaiseAndSetIfChanged(ref _isDirty, value);
    }

    public string DirtyText { get; set; } = "";

    public string RelativePath
    {
        get
        {
            if (Type != DocumentType.File)
            {
                return string.Empty;  // for information tabs.
            }

            try
            {
                return Path.GetRelativePath(RelativeDirectoryPath, FileNameOrTitle);
            }
            catch (Exception e)
            {
                return "-";
            }
        }
    }

    public BlitzDocument(DocumentType documentType, string fileNameOrTitle, string? relativeDirectoryPath = null)
    {
        RelativeDirectoryPath = relativeDirectoryPath ?? string.Empty;
        FileNameOrTitle = fileNameOrTitle;
        Type = documentType;
    }
}
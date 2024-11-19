using System;
using Blitz.Interfacing;

namespace Blitz.Avalonia.Controls.ViewModels;

public class ExceptionViewModel : ViewModelBase
{
    private readonly ExceptionResult _exceptionResult;
    public ExceptionViewModel(ExceptionResult exceptionResult)
    {
        _exceptionResult = exceptionResult;
    }

    public string Message => _exceptionResult.ExceptionMessage ?? string.Empty;

    public string StackInfo => _exceptionResult.ExceptionStack ?? string.Empty;
}
using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Blitz.Avalonia.Controls;
using ReactiveUI;
using Xamarin.Forms.Internals;

namespace Blitz;

sealed class Program
{
    private static Mutex? _mainMutex;
    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var findIndex = args.IndexOf("-find");
        if (findIndex != -1)
        {
            if (args.Length > findIndex + 1)
            {
                WriteFindDirective(args[findIndex+1]);
            }
            else
            {
                WriteFindDirective();
            }
        }
        
        string mainMutex = "BlitzMainMutex";
        try
        {
            if (Mutex.TryOpenExisting(mainMutex, out _))
            {
                return;
            }
            _mainMutex = new Mutex(false, mainMutex);
        }
        catch (Exception)
        {
            return;
        }

        TaskScheduler.UnobservedTaskException+=TaskSchedulerOnUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException+=CurrentDomainOnUnhandledException;
        RxApp.DefaultExceptionHandler = new MyCoolObservableExceptionHandler(); 
        
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
           GlobalExceptionReport(e);
        }
    }

    private static void WriteFindDirective(string? inWorkingDirectory = null)
    {
        string commandFolder = PluginCommands.GetCommandsFolder();
        inWorkingDirectory ??= Environment.CurrentDirectory;
        File.WriteAllText(Path.Combine(commandFolder, $"{PluginCommands.SimpleFolderSearch}"), inWorkingDirectory);
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            GlobalExceptionReport(ex);
        }
    }

    private static void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        GlobalExceptionReport(e.Exception);
    }

    static void PrintStack(Exception ex, StringBuilder builder)
    {
        if (ex.StackTrace == null) return;
        builder.AppendLine("<h2>");
        builder.Append("Exception Stack: ");
        builder.AppendLine("</h2>");
        builder.AppendLine("<p>");
        using var re = new StringReader(ex.StackTrace);
        while (re.Peek() != -1)
        {
            var line = re.ReadLine()!;
            builder.Append(HttpUtility.HtmlEncode(line));
            builder.AppendLine("<br>");
        }
        builder.AppendLine("</p>");

    }

    static void PrintExceptionMessage(Exception ex, StringBuilder builder)
    {
        builder.AppendLine("<h2>");
        builder.Append("Exception Message: ");
        builder.AppendLine("</h2>");
        builder.AppendLine(HttpUtility.HtmlEncode(ex.Message));

    }

    static void PrintExceptionAsHmtl(Exception ex, bool simple = false)
    {
        var dir = Path.Combine(Path.GetTempPath(), "blitz_exceptions");
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, "BlitzException.html");
        
        var builder = new StringBuilder();
        builder.AppendLine("<h1>");
        builder.AppendLine("Blitz Search Has crashed :( ");
        builder.AppendLine("</h1>");

        builder.AppendLine("<p>");
        builder.AppendLine(
            "Currently Blitz Search Does not automatically send information anywhere, please help me to improve the quality of Blitz Search by sending this information to the either my email or Discord channel.");
        builder.AppendLine("</p>");

        builder.AppendLine("<br>");
        builder.AppendLine("<a href=\"mailto:natestah@gmail.com\">Email Support</a>");
        builder.AppendLine("<br>");
        builder.AppendLine("<a href=\"https://discord.com/invite/UYPwQY9ngm\">Join the BlitzSearch Discord</a>");
        builder.AppendLine("<br>");
        builder.AppendLine("<br>");

        builder.Append("Version: ");
        try
        {
            builder.AppendLine(Assembly.GetEntryAssembly()!.GetName().Version!.ToString().TrimEnd());
        }
        catch (Exception)
        {
            builder.AppendLine("Version Unavailable");
        }
        PrintExceptionMessage(ex, builder);

        if (!simple)
        {
            PrintStack(ex, builder);
        }


        if (!simple)
        {
            var innerException = ex.InnerException;

            while (innerException != null)
            {
                PrintExceptionMessage(innerException, builder);
                if (innerException.StackTrace != null)
                {
                    PrintStack(innerException, builder);
                }
                innerException = innerException.InnerException;
            }

        }
    
        builder.AppendLine("</body>");
    
        File.WriteAllText(filePath,builder.ToString());
        
        var processStartInfo = new ProcessStartInfo(filePath)
        {
            UseShellExecute = true
        };
        Process.Start(processStartInfo); 
    }

    static void GlobalExceptionReport(Exception ex)
    {
        try
        {
            PrintExceptionAsHmtl(ex);
        }
        catch (Exception)
        {
            GlobalExeptionReportSimplified(ex);
        }
        
    }

    static void GlobalExeptionReportSimplified(Exception ex)
    {
        PrintExceptionAsHmtl(ex, true);
    }

    static void PrintStackText(Exception ex, StringBuilder builder)
    {
        if (ex.StackTrace == null) return;
        builder.Append("Exception Stack: ");
        builder.AppendLine("");
        using var re = new StringReader(ex.StackTrace);
        while (re.Peek() != -1)
        {
            var line = re.ReadLine()!;
            builder.AppendLine(line);
        }

    }

    static void PrintExceptionMessageText(Exception ex, StringBuilder builder)
    {
        builder.Append("Exception Message: ");
        builder.AppendLine();
        builder.AppendLine(ex.Message);
    }
    
    static void PrintExceptionToText(Exception ex)
    {
        var dir = Path.Combine(Path.GetTempPath(), "blitz_exceptions");
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, "BlitzException.txt");
        
        var builder = new StringBuilder();
        
        builder.AppendLine("Blitz Search Has crashed :( ");
        builder.AppendLine(
            "Currently Blitz Search Does not send any data, I would like to get this information. Send to Email or Discord");
        builder.Append("Email: ");
        builder.AppendLine("natestah@gmail.com");
        builder.Append("Discord: ");
        builder.AppendLine("https://discord.com/invite/UYPwQY9ngm");

        PrintExceptionMessageText(ex, builder);
        PrintStackText(ex, builder);

        var innerException = ex.InnerException;

        while (innerException != null)
        {
            PrintExceptionMessageText(innerException, builder);
            if (innerException.StackTrace != null)
            {
                PrintStackText(innerException, builder);
            }
            innerException = innerException.InnerException;
        }
        File.WriteAllText(filePath,builder.ToString());
        var processStartInfo = new ProcessStartInfo(filePath)
        {
            UseShellExecute = true
        };
        Process.Start(processStartInfo); 
    }
    
    public class MyCoolObservableExceptionHandler : IObserver<Exception>
    {
        public void OnNext(Exception value)
        {
            GlobalExceptionReport(value);
            RxApp.MainThreadScheduler.Schedule(() => throw value) ;
        }

        public void OnError(Exception error)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            GlobalExceptionReport(error);
        }

        public void OnCompleted()
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            RxApp.MainThreadScheduler.Schedule(() => throw new NotImplementedException());
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
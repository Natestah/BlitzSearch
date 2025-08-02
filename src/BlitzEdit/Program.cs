using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ReactiveUI;
namespace BlitzEdit;

sealed class Program
{
    
    private static Mutex? _mainMutex;

    
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        string mainMutex = "BlitzEditMainMutex";
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
        RxApp.DefaultExceptionHandler = new MyObservableExceptionHandler(); 
        
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

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            GlobalExceptionReport(ex);
        }
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

    static void PrintExceptionAsHmtl(Exception ex, bool simple = false)
    {
        var dir = Path.Combine(Path.GetTempPath(), "blitz_edit_exceptions");
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, "BlitzEditException.html");
        
        var builder = new StringBuilder();
        builder.AppendLine("<h1>");
        builder.AppendLine("Blitz Edit Has crashed :( ");
        builder.AppendLine("</h1>");

        builder.AppendLine("<p>");
        builder.AppendLine(
            "Currently Blitz Edit Does not automatically send information anywhere, please help me to improve the quality of Blitz Edit by sending this information to the Github issue tracker.");
        builder.AppendLine("</p>");

        builder.AppendLine("<br>");
        builder.AppendLine("<a href=\"mailto:natestah@gmail.com\">My Personal Email</a>");
        builder.AppendLine("<br>");
        builder.AppendLine("<a href=\"https://github.com/Natestah/BlitzSearch/issues\">File an Issue on Github</a>");
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
    
    static void PrintExceptionMessage(Exception ex, StringBuilder builder)
    {
        builder.AppendLine("<h2>");
        builder.Append("Exception Message: ");
        builder.AppendLine("</h2>");
        builder.AppendLine(HttpUtility.HtmlEncode(ex.Message));
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
    public class MyObservableExceptionHandler : IObserver<Exception>
    {
        public void OnNext(Exception value)
        {
            GlobalExceptionReport(value);
            RxApp.MainThreadScheduler.Schedule(() => { throw value; }) ;
        }

        public void OnError(Exception error)
        {
            if (Debugger.IsAttached) Debugger.Break();
            GlobalExceptionReport(error);
        }

        public void OnCompleted()
        {
            if (Debugger.IsAttached) Debugger.Break();
            RxApp.MainThreadScheduler.Schedule(() => { throw new NotImplementedException(); });
        }
    }

    private static void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        GlobalExceptionReport(e.Exception);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
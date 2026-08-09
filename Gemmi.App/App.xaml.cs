using System;
using System.IO;
using System.Windows;

namespace Gemmi.App;

public partial class App : Application
{
    private static readonly string LogPath = @"C:\Users\admin\source\gemmi-engine\gemmi_debug.log";

    public static void Log(string msg)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n");
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Log("App.OnStartup called");

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Log($"FATAL AppDomain UnhandledException: {ex?.Message}\n{ex?.StackTrace}");
            if (ex?.InnerException != null)
            {
                Log($"FATAL InnerException: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
            }
        };

        DispatcherUnhandledException += (s, args) =>
        {
            Log($"FATAL DispatcherUnhandledException: {args.Exception.Message}\n{args.Exception.StackTrace}");
            if (args.Exception.InnerException != null)
            {
                Log($"FATAL InnerException: {args.Exception.InnerException.Message}\n{args.Exception.InnerException.StackTrace}");
            }
            args.Handled = true;
        };

        base.OnStartup(e);
    }
}

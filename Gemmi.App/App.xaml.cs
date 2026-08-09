using System;
using System.Windows;

namespace Gemmi.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            var innerMsg = ex?.InnerException != null ? $"\n\nInner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}" : "";
            MessageBox.Show($"Unhandled Exception:\n{ex?.Message}{innerMsg}\n\nStack Trace:\n{ex?.StackTrace}", "Gemmi Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            var innerMsg = args.Exception.InnerException != null ? $"\n\nInner Exception: {args.Exception.InnerException.Message}\n{args.Exception.InnerException.StackTrace}" : "";
            MessageBox.Show($"Dispatcher Exception:\n{args.Exception.Message}{innerMsg}\n\nStack Trace:\n{args.Exception.StackTrace}", "Gemmi Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        base.OnStartup(e);
    }
}

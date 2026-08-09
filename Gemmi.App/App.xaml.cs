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
            MessageBox.Show($"Unhandled Exception:\n{ex?.Message}\n\nStack Trace:\n{ex?.StackTrace}", "Gemmi Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Dispatcher Exception:\n{args.Exception.Message}\n\nStack Trace:\n{args.Exception.StackTrace}", "Gemmi Engine Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        base.OnStartup(e);
    }
}

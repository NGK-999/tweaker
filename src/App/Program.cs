using System;
using System.Windows;
using ApexTweaker.Minecraft;
using ApexTweaker.UI.Wpf;
using ApexTweaker.UI.Wpf.Windows;

namespace ApexTweaker;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        _ = ApplicationPaths.MigrateLegacyMinecraftData();

        if (MinecraftCommandLine.TryRun(args, out var exitCode))
        {
            Environment.ExitCode = exitCode;
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                ShowFatalCrash("AppDomain Exception", ex.Message, ex.StackTrace);
                return;
            }

            ShowFatalCrash(
                "AppDomain Exception",
                args.ExceptionObject?.ToString() ?? "Unknown fatal exception.",
                null);
        };

        try
        {
            var app = new System.Windows.Application
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown
            };

            app.DispatcherUnhandledException += (_, args) =>
            {
                args.Handled = true;
                ShowFatalCrash("Dispatcher Exception", args.Exception.Message, args.Exception.StackTrace);
            };

            var disclaimerWindow = new StartupDisclaimerWindow();
            if (disclaimerWindow.ShowDialog() != true)
            {
                return;
            }

            var loadingWindow = new LoadingWindow();
            if (loadingWindow.ShowDialog() != true)
            {
                return;
            }

            var mainWindow = new MainWindow();
            app.MainWindow = mainWindow;
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            app.Run(mainWindow);
        }
        catch (Exception ex)
        {
            ShowFatalCrash("Application.Run Exception", ex.Message, ex.StackTrace);
        }
    }

    private static void ShowFatalCrash(string title, string message, string? stackTrace)
    {
        System.Windows.MessageBox.Show(
            $"{title}:{Environment.NewLine}{message}{Environment.NewLine}{Environment.NewLine}StackTrace:{Environment.NewLine}{stackTrace ?? "<sem stacktrace>"}",
            "CRASH FATAL",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}


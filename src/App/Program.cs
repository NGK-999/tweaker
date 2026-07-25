using System;
using System.Linq;
using System.Windows;
using ApexTweaker.Infrastructure;
using ApexTweaker.Minecraft;
using ApexTweaker.UI.Wpf;
using ApexTweaker.UI.Wpf.Testing;
using ApexTweaker.UI.Wpf.Windows;

namespace ApexTweaker;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        RuntimeModeContext.Configure(
            args.Any(a => string.Equals(a, "--demo", StringComparison.OrdinalIgnoreCase))
                ? RuntimeMode.Demo
                : RuntimeMode.Standard);

        if (args.Any(a => string.Equals(a, "--market-coverage-self-test", StringComparison.OrdinalIgnoreCase)))
        {
            Environment.ExitCode = MarketCoverageSelfTest.Run();
            return;
        }

        if (args.Any(a => string.Equals(a, "--gaming-fps-probe-self-test", StringComparison.OrdinalIgnoreCase)))
        {
            Environment.ExitCode = GamingFpsProbeSelfTest.Run();
            return;
        }

        if (args.Any(a => string.Equals(a, "--demo-self-test", StringComparison.OrdinalIgnoreCase)))
        {
            Environment.ExitCode = DemoSafetySelfTest.Run();
            return;
        }

        if (args.Any(a => string.Equals(a, "--catalog-feedback-self-test", StringComparison.OrdinalIgnoreCase)))
        {
            Environment.ExitCode = CatalogFeedbackSelfTest.Run();
            return;
        }

        if (args.Any(a => string.Equals(a, "--optimization-state-audit", StringComparison.OrdinalIgnoreCase)))
        {
            Environment.ExitCode = OptimizationStateAudit.Run();
            return;
        }

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


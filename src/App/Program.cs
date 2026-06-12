using System;
using System.Windows.Forms;
using Renomeador.Forms;

namespace Renomeador;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) =>
        {
            ShowFatalCrash(
                "Thread Exception",
                args.Exception.Message,
                args.Exception.StackTrace);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                ShowFatalCrash(
                    "AppDomain Exception",
                    ex.Message,
                    ex.StackTrace);
                return;
            }

            ShowFatalCrash(
                "AppDomain Exception",
                args.ExceptionObject?.ToString() ?? "Unknown fatal exception.",
                null);
        };

        try
        {
            ApplicationConfiguration.Initialize();

            using (var disclaimer = new StartupDisclaimerDialog())
            {
                if (disclaimer.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            using (var loadingForm = new LoadingForm())
            {
                if (loadingForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            Application.Run(new ValorantTweakerForm());
        }
        catch (Exception ex)
        {
            ShowFatalCrash(
                "Application.Run Exception",
                ex.Message,
                ex.StackTrace);
        }
    }

    private static void ShowFatalCrash(string title, string message, string? stackTrace)
    {
        MessageBox.Show(
            $"{title}:{Environment.NewLine}{message}{Environment.NewLine}{Environment.NewLine}StackTrace:{Environment.NewLine}{stackTrace ?? "<sem stacktrace>"}",
            "CRASH FATAL",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}

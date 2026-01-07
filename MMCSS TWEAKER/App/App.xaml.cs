using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using MMCSSTweaker.Core;
using MMCSSTweaker.Win32;

namespace MMCSSTweaker;

public partial class App : Application
{
    private const string AppUserModelId = "com.beyondperformance.mmcsstweaker.1";

    private void OnStartup(object sender, StartupEventArgs e)
    {
        GuiSettingsStore.TryDeleteLegacyConfig();
        LocalizationManager loc = LocalizationManager.Instance;

        TrySetAppUserModelId();
        RegisterGlobalExceptionHandlers();

        if (!AdminUtils.IsRunningAsAdmin())
        {
            if (AdminUtils.RelaunchAsAdmin(e.Args))
            {
                Shutdown();
                return;
            }

            UiDialog.Warning(null, "MMCSS-TWEAKER", loc["Msg_AdminRequired"]);
            Shutdown(1);
            return;
        }

        string[] args = e.Args ?? Array.Empty<string>();
        if (HasArg(args, "--help") || HasArg(args, "-h") || HasArg(args, "/?"))
        {
            EnsureConsole();
            PrintUsage();
            Shutdown(0);
            return;
        }

        if (HasArg(args, "--auto-optimize"))
        {
            EnsureConsole();
            if (AutoOptimize.ApplyAutoOptimization(out string? error))
            {
                Console.WriteLine($"✅ {loc["Cli_AutoOptimizeSuccess"]}");
                Console.WriteLine(loc["Cli_AutoOptimizeSuccessDetails"]);
                Shutdown(0);
                return;
            }

            Console.WriteLine($"⚠️ {loc["Cli_AutoOptimizePartial"]}");
            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine(error);
            Console.WriteLine(loc["Cli_RunGuiForMore"]);

            Shutdown(1);
            return;
        }

        if (args.Length == 0 || HasArg(args, "--gui"))
        {
            try
            {
                var window = new MainWindow();
                MainWindow = window;
                window.Show();
            }
            catch (Exception ex)
            {
                ShowFatal(ex);
                Shutdown(1);
            }
            return;
        }

        EnsureConsole();
        Console.WriteLine(loc["Cli_InvalidArgs"]);
        Shutdown(1);
    }

    private void RegisterGlobalExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowFatal(e.Exception);
        e.Handled = true;
        Shutdown(1);
    }

    private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ShowFatal(ex);
    }

    private static void ShowFatal(Exception ex)
    {
        try
        {
            LocalizationManager loc = LocalizationManager.Instance;
            string logPath = Path.Combine(AppContext.BaseDirectory, "MMCSS-TWEAKER_crash.txt");
            File.WriteAllText(logPath, ex.ToString(), Encoding.UTF8);

            string typeName = ex.GetType().FullName ?? ex.GetType().Name;
            UiDialog.Error(
                Application.Current?.MainWindow,
                "MMCSS-TWEAKER",
                loc.Format("Msg_FatalError", typeName, ex.Message ?? string.Empty, logPath));
        }
        catch
        {
        }
    }

    private static bool HasArg(string[] args, string name)
    {
        return args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureConsole()
    {
        try
        {
            if (!NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS))
                NativeMethods.AllocConsole();

            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
        }
        catch
        {
        }
    }

    private static void PrintUsage()
    {
        LocalizationManager loc = LocalizationManager.Instance;

        Console.WriteLine(loc["Cli_UsageTitle"]);
        Console.WriteLine();
        Console.WriteLine(loc["Cli_Usage"]);
        Console.WriteLine("  MMCSS-TWEAKER.exe --gui");
        Console.WriteLine("  MMCSS-TWEAKER.exe --auto-optimize");
        Console.WriteLine();
        Console.WriteLine(loc["Cli_Options"]);
        Console.WriteLine(loc["Cli_OptionAutoOptimize"]);
        Console.WriteLine(loc["Cli_OptionGui"]);
    }

    private static void TrySetAppUserModelId()
    {
        try
        {
            _ = NativeMethods.SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
        }
        catch
        {
        }
    }
}

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using MMCSSTweaker.Core;
using MMCSSTweaker.Win32;

namespace MMCSSTweaker;

public partial class MainWindow : Window
{
    private const string DeveloperUrl = "https://t.me/arsenzaa";
    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) => RefreshFromRegistry();
        SourceInitialized += (_, _) => ApplyTitleBarColors();

        string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "custom_icon.ico");
        if (File.Exists(iconPath))
        {
            try
            {
                Icon = BitmapFrame.Create(new Uri(iconPath));
            }
            catch
            {
            }
        }
    }

    private void TelegramLink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = true;

        if (OpenUrl(DeveloperUrl))
            return;

        LocalizationManager loc = LocalizationManager.Instance;
        UiDialog.Warning(
            this,
            loc["Title_Telegram"],
            loc.Format("Msg_TelegramOpenFailed", DeveloperUrl));
    }

    private void LanguageToggle_OnClick(object sender, RoutedEventArgs e)
    {
        LocalizationManager loc = LocalizationManager.Instance;
        loc.Language = loc.Language == UiLanguage.En ? UiLanguage.Ru : UiLanguage.En;
    }

    private void ApplyAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        bool successSystem = SystemTab.ApplySettings(showMessage: false);
        bool successAudio = AudioTab.ApplySettings(showMessage: false);
        LocalizationManager loc = LocalizationManager.Instance;

        if (successSystem && successAudio)
        {
            UiDialog.Info(this, loc["Title_Success"], loc["Msg_ApplyAllSuccess"]);
        }
        else
        {
            UiDialog.Warning(this, loc["Title_Error"], loc["Msg_ApplyAllPartial"]);
        }

        RefreshFromRegistry();
    }

    private void AutoOptimizeAllButton_OnClick(object sender, RoutedEventArgs e)
    {
        LocalizationManager loc = LocalizationManager.Instance;
        if (!AutoOptimize.ApplyAutoOptimization(out string? error))
        {
            string message = loc["Msg_AutoOptimizeAllPartial"];
            if (!string.IsNullOrWhiteSpace(error))
                message = $"{message}\n\n{error}";

            UiDialog.Warning(this, loc["Title_AutoOptimization"], message);
            return;
        }

        UiDialog.Info(this, loc["Title_AutoOptimization"], loc["Msg_AutoOptimizeAllSuccess"]);
        RefreshFromRegistry();
    }

    private void RefreshButton_OnClick(object sender, RoutedEventArgs e)
    {
        RefreshFromRegistry();
    }

    private void RestoreBackupButton_OnClick(object sender, RoutedEventArgs e)
    {
        LocalizationManager loc = LocalizationManager.Instance;

        if (!BackupRegManager.RestoreBackup(out string? error, out _))
        {
            UiDialog.Error(this, loc["Title_Backup"], loc.Format("Msg_RestoreBackupFailed", error ?? string.Empty));
            return;
        }

        UiDialog.Info(this, loc["Title_Backup"], loc["Msg_RestoreBackupSuccess"]);

        RefreshFromRegistry();
    }

    private void ApplyTitleBarColors()
    {
        try
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
                return;

            Color background = GetResourceColor("ControlBackground", Colors.Black);
            int captionColor = ToColorRef(background);
            _ = NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

            Color foreground = GetResourceColor("LabelForeground", Colors.White);
            int textColor = ToColorRef(foreground);
            _ = NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
        }
        catch
        {
        }
    }

    private Color GetResourceColor(string key, Color fallback)
    {
        if (TryFindResource(key) is SolidColorBrush brush)
            return brush.Color;

        return fallback;
    }

    private static int ToColorRef(Color color)
    {
        return color.R | (color.G << 8) | (color.B << 16);
    }

    private void RefreshFromRegistry()
    {
        SystemTab.LoadSettings();
        AudioTab.LoadSettings();
    }

    private static bool OpenUrl(string url)
    {
        return TryOpenUrl(url);
    }

    private static bool TryOpenUrl(string url)
    {
        if (TryStartProcess(url, arguments: null))
            return true;

        return TryStartProcess("explorer.exe", url);
    }

    private static bool TryStartProcess(string fileName, string? arguments)
    {
        try
        {
            ProcessStartInfo info = new(fileName)
            {
                UseShellExecute = true,
            };

            if (!string.IsNullOrWhiteSpace(arguments))
                info.Arguments = arguments;

            _ = Process.Start(info);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

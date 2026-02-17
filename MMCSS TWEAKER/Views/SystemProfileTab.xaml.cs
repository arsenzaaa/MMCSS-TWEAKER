using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using MMCSSTweaker.Core;

namespace MMCSSTweaker.Views;

public partial class SystemProfileTab : UserControl
{
    private static readonly Regex HexRegex = new("^[0-9A-Fa-f]+$", RegexOptions.Compiled);

    public event EventHandler? AutoOptimizeRequested;

    public SystemProfileTab()
    {
        InitializeComponent();
        NoLazyMode.SelectedIndex = -1;
        LocalizationManager.Instance.PropertyChanged += LocalizationManager_OnPropertyChanged;
        Unloaded += (_, _) => LocalizationManager.Instance.PropertyChanged -= LocalizationManager_OnPropertyChanged;
        ToggleLazyModeVisibility();
        UpdateNetworkThrottlingState();
    }

    public void LoadSettings()
    {
        bool hasMaxThreadsPer = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.SystemProfile,
            "MaxThreadsPerProcess",
            out uint maxThreadsPer);
        if (hasMaxThreadsPer)
        {
            MaxThreadsPerProcess.Value = (int)maxThreadsPer;
            MaxThreadsPerProcess.IsUnset = false;
        }
        else
        {
            MaxThreadsPerProcess.Value = MaxThreadsPerProcess.Minimum;
            MaxThreadsPerProcess.IsUnset = true;
        }

        bool hasMaxThreadsTotal = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.SystemProfile,
            "MaxThreadsTotal",
            out uint maxThreadsTotal);
        if (hasMaxThreadsTotal)
        {
            MaxThreadsTotal.Value = (int)maxThreadsTotal;
            MaxThreadsTotal.IsUnset = false;
        }
        else
        {
            MaxThreadsTotal.Value = MaxThreadsTotal.Minimum;
            MaxThreadsTotal.IsUnset = true;
        }

        bool hasNetworkThrottling = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.SystemProfile,
            "NetworkThrottlingIndex",
            out uint networkThrottling);

        if (hasNetworkThrottling && networkThrottling == 0xFFFFFFFF)
        {
            NetworkUnlimited.IsChecked = true;
            NetworkThrottlingIndex.IsEnabled = false;
            NetworkThrottlingIndex.Value = 1;
            NetworkThrottlingIndex.IsUnset = false;
        }
        else
        {
            NetworkUnlimited.IsChecked = false;
            NetworkThrottlingIndex.IsEnabled = true;

            if (hasNetworkThrottling)
            {
                uint normalizedNetworkThrottling = networkThrottling == 0 ? 1u : networkThrottling;
                NetworkThrottlingIndex.Value = (int)normalizedNetworkThrottling;
                NetworkThrottlingIndex.IsUnset = false;
            }
            else
            {
                NetworkThrottlingIndex.Value = NetworkThrottlingIndex.Minimum;
                NetworkThrottlingIndex.IsUnset = true;
            }
        }

        bool hasTimerRes = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.SystemProfile,
            "SchedulerTimerResolution",
            out uint timerRes);
        if (hasTimerRes)
        {
            SchedulerTimerResolution.Value = (int)timerRes;
            SchedulerTimerResolution.IsUnset = false;
        }
        else
        {
            SchedulerTimerResolution.Value = SchedulerTimerResolution.Minimum;
            SchedulerTimerResolution.IsUnset = true;
        }

        bool hasNoLazyMode = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.SystemProfile,
            "NoLazyMode",
            out uint noLazyMode);
        if (!hasNoLazyMode)
        {
            NoLazyMode.SelectedIndex = -1;
        }
        else
        {
            NoLazyMode.SelectedIndex = noLazyMode == 0 ? 0 : 1;
        }

        bool hasIdleCycles = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.SystemProfile,
            "IdleDetectionCycles",
            out uint idleCycles);
        if (hasIdleCycles)
        {
            IdleDetectionCycles.Value = (int)idleCycles;
            IdleDetectionCycles.IsUnset = false;
        }
        else
        {
            IdleDetectionCycles.Value = IdleDetectionCycles.Minimum;
            IdleDetectionCycles.IsUnset = true;
        }

        bool hasSchedPeriod = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.SystemProfile,
            "SchedulerPeriod",
            out uint schedPeriod);
        if (hasSchedPeriod)
        {
            SchedulerPeriod.Value = (int)schedPeriod;
            SchedulerPeriod.IsUnset = false;
        }
        else
        {
            SchedulerPeriod.Value = SchedulerPeriod.Minimum;
            SchedulerPeriod.IsUnset = true;
        }

        bool hasLazyTimeout = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.SystemProfile,
            "LazyModeTimeout",
            out uint lazyTimeout);
        LazyModeTimeout.Text = hasLazyTimeout ? lazyTimeout.ToString("X8") : UiConstants.UnsetValue;

        bool hasSysResp = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.SystemProfile,
            "SystemResponsiveness",
            out uint sysResp);
        if (hasSysResp)
        {
            SystemResponsiveness.Value = (int)sysResp;
            SystemResponsiveness.IsUnset = false;
        }
        else
        {
            SystemResponsiveness.Value = SystemResponsiveness.Minimum;
            SystemResponsiveness.IsUnset = true;
        }

        ToggleLazyModeVisibility();
        UpdateNetworkThrottlingState();
    }

    public bool ApplySettings(bool showMessage = true)
    {
        bool success = ApplySettingsInternal();

        if (showMessage)
        {
            LocalizationManager loc = LocalizationManager.Instance;
            if (success)
                UiDialog.Info(this, loc["Title_Success"], loc["Msg_SystemProfileApplied"]);
            else
                UiDialog.Warning(this, loc["Title_Error"], loc["Msg_SystemProfileApplyFailed"]);
        }

        return success;
    }

    private bool ApplySettingsInternal()
    {
        uint noLazy = GetSelectedNoLazyMode();
        bool noLazyUnset = NoLazyMode.SelectedIndex < 0;

        uint? lazyTimeout = null;
        string text = (LazyModeTimeout.Text ?? string.Empty).Trim();
        if (text.Length != 0 && !string.Equals(text, UiConstants.UnsetValue, StringComparison.Ordinal))
        {
            if (HexRegex.IsMatch(text) && uint.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out uint parsed))
                lazyTimeout = parsed;
            else
                lazyTimeout = 0;
        }

        bool success = true;

        if (!MaxThreadsPerProcess.IsUnset)
            success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "MaxThreadsPerProcess", (uint)MaxThreadsPerProcess.Value);

        if (!MaxThreadsTotal.IsUnset)
            success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "MaxThreadsTotal", (uint)MaxThreadsTotal.Value);

        if (NetworkUnlimited.IsChecked == true)
        {
            success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "NetworkThrottlingIndex", 0xFFFFFFFF);
        }
        else if (!NetworkThrottlingIndex.IsUnset)
        {
            success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "NetworkThrottlingIndex", (uint)NetworkThrottlingIndex.Value);
        }

        if (!SchedulerTimerResolution.IsUnset)
            success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "SchedulerTimerResolution", (uint)SchedulerTimerResolution.Value);

        if (!noLazyUnset)
            success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "NoLazyMode", noLazy);

        if (!noLazyUnset && noLazy == 0)
        {
            if (!IdleDetectionCycles.IsUnset)
                success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "IdleDetectionCycles", (uint)IdleDetectionCycles.Value);

            if (!SchedulerPeriod.IsUnset)
                success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "SchedulerPeriod", (uint)SchedulerPeriod.Value);

            if (lazyTimeout is { } timeoutValue)
                success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "LazyModeTimeout", timeoutValue);
        }

        if (!SystemResponsiveness.IsUnset)
            success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "SystemResponsiveness", (uint)SystemResponsiveness.Value);

        return success;
    }

    public bool AutoOptimize(bool showMessage = true)
    {
        bool success = MMCSSTweaker.Core.AutoOptimize.ApplySystemProfile();
        if (success)
        {
            NoLazyMode.SelectedIndex = 0;
            NetworkThrottlingIndex.Value = 10;
            NetworkUnlimited.IsChecked = false;
            LazyModeTimeout.Text = "FFFFFFFF";
            SchedulerPeriod.Value = 1_000_000;
            IdleDetectionCycles.Value = 2;

            if (showMessage)
            {
                LocalizationManager loc = LocalizationManager.Instance;
                UiDialog.Info(this, loc["Title_AutoOptimization"], loc["Msg_SystemProfileOptimized"]);
            }

            AutoOptimizeRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (showMessage)
        {
            LocalizationManager loc = LocalizationManager.Instance;
            UiDialog.Warning(this, loc["Title_Error"], loc["Msg_ApplyFailedGeneric"]);
        }

        return success;
    }

    private void NoLazyMode_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ToggleLazyModeVisibility();
    }

    private void ToggleLazyModeVisibility()
    {
        LocalizationManager loc = LocalizationManager.Instance;
        bool noLazyUnset = NoLazyMode.SelectedIndex < 0;
        bool enabled = !noLazyUnset && GetSelectedNoLazyMode() == 0;
        LazyModeGroup.IsEnabled = enabled;
        if (enabled)
        {
            LazyModeDisabledReason.Visibility = Visibility.Collapsed;
        }
        else
        {
            LazyModeDisabledReason.Visibility = Visibility.Visible;
            LazyModeDisabledReason.Text = noLazyUnset
                ? loc["Reason_LazyMode_Unset"]
                : loc["Reason_LazyMode_Disabled"];
        }
    }

    private uint GetSelectedNoLazyMode()
    {
        if (NoLazyMode.SelectedItem is ComboBoxItem { Tag: { } tag } && uint.TryParse(tag.ToString(), out uint value))
            return value;
        return 0;
    }

    private void NetworkUnlimited_OnChanged(object sender, RoutedEventArgs e)
    {
        UpdateNetworkThrottlingState();
    }

    private void UpdateNetworkThrottlingState()
    {
        NetworkThrottlingIndex.IsEnabled = NetworkUnlimited.IsChecked != true;
        LocalizationManager loc = LocalizationManager.Instance;
        if (NetworkUnlimited.IsChecked == true)
        {
            NetworkThrottlingDisabledReason.Visibility = Visibility.Visible;
            NetworkThrottlingDisabledReason.Text = loc["Reason_NetworkThrottling_Unlimited"];
        }
        else
        {
            NetworkThrottlingDisabledReason.Visibility = Visibility.Collapsed;
        }
    }

    private void LocalizationManager_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LocalizationManager.Language) or "Item[]")
        {
            ToggleLazyModeVisibility();
            UpdateNetworkThrottlingState();
        }
    }

    private void UnsetTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox &&
            string.Equals((textBox.Text ?? string.Empty).Trim(), UiConstants.UnsetValue, StringComparison.Ordinal))
        {
            textBox.SelectAll();
        }
    }

    private void HexTextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Text))
            return;

        foreach (char c in e.Text)
        {
            bool isHex = (c >= '0' && c <= '9') ||
                         (c >= 'a' && c <= 'f') ||
                         (c >= 'A' && c <= 'F');
            if (!isHex)
            {
                e.Handled = true;
                return;
            }
        }
    }
}

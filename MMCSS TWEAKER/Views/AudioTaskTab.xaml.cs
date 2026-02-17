using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using MMCSSTweaker.Core;

namespace MMCSSTweaker.Views;

public partial class AudioTaskTab : UserControl
{
    private static readonly Regex HexRegex = new("^[0-9A-Fa-f]+$", RegexOptions.Compiled);

    private const int DefaultAudioPriority = 6;
    private const int DefaultBackgroundPriority = 1;
    private const int DefaultPriorityWhenYielded = 1;

    private readonly bool _hasNetAdapterCx;
    private uint _currentPriority;
    private uint _currentBgPriority;
    private uint _currentYieldedPriority;
    private uint _currentRwtpPriority;
    private bool _initializing = true;
    private bool _ndisThreadDisabledByThrottle;

    public AudioTaskTab()
    {
        InitializeComponent();
        _hasNetAdapterCx = NetAdapterCxDetector.HasNetAdapterCx();
        UpdateLocalizedText();
        LocalizationManager.Instance.PropertyChanged += LocalizationManager_OnPropertyChanged;
        Unloaded += (_, _) => LocalizationManager.Instance.PropertyChanged -= LocalizationManager_OnPropertyChanged;

        SchedulingCategory.SelectedIndex = -1;
        LatencySensitive.SelectedIndex = -1;

        UpdateControlStates();
        UpdateThreadStatus();
        _initializing = false;
    }

    private void LocalizationManager_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LocalizationManager.Language) or "Item[]")
        {
            UpdateLocalizedText();
            UpdateControlStates();
            UpdateThreadStatus();
        }
    }

    private void UpdateLocalizedText()
    {
        LocalizationManager loc = LocalizationManager.Instance;
        NdisLabel.Text = _hasNetAdapterCx ? loc["Label_NetAdapterCxThreads"] : loc["Label_NdisThread"];
    }

    public void LoadSettings()
    {
        bool hasAffinity = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.AudioTask,
            "Affinity",
            out uint affinity);

        bool hasLatency = RegistryUtils.TryReadString(
            RegistryHive.LocalMachine,
            RegistryPaths.AudioTask,
            "Latency Sensitive",
            out string latency);
        latency = latency.Trim();
        hasLatency = hasLatency && !string.IsNullOrWhiteSpace(latency);

        bool hasSchedCat = RegistryUtils.TryReadString(
            RegistryHive.LocalMachine,
            RegistryPaths.AudioTask,
            "Scheduling Category",
            out string schedCat);
        schedCat = schedCat.Trim();
        hasSchedCat = hasSchedCat && !string.IsNullOrWhiteSpace(schedCat);

        bool hasPriority = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.AudioTask,
            "Priority",
            out uint priority);

        bool hasBgPriority = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.AudioTask,
            "BackgroundPriority",
            out uint bgPriority);

        bool hasYieldedPriority = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.AudioTask,
            "Priority When Yielded",
            out uint yieldedPriority);

        bool hasRwtpPriority = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.NdisParameters,
            "ReceiveWorkerThreadPriority",
            out uint rwtpPriority);

        bool hasNetworkThrottling = RegistryUtils.TryReadDword(
            RegistryHive.LocalMachine,
            RegistryPaths.SystemProfile,
            "NetworkThrottlingIndex",
            out uint networkThrottling);
        _ndisThreadDisabledByThrottle = hasNetworkThrottling && networkThrottling == 0xFFFFFFFF;

        _currentPriority = hasPriority ? priority : 1;
        _currentBgPriority = hasBgPriority ? bgPriority : 1;
        _currentYieldedPriority = hasYieldedPriority ? yieldedPriority : 1;
        uint normalizedRwtpPriority = NormalizeReceiveWorkerThreadPriority(rwtpPriority);
        _currentRwtpPriority = hasRwtpPriority ? normalizedRwtpPriority : 8;

        Affinity.Text = hasAffinity ? affinity.ToString("X") : UiConstants.UnsetValue;

        if (hasLatency)
            SetComboBoxByText(LatencySensitive, latency);
        else
            LatencySensitive.SelectedIndex = -1;

        if (hasSchedCat)
            SetComboBoxByText(SchedulingCategory, schedCat);
        else
            SchedulingCategory.SelectedIndex = -1;

        if (hasPriority)
        {
            Priority.Value = (int)priority;
            Priority.IsUnset = false;
        }
        else
        {
            Priority.Value = Priority.Minimum;
            Priority.IsUnset = true;
        }

        if (hasBgPriority)
        {
            BackgroundPriority.Value = (int)bgPriority;
            BackgroundPriority.IsUnset = false;
        }
        else
        {
            BackgroundPriority.Value = BackgroundPriority.Minimum;
            BackgroundPriority.IsUnset = true;
        }

        if (hasYieldedPriority)
        {
            PriorityWhenYielded.Value = (int)yieldedPriority;
            PriorityWhenYielded.IsUnset = false;
        }
        else
        {
            PriorityWhenYielded.Value = PriorityWhenYielded.Minimum;
            PriorityWhenYielded.IsUnset = true;
        }

        if (hasRwtpPriority)
        {
            ReceiveWorkerThreadPriority.Value = (int)normalizedRwtpPriority;
            ReceiveWorkerThreadPriority.IsUnset = false;
        }
        else
        {
            ReceiveWorkerThreadPriority.Value = ReceiveWorkerThreadPriority.Minimum;
            ReceiveWorkerThreadPriority.IsUnset = true;
        }

        UpdateControlStates();
        UpdateThreadStatus();
    }

    public bool AutoOptimize(bool showMessage = true)
    {
        bool success = MMCSSTweaker.Core.AutoOptimize.ApplyAudioTasks();
        if (success)
        {
            SetComboBoxByText(SchedulingCategory, "Medium");
            Priority.Value = 1;
            PriorityWhenYielded.Value = 1;

            if (showMessage)
            {
                LocalizationManager loc = LocalizationManager.Instance;
                UiDialog.Info(this, loc["Title_AutoOptimization"], loc["Msg_AudioOptimized"]);
            }
        }
        else if (showMessage)
        {
            LocalizationManager loc = LocalizationManager.Instance;
            UiDialog.Warning(this, loc["Title_Error"], loc["Msg_AudioApplyFailed"]);
        }

        return success;
    }

    public bool ApplySettings(bool showMessage = true)
    {
        uint? affinity = ParseHexOrNull(Affinity.Text);
        string? latency = GetComboBoxValueOrNull(LatencySensitive);
        string? schedCat = GetComboBoxValueOrNull(SchedulingCategory);

        uint? priority = null;
        uint? bgPriority = null;
        uint? yieldedPriority = null;
        uint? rwtpPriority = null;

        if (schedCat is not null)
        {
            if (schedCat == "Medium" && !Priority.IsUnset)
                priority = (uint)Priority.Value;

            if (schedCat == "Low" && !BackgroundPriority.IsUnset)
                bgPriority = (uint)BackgroundPriority.Value;

            if (schedCat != "Low" && !PriorityWhenYielded.IsUnset)
                yieldedPriority = (uint)PriorityWhenYielded.Value;

            if (!_hasNetAdapterCx && schedCat != "Low" && !ReceiveWorkerThreadPriority.IsUnset)
                rwtpPriority = NormalizeReceiveWorkerThreadPriority((uint)ReceiveWorkerThreadPriority.Value);
        }

        bool success = true;
        success &= ApplyAudioTask(RegistryPaths.AudioTask, affinity, latency, schedCat, priority, bgPriority, yieldedPriority);
        success &= ApplyAudioTask(RegistryPaths.ProAudioTask, affinity, latency, schedCat, priority, bgPriority, yieldedPriority);

        if (rwtpPriority is { } rwtpValue)
        {
            success &= RegistryUtils.WriteDword(
                RegistryHive.LocalMachine,
                RegistryPaths.NdisParameters,
                "ReceiveWorkerThreadPriority",
                rwtpValue);
        }

        if (success)
        {
            if (showMessage)
            {
                LocalizationManager loc = LocalizationManager.Instance;
                UiDialog.Info(this, loc["Title_Success"], loc["Msg_AudioApplied"]);
            }

            LoadSettings();
        }
        else if (showMessage)
        {
            LocalizationManager loc = LocalizationManager.Instance;
            UiDialog.Warning(this, loc["Title_Error"], loc["Msg_AudioThreadsApplyFailed"]);
        }

        return success;
    }

    private static bool ApplyAudioTask(
        string subKeyPath,
        uint? affinity,
        string? latencySensitive,
        string? schedulingCategory,
        uint? priority,
        uint? backgroundPriority,
        uint? priorityWhenYielded)
    {
        bool success = true;

        if (affinity is { } affinityValue)
            success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, subKeyPath, "Affinity", affinityValue);

        if (latencySensitive is { } latencyValue)
            success &= RegistryUtils.WriteString(RegistryHive.LocalMachine, subKeyPath, "Latency Sensitive", latencyValue);

        if (schedulingCategory is { } categoryValue)
            success &= RegistryUtils.WriteString(RegistryHive.LocalMachine, subKeyPath, "Scheduling Category", categoryValue);

        if (priority is { } priorityValue)
            success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, subKeyPath, "Priority", priorityValue);

        if (backgroundPriority is { } bgPriorityValue)
            success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, subKeyPath, "BackgroundPriority", bgPriorityValue);

        if (priorityWhenYielded is { } yieldedValue)
            success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, subKeyPath, "Priority When Yielded", yieldedValue);

        return success;
    }

    private void SchedulingCategory_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing)
            return;

        UpdateControlStates();
        UpdateThreadStatus();
    }

    private void AnyValueChanged(object? sender, RoutedPropertyChangedEventArgs<int> e)
    {
        if (_initializing)
            return;

        UpdateThreadStatus();
    }

    private void UpdateControlStates()
    {
        LocalizationManager loc = LocalizationManager.Instance;
        string selectedCategory = GetComboBoxText(SchedulingCategory).Trim();
        bool categoryUnset = selectedCategory.Length == 0 || string.Equals(selectedCategory, UiConstants.UnsetValue, StringComparison.Ordinal);
        string category = categoryUnset ? string.Empty : selectedCategory;
        bool isLow = string.Equals(category, "Low", StringComparison.OrdinalIgnoreCase);
        bool isMedium = string.Equals(category, "Medium", StringComparison.OrdinalIgnoreCase);
        bool isHigh = string.Equals(category, "High", StringComparison.OrdinalIgnoreCase);

        PriorityGroup.IsEnabled = isMedium;
        if (isMedium)
        {
            PriorityDisabledReason.Visibility = Visibility.Collapsed;
        }
        else
        {
            PriorityDisabledReason.Visibility = Visibility.Visible;
            if (categoryUnset)
                PriorityDisabledReason.Text = loc["Reason_Priority_Unset"];
            else if (isLow)
                PriorityDisabledReason.Text = loc["Reason_Priority_Low"];
            else
                PriorityDisabledReason.Text = loc["Reason_Priority_High"];
        }

        BackgroundPriorityGroup.IsEnabled = isLow;
        if (isLow)
        {
            BackgroundPriorityDisabledReason.Visibility = Visibility.Collapsed;
        }
        else
        {
            BackgroundPriorityDisabledReason.Visibility = Visibility.Visible;
            BackgroundPriorityDisabledReason.Text = categoryUnset
                ? loc["Reason_Background_Unset"]
                : loc["Reason_Background_NotLow"];
        }

        YieldedPriorityGroup.IsEnabled = !isLow && !categoryUnset;
        if (YieldedPriorityGroup.IsEnabled)
        {
            YieldedPriorityDisabledReason.Visibility = Visibility.Collapsed;
        }
        else
        {
            YieldedPriorityDisabledReason.Visibility = Visibility.Visible;
            YieldedPriorityDisabledReason.Text = categoryUnset
                ? loc["Reason_Yielded_Unset"]
                : loc["Reason_Yielded_Low"];
        }

        bool ndisEnabled = !_hasNetAdapterCx && !categoryUnset && !isLow && !_ndisThreadDisabledByThrottle;
        NdisSettingsGroup.IsEnabled = ndisEnabled;
        if (ndisEnabled)
        {
            NdisDisabledReason.Visibility = Visibility.Collapsed;
        }
        else
        {
            NdisDisabledReason.Visibility = Visibility.Visible;
            if (_hasNetAdapterCx)
                NdisDisabledReason.Text = loc["Reason_Ndis_NetAdapterCx"];
            else if (_ndisThreadDisabledByThrottle)
                NdisDisabledReason.Text = loc["Reason_Ndis_ThrottlingDisabled"];
            else if (categoryUnset)
                NdisDisabledReason.Text = loc["Reason_Ndis_Unset"];
            else
                NdisDisabledReason.Text = loc["Reason_Ndis_Low"];
        }
    }

    private int GetNdisPriority()
    {
        int value = ReceiveWorkerThreadPriority.Value;
        if (value == 8 || (16 <= value && value <= 31))
            return value;
        return (int)_currentRwtpPriority;
    }

    private static uint NormalizeReceiveWorkerThreadPriority(uint value)
    {
        if (value == 8 || (value >= 16 && value <= 31))
            return value;
        return 8;
    }

    private void UpdateThreadStatus()
    {
        LocalizationManager loc = LocalizationManager.Instance;
        string selectedCategory = GetComboBoxText(SchedulingCategory).Trim();
        bool categoryUnset = selectedCategory.Length == 0 || string.Equals(selectedCategory, UiConstants.UnsetValue, StringComparison.Ordinal);
        string category = categoryUnset ? "Low" : selectedCategory;

        if (category == "Low")
        {
            MmcssStatus.Text = categoryUnset ? UiConstants.UnsetValue : loc["Status_Inactive"];
            if (categoryUnset)
            {
                AudioThreadsPriority.Text = UiConstants.UnsetValue;
            }
            else
            {
                int bgPriority = BackgroundPriority.IsUnset ? DefaultBackgroundPriority : BackgroundPriority.Value;
                AudioThreadsPriority.Text = (bgPriority + 7).ToString();
            }
        }
        else
        {
            MmcssStatus.Text = categoryUnset ? UiConstants.UnsetValue : loc["Status_MmcssActiveFixed"];

            if (categoryUnset)
            {
                AudioThreadsPriority.Text = UiConstants.UnsetValue;
            }
            else
            {
                int boosted;
                if (category == "Medium")
                {
                    int prio = Priority.IsUnset ? DefaultAudioPriority : Priority.Value;
                    boosted = prio is 7 or 8 ? 23 : 16 + prio;
                }
                else
                {
                    boosted = 25;
                }

                int nonBoosted = PriorityWhenYielded.IsUnset ? DefaultPriorityWhenYielded : PriorityWhenYielded.Value;
                AudioThreadsPriority.Text = loc.Format("Status_AudioNoBoost", boosted, nonBoosted);
            }
        }

        if (category == "Low")
        {
            NdisStatus.Text = categoryUnset ? UiConstants.UnsetValue : loc["Status_Inactive"];
        }
        else
        {
            if (categoryUnset)
            {
                NdisStatus.Text = UiConstants.UnsetValue;
            }
            else if (_hasNetAdapterCx)
            {
                NdisStatus.Text = loc["Status_NdisActiveFixed"];
            }
            else
            {
                if (_ndisThreadDisabledByThrottle)
                {
                    NdisStatus.Text = loc["Status_Inactive"];
                }
                else if (ReceiveWorkerThreadPriority.IsUnset)
                {
                    NdisStatus.Text = UiConstants.UnsetValue;
                }
                else
                {
                    int ndisPrio = GetNdisPriority();
                    NdisStatus.Text = loc.Format("Status_ActivePriority", ndisPrio);
                }
            }
        }
    }

    private static uint? ParseHexOrNull(string? input)
    {
        string text = (input ?? string.Empty).Trim();
        if (text.Length == 0 || string.Equals(text, UiConstants.UnsetValue, StringComparison.Ordinal))
            return null;

        if (!HexRegex.IsMatch(text))
            return 0;

        return uint.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out uint parsed) ? parsed : 0;
    }

    private static string GetComboBoxText(ComboBox comboBox)
    {
        return comboBox.SelectedItem switch
        {
            ComboBoxItem comboItem => comboItem.Content?.ToString() ?? string.Empty,
            _ => comboBox.Text ?? string.Empty
        };
    }

    private static string? GetComboBoxValueOrNull(ComboBox comboBox)
    {
        string text = GetComboBoxText(comboBox).Trim();
        if (text.Length == 0 || string.Equals(text, UiConstants.UnsetValue, StringComparison.Ordinal))
            return null;
        return text;
    }

    private static void SetComboBoxByText(ComboBox comboBox, string text)
    {
        foreach (object item in comboBox.Items)
        {
            if (item is ComboBoxItem comboItem &&
                string.Equals(comboItem.Content?.ToString(), text, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = comboItem;
                return;
            }
        }

        comboBox.Text = text;
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

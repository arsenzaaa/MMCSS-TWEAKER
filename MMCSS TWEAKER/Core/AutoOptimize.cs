using Microsoft.Win32;

namespace MMCSSTweaker.Core;

internal enum MmcssPreset
{
    Fps,
    Network,
}

internal static class AutoOptimize
{
    internal static bool ApplyAutoOptimization(out string? error)
    {
        return NetAdapterCxDetector.IsPrimaryAdapterNetAdapterCx()
            ? ApplyAutoOptimizationNetAdapterCx(out error)
            : ApplyAutoOptimizationNdis(out error);
    }

    internal static bool ApplySystemProfile() => ApplySystemProfile(MmcssPreset.Fps);

    internal static bool ApplySystemProfile(MmcssPreset preset)
    {
        return preset switch
        {
            MmcssPreset.Fps => ApplySystemProfileFps(),
            MmcssPreset.Network => ApplySystemProfileNetwork(),
            _ => false,
        };
    }

    internal static bool ApplyAudioTasks() => ApplyAudioTasks(MmcssPreset.Fps);

    internal static bool ApplyAudioTasks(MmcssPreset preset)
    {
        return preset switch
        {
            MmcssPreset.Fps => ApplyAudioTasksFps(),
            MmcssPreset.Network => ApplyAudioTasksNetwork(),
            _ => false,
        };
    }

    private static bool ApplySystemProfileFps()
    {
        var settings = new Dictionary<string, uint>
        {
            ["NoLazyMode"] = 0,
            ["NetworkThrottlingIndex"] = 10,
            ["LazyModeTimeout"] = 0xFFFFFFFF,
            ["SchedulerPeriod"] = 1_000_000,
            ["IdleDetectionCycles"] = 2,
        };

        bool success = true;
        foreach ((string name, uint value) in settings)
            success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, name, value);

        return success;
    }

    private static bool ApplySystemProfileNetwork()
    {
        return RegistryUtils.WriteDword(
            RegistryHive.LocalMachine,
            RegistryPaths.SystemProfile,
            "NetworkThrottlingIndex",
            0xFFFFFFFF);
    }

    private static bool ApplyAudioTasksFps()
    {
        var dwordSettings = new Dictionary<string, uint>
        {
            ["Priority"] = 1,
            ["Priority When Yielded"] = 1,
        };

        const string schedulingCategory = "Medium";

        bool success = true;
        foreach (string subKey in new[] { RegistryPaths.AudioTask, RegistryPaths.ProAudioTask })
        {
            foreach ((string name, uint value) in dwordSettings)
                success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, subKey, name, value);

            success &= RegistryUtils.WriteString(RegistryHive.LocalMachine, subKey, "Scheduling Category", schedulingCategory);
        }

        return success;
    }

    private static bool ApplyAudioTasksNetwork()
    {
        var dwordSettings = new Dictionary<string, uint>
        {
            ["Priority"] = 1,
            ["BackgroundPriority"] = 3,
        };

        const string schedulingCategory = "Low";

        bool success = true;
        foreach (string subKey in new[] { RegistryPaths.AudioTask, RegistryPaths.ProAudioTask })
        {
            foreach ((string name, uint value) in dwordSettings)
                success &= RegistryUtils.WriteDword(RegistryHive.LocalMachine, subKey, name, value);

            success &= RegistryUtils.WriteString(RegistryHive.LocalMachine, subKey, "Scheduling Category", schedulingCategory);
        }

        return success;
    }

    private static bool ApplyAutoOptimizationNetAdapterCx(out string? error)
    {
        var failures = new List<string>();

        void WriteDword(string subKey, string valueName, uint value)
        {
            if (!RegistryUtils.WriteDword(RegistryHive.LocalMachine, subKey, valueName, value))
                failures.Add($"{subKey}\\{valueName}");
        }

        void WriteString(string subKey, string valueName, string value)
        {
            if (!RegistryUtils.WriteString(RegistryHive.LocalMachine, subKey, valueName, value))
                failures.Add($"{subKey}\\{valueName}");
        }

        WriteDword(RegistryPaths.SystemProfile, "NetworkThrottlingIndex", 0xFFFFFFFF);
        WriteDword(RegistryPaths.SystemProfile, "SystemResponsiveness", 0x14);
        WriteDword(RegistryPaths.SystemProfile, "MaxThreadsPerProcess", 0x08);
        WriteDword(RegistryPaths.SystemProfile, "MaxThreadsTotal", 0x40);
        WriteDword(RegistryPaths.SystemProfile, "NoLazyMode", 0);
        WriteDword(RegistryPaths.SystemProfile, "SchedulerTimerResolution", 0x2710);
        WriteDword(RegistryPaths.SystemProfile, "LazyModeTimeout", 0xFFFFFFFF);
        WriteDword(RegistryPaths.SystemProfile, "SchedulerPeriod", 1_000_000);
        WriteDword(RegistryPaths.SystemProfile, "IdleDetectionCycles", 2);

        foreach (string subKey in new[] { RegistryPaths.AudioTask, RegistryPaths.ProAudioTask })
        {
            WriteDword(subKey, "Priority", 1);
            WriteDword(subKey, "Priority When Yielded", 16);
            WriteDword(subKey, "BackgroundPriority", 3);
            WriteString(subKey, "Scheduling Category", "Low");
            WriteString(subKey, "Latency Sensitive", "False");
        }

        WriteString(RegistryPaths.AudioTask, "Background Only", "True");
        WriteString(RegistryPaths.ProAudioTask, "Background Only", "False");

        return FinalizeAutoOptimization(failures, out error);
    }

    private static bool ApplyAutoOptimizationNdis(out string? error)
    {
        var failures = new List<string>();

        void WriteDword(string subKey, string valueName, uint value)
        {
            if (!RegistryUtils.WriteDword(RegistryHive.LocalMachine, subKey, valueName, value))
                failures.Add($"{subKey}\\{valueName}");
        }

        void WriteString(string subKey, string valueName, string value)
        {
            if (!RegistryUtils.WriteString(RegistryHive.LocalMachine, subKey, valueName, value))
                failures.Add($"{subKey}\\{valueName}");
        }

        WriteDword(RegistryPaths.SystemProfile, "NoLazyMode", 0);
        WriteDword(RegistryPaths.SystemProfile, "NetworkThrottlingIndex", 10);
        WriteDword(RegistryPaths.SystemProfile, "LazyModeTimeout", 0xFFFFFFFF);
        WriteDword(RegistryPaths.SystemProfile, "SchedulerPeriod", 1_000_000);
        WriteDword(RegistryPaths.SystemProfile, "IdleDetectionCycles", 2);

        foreach (string subKey in new[] { RegistryPaths.AudioTask, RegistryPaths.ProAudioTask })
        {
            WriteDword(subKey, "Priority", 1);
            WriteDword(subKey, "Priority When Yielded", 1);
            WriteString(subKey, "Scheduling Category", "Medium");
            WriteString(subKey, "Latency Sensitive", "False");
        }

        return FinalizeAutoOptimization(failures, out error);
    }

    private static bool FinalizeAutoOptimization(List<string> failures, out string? error)
    {
        if (failures.Count == 0)
        {
            error = null;
            return true;
        }

        error = "Failed to write registry values:" + Environment.NewLine + string.Join(Environment.NewLine, failures);
        return false;
    }
}

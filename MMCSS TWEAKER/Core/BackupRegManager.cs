using System;
using Microsoft.Win32;

namespace MMCSSTweaker.Core;

internal static class BackupRegManager
{
    internal static bool RestoreBackup(out string? error, out string? usedPath)
    {
        error = null;
        usedPath = "registry";

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

        void DeleteValue(string subKey, string valueName)
        {
            if (!RegistryUtils.DeleteValue(RegistryHive.LocalMachine, subKey, valueName))
                failures.Add($"{subKey}\\{valueName}");
        }

        WriteDword(RegistryPaths.SystemProfile, "NetworkThrottlingIndex", 0x0a);
        WriteDword(RegistryPaths.SystemProfile, "SystemResponsiveness", 0x0a);
        DeleteValue(RegistryPaths.SystemProfile, "NoLazyMode");
        DeleteValue(RegistryPaths.SystemProfile, "LazyModeTimeout");
        DeleteValue(RegistryPaths.SystemProfile, "SchedulerPeriod");
        DeleteValue(RegistryPaths.SystemProfile, "IdleDetectionCycles");
        DeleteValue(RegistryPaths.SystemProfile, "MaxThreadsPerProcess");
        DeleteValue(RegistryPaths.SystemProfile, "MaxThreadsTotal");
        DeleteValue(RegistryPaths.SystemProfile, "SchedulerTimerResolution");

        WriteDword(RegistryPaths.AudioTask, "Affinity", 0);
        WriteString(RegistryPaths.AudioTask, "Background Only", "True");
        WriteDword(RegistryPaths.AudioTask, "Clock Rate", 0x2710);
        WriteDword(RegistryPaths.AudioTask, "GPU Priority", 0x08);
        WriteDword(RegistryPaths.AudioTask, "Priority", 0x06);
        WriteString(RegistryPaths.AudioTask, "Scheduling Category", "Medium");
        WriteString(RegistryPaths.AudioTask, "SFIO Priority", "Normal");
        DeleteValue(RegistryPaths.AudioTask, "BackgroundPriority");
        DeleteValue(RegistryPaths.AudioTask, "Priority When Yielded");
        DeleteValue(RegistryPaths.AudioTask, "Latency Sensitive");

        WriteDword(RegistryPaths.ProAudioTask, "Affinity", 0);
        WriteString(RegistryPaths.ProAudioTask, "Background Only", "False");
        WriteDword(RegistryPaths.ProAudioTask, "Clock Rate", 0x2710);
        WriteDword(RegistryPaths.ProAudioTask, "GPU Priority", 0x08);
        WriteDword(RegistryPaths.ProAudioTask, "Priority", 0x01);
        WriteString(RegistryPaths.ProAudioTask, "Scheduling Category", "High");
        WriteString(RegistryPaths.ProAudioTask, "SFIO Priority", "Normal");
        DeleteValue(RegistryPaths.ProAudioTask, "Priority When Yielded");
        DeleteValue(RegistryPaths.ProAudioTask, "Latency Sensitive");
        DeleteValue(RegistryPaths.ProAudioTask, "BackgroundPriority");

        DeleteValue(RegistryPaths.NdisParameters, "ReceiveWorkerThreadPriority");

        if (failures.Count == 0)
            return true;

        error = "Failed to reset registry values:" + Environment.NewLine + string.Join(Environment.NewLine, failures);
        return false;
    }
}

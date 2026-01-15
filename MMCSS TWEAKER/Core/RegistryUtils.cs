using Microsoft.Win32;

namespace MMCSSTweaker.Core;

internal static class RegistryUtils
{
    private static RegistryView View =>
        Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;

    internal static bool TryReadDword(RegistryHive hive, string subKeyPath, string valueName, out uint value)
    {
        value = 0;

        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View);
            using RegistryKey? key = baseKey.OpenSubKey(subKeyPath, writable: false);
            if (key is null)
                return false;

            object? raw = key.GetValue(valueName);
            if (raw is null)
                return false;

            switch (raw)
            {
                case int intValue:
                    value = unchecked((uint)intValue);
                    return true;
                case uint uintValue:
                    value = uintValue;
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }

    internal static uint ReadDword(RegistryHive hive, string subKeyPath, string valueName, uint defaultValue)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View);
            using RegistryKey? key = baseKey.OpenSubKey(subKeyPath, writable: false);
            if (key is null)
                return defaultValue;

            object? value = key.GetValue(valueName);
            if (value is null)
                return defaultValue;

            return value switch
            {
                int intValue => unchecked((uint)intValue),
                uint uintValue => uintValue,
                _ => defaultValue
            };
        }
        catch
        {
            return defaultValue;
        }
    }

    internal static bool WriteDword(RegistryHive hive, string subKeyPath, string valueName, uint value)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View);
            using RegistryKey key = baseKey.CreateSubKey(subKeyPath, writable: true);
            key.SetValue(valueName, unchecked((int)value), RegistryValueKind.DWord);
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static bool TryReadString(RegistryHive hive, string subKeyPath, string valueName, out string value)
    {
        value = string.Empty;

        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View);
            using RegistryKey? key = baseKey.OpenSubKey(subKeyPath, writable: false);
            if (key is null)
                return false;

            object? raw = key.GetValue(valueName);
            if (raw is not string stringValue)
                return false;

            value = stringValue;
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static string ReadString(RegistryHive hive, string subKeyPath, string valueName, string defaultValue)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View);
            using RegistryKey? key = baseKey.OpenSubKey(subKeyPath, writable: false);
            if (key is null)
                return defaultValue;

            object? value = key.GetValue(valueName);
            return value as string ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    internal static bool WriteString(RegistryHive hive, string subKeyPath, string valueName, string value)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View);
            using RegistryKey key = baseKey.CreateSubKey(subKeyPath, writable: true);
            key.SetValue(valueName, value, RegistryValueKind.String);
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static bool DeleteValue(RegistryHive hive, string subKeyPath, string valueName)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View);
            using RegistryKey? key = baseKey.OpenSubKey(subKeyPath, writable: true);
            if (key is null)
                return true;

            key.DeleteValue(valueName, throwOnMissingValue: false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

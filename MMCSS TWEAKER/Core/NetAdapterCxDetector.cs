using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Win32;

namespace MMCSSTweaker.Core;

internal static class NetAdapterCxDetector
{
    private static RegistryView View =>
        Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;

    internal static bool IsPrimaryAdapterNetAdapterCx()
    {
        if (TryGetPrimaryAdapterServiceName(out string serviceName))
            return IsNetAdapterCxService(serviceName);

        return HasNetAdapterCx();
    }

    internal static bool HasNetAdapterCx()
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, View);

            using RegistryKey? services = baseKey.OpenSubKey(RegistryPaths.ServicesRoot, writable: false);
            if (services is null)
                return false;

            foreach (string serviceName in services.GetSubKeyNames())
            {
                if (string.Equals(serviceName, "rtcx21", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    using RegistryKey? service = services.OpenSubKey(serviceName, writable: false);
                    if (service is null)
                        continue;

                    string displayName = service.GetValue("DisplayName") as string ?? string.Empty;
                    if (!displayName.Contains("NetAdapter", StringComparison.OrdinalIgnoreCase))
                        continue;

                    using RegistryKey? enumKey = service.OpenSubKey("Enum", writable: false);
                    if (enumKey is not null)
                        return true;
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool TryGetPrimaryAdapterServiceName(out string serviceName)
    {
        serviceName = string.Empty;

        NetworkInterface? adapter = GetPrimaryNetworkInterface();
        if (adapter == null)
            return false;

        string interfaceId = adapter.Id;
        if (string.IsNullOrWhiteSpace(interfaceId))
            return false;

        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, View);
            using RegistryKey? classKey = baseKey.OpenSubKey(RegistryPaths.NetworkAdapterClass, writable: false);
            if (classKey is null)
                return false;

            foreach (string subKeyName in classKey.GetSubKeyNames())
            {
                using RegistryKey? adapterKey = classKey.OpenSubKey(subKeyName, writable: false);
                if (adapterKey is null)
                    continue;

                string netCfgInstanceId = adapterKey.GetValue("NetCfgInstanceId") as string ?? string.Empty;
                if (!string.Equals(netCfgInstanceId, interfaceId, StringComparison.OrdinalIgnoreCase))
                    continue;

                serviceName = adapterKey.GetValue("Service") as string ?? string.Empty;
                return !string.IsNullOrWhiteSpace(serviceName);
            }
        }
        catch
        {
        }

        return false;
    }

    private static NetworkInterface? GetPrimaryNetworkInterface()
    {
        NetworkInterface? best = null;
        int bestScore = -1;

        foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.OperationalStatus != OperationalStatus.Up)
                continue;

            if (adapter.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;

            IPInterfaceProperties props;
            try
            {
                props = adapter.GetIPProperties();
            }
            catch
            {
                continue;
            }

            int score = 0;
            if (HasUsableGateway(props))
                score += 2;
            if (props.UnicastAddresses.Any(a => a.Address.AddressFamily == AddressFamily.InterNetwork))
                score += 1;
            if (props.UnicastAddresses.Any(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6))
                score += 1;

            if (score > bestScore)
            {
                best = adapter;
                bestScore = score;
            }
        }

        return best;
    }

    private static bool HasUsableGateway(IPInterfaceProperties props)
    {
        foreach (GatewayIPAddressInformation gateway in props.GatewayAddresses)
        {
            IPAddress address = gateway.Address;
            if (address.AddressFamily == AddressFamily.InterNetwork && !address.Equals(IPAddress.Any))
                return true;
            if (address.AddressFamily == AddressFamily.InterNetworkV6 && !address.Equals(IPAddress.IPv6Any))
                return true;
        }

        return false;
    }

    private static bool IsNetAdapterCxService(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            return false;

        if (string.Equals(serviceName, "rtcx21", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, View);
            using RegistryKey? services = baseKey.OpenSubKey(RegistryPaths.ServicesRoot, writable: false);
            if (services is null)
                return false;

            using RegistryKey? service = services.OpenSubKey(serviceName, writable: false);
            if (service is null)
                return false;

            string displayName = service.GetValue("DisplayName") as string ?? string.Empty;
            if (displayName.Contains("NetAdapter", StringComparison.OrdinalIgnoreCase))
                return true;

            string description = service.GetValue("Description") as string ?? string.Empty;
            return description.Contains("NetAdapter", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

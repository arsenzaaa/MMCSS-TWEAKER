namespace MMCSSTweaker.Core;

internal static class RegistryPaths
{
    internal const string SystemProfile =
        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile";

    internal const string AudioTask =
        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Audio";

    internal const string ProAudioTask =
        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Pro Audio";

    internal const string NdisParameters =
        @"SYSTEM\CurrentControlSet\Services\NDIS\Parameters";

    internal const string ServicesRoot =
        @"SYSTEM\CurrentControlSet\Services";

    internal const string NetworkAdapterClass =
        @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
}

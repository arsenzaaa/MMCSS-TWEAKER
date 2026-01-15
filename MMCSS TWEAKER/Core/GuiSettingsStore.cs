using System;
using System.IO;

namespace MMCSSTweaker.Core;

public enum UiLanguage
{
    En,
    Ru,
}

public static class GuiSettingsStore
{
    public static void TryDeleteLegacyConfig()
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MMCSSTweaker");
            string path = Path.Combine(dir, "config.json");
            if (File.Exists(path))
                File.Delete(path);

            if (Directory.Exists(dir) &&
                Directory.GetFiles(dir).Length == 0 &&
                Directory.GetDirectories(dir).Length == 0)
            {
                Directory.Delete(dir);
            }
        }
        catch
        {
        }
    }
}

using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MMCSSTweaker.Core;

internal static class RegFileImporter
{
    internal static bool TryImport(string path, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = "Reg file path is empty.";
            return false;
        }

        if (!File.Exists(path))
        {
            error = $"Reg file not found: {path}";
            return false;
        }

        try
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "reg.exe",
                Arguments = $"import \"{path}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using Process? process = Process.Start(startInfo);
            if (process == null)
            {
                error = "Failed to start reg.exe.";
                return false;
            }

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            if (!process.WaitForExit(20_000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                error = "Timeout while importing .reg file.";
                return false;
            }

            if (process.ExitCode == 0)
                return true;

            string details = string.Join(
                Environment.NewLine,
                new[]
                {
                    $"ExitCode: {process.ExitCode}",
                    stdout.Trim(),
                    stderr.Trim(),
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

            error = string.IsNullOrWhiteSpace(details) ? "Unknown error while importing .reg file." : details;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}

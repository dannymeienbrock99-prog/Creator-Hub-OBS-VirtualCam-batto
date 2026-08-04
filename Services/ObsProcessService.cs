using System.Diagnostics;

namespace CreatorHubLive.Services;

public static class ObsProcessService
{
    public static bool IsObsRunning() => Process.GetProcessesByName("obs64").Length > 0;

    public static void StartObs()
    {
        if (IsObsRunning())
            return;

        string[] candidates =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "obs-studio", "bin", "64bit", "obs64.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "obs-studio", "bin", "64bit", "obs64.exe")
        };

        string? path = candidates.FirstOrDefault(File.Exists);
        if (path is null)
            throw new FileNotFoundException("OBS Studio wurde nicht gefunden.");

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            WorkingDirectory = Path.GetDirectoryName(path),
            UseShellExecute = true
        });
    }
}

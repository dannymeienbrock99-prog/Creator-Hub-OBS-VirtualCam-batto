using System.Diagnostics;
using CreatorHubLive.Models;

namespace CreatorHubLive.Services;

public sealed class MultistreamService : IDisposable
{
    private readonly List<Process> _processes = [];

    public bool IsRunning => _processes.Any(p => !p.HasExited);

    public async Task StartAsync(string ffmpegPath, string inputUrl, IEnumerable<StreamTarget> targets, Action<string> log, CancellationToken ct = default)
    {
        Stop(log);
        List<StreamTarget> enabled = targets.Where(t => t.Enabled).ToList();
        if (enabled.Count == 0)
            throw new InvalidOperationException("Mindestens eine Plattform muss aktiviert sein.");
        if (!File.Exists(ffmpegPath) && !string.Equals(ffmpegPath, "ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("FFmpeg wurde nicht gefunden.", ffmpegPath);

        foreach (StreamTarget target in enabled)
        {
            if (string.IsNullOrWhiteSpace(target.ServerUrl) || string.IsNullOrWhiteSpace(target.StreamKey))
                throw new InvalidOperationException($"Für {target.Name} fehlen Server-URL oder Stream-Key.");

            string args = $"-hide_banner -loglevel warning -i \"{inputUrl}\" -c copy -f flv \"{target.OutputUrl}\"";
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                    log($"{target.Name}: {e.Data}");
            };
            process.Exited += (_, _) => log($"{target.Name}: FFmpeg beendet (Code {process.ExitCode}).");

            if (!process.Start())
                throw new InvalidOperationException($"FFmpeg für {target.Name} konnte nicht gestartet werden.");

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            _processes.Add(process);
            target.Status = "LIVE";
            log($"{target.Name}: Multistream-Ausgabe gestartet.");
            await Task.Delay(150, ct);
        }
    }

    public void Stop(Action<string>? log = null)
    {
        foreach (Process process in _processes)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(3000);
                }
            }
            catch (Exception ex)
            {
                log?.Invoke("FFmpeg Stop: " + ex.Message);
            }
            finally
            {
                process.Dispose();
            }
        }
        _processes.Clear();
        log?.Invoke("Multistream-Ausgaben beendet.");
    }

    public void Dispose() => Stop();
}

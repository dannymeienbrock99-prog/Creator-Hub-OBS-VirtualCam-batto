namespace CreatorHubLive.Models;

public sealed class AppSettings
{
    public string ObsHost { get; set; } = "127.0.0.1";
    public int ObsPort { get; set; } = 4455;
    public string ObsPassword { get; set; } = "";
    public string FfmpegPath { get; set; } = "ffmpeg.exe";
    public string RelayListenUrl { get; set; } = "rtmp://127.0.0.1:1935/live/creatorhub";
    public bool StartVirtualCamera { get; set; } = true;
    public List<StreamTarget> Targets { get; set; } =
    [
        new() { Name = "Twitch", ServerUrl = "rtmp://live.twitch.tv/app", Enabled = false },
        new() { Name = "YouTube", ServerUrl = "rtmp://a.rtmp.youtube.com/live2", Enabled = false },
        new() { Name = "TikTok", ServerUrl = "rtmp://push-rtmp-l1.tiktok.com/game", Enabled = false }
    ];
}

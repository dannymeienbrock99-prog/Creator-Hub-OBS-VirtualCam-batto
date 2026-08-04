namespace CreatorHubLive.Models;

public sealed class StreamTarget
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public string ServerUrl { get; set; } = "";
    public string StreamKey { get; set; } = "";
    public string Status { get; set; } = "Bereit";

    public string OutputUrl => string.IsNullOrWhiteSpace(StreamKey)
        ? ServerUrl.TrimEnd('/')
        : $"{ServerUrl.TrimEnd('/')}/{StreamKey.TrimStart('/')}";
}

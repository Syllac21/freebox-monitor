namespace FreeboxMonitor.Worker.Models;

public class FreeboxOptions
{
    public const string SectionName = "Freebox";

    public string BaseUrl { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string? AppToken { get; set; }
    public List<string> TrackedDeviceNames { get; set; } = [];
}

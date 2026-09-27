namespace FreeboxMonitor.Web.Models;

public class FreeboxOptions
{
    public const string SectionName = "Freebox";

    public string BaseUrl { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string AppToken { get; set; } = string.Empty;
    public List<string> TrackedDeviceNames { get; set; } = [];
}

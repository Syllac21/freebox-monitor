namespace FreeboxMonitor.Web.Models;

public class DeviceStatus
{
    public string DeviceName { get; set; } = string.Empty;
    public bool IsReachable { get; set; }
    public DateTimeOffset EventTime { get; set; }
}

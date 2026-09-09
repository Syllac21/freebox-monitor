namespace FreeboxMonitor.Worker.Models;

public class FreeboxResponse<T>
{
    public bool Success { get; set; }
    public T? Result { get; set; }
    public string? Msg { get; set; }
    public string? ErrorCode { get; set; }
}

public class AuthorizeResult
{
    public string AppToken { get; set; } = string.Empty;
    public int TrackId { get; set; }
}

public class AuthorizeStatus
{
    public string Status { get; set; } = string.Empty; // "pending", "granted", "denied", "timeout"
    public string Challenge { get; set; } = string.Empty;
}

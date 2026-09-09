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
    public string Status { get; set; } = string.Empty;
    public string Challenge { get; set; } = string.Empty;
}

public class LoginStatus
{
    public bool LoggedIn { get; set; }
    public string Challenge { get; set; } = string.Empty;
}

public class OpenSessionResult
{
    public string SessionToken { get; set; } = string.Empty;
    public string Challenge { get; set; } = string.Empty;
}

public class LanHost
{
    public string Id { get; set; } = string.Empty;
    public string PrimaryName { get; set; } = string.Empty;
    public bool Reachable { get; set; }
    public long LastTimeReachable { get; set; }
    public string L2Ident { get; set; } = string.Empty; // souvent l'adresse MAC
}

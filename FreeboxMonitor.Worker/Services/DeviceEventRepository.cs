using Dapper;
using Npgsql;

namespace FreeboxMonitor.Worker.Services;

public class DeviceEventRepository
{
    private readonly string _connectionString;
    private readonly ILogger<DeviceEventRepository> _logger;

    public DeviceEventRepository(IConfiguration configuration, ILogger<DeviceEventRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("FreeboxMonitorDb")!;
        _logger = logger;
    }

    public async Task InsertEventAsync(string deviceName, bool isReachable)
    {
        const string sql = """
            INSERT INTO device_events (device_name, is_reachable)
            VALUES (@DeviceName, @IsReachable)
            """;

        await using var connection = new NpgsqlConnection(_connectionString);

        await connection.ExecuteAsync(sql, new
        {
            DeviceName = deviceName,
            IsReachable = isReachable
        });

        _logger.LogInformation("Événement enregistré : {Device} — reachable: {Reachable}", deviceName, isReachable);
    }
}

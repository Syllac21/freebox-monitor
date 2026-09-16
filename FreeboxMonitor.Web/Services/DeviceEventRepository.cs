using Dapper;
using Npgsql;
using FreeboxMonitor.Web.Models;

namespace FreeboxMonitor.Web.Services;

public class DeviceEventRepository
{
    private readonly string _connectionString;

    public DeviceEventRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FreeboxMonitorDb")!;
    }

    public async Task<List<DeviceStatus>> GetCurrentStatusAsync()
    {
        const string sql = """
            SELECT DISTINCT ON (device_name)
                device_name AS DeviceName,
                is_reachable AS IsReachable,
                event_time AS EventTime
            FROM device_events
            ORDER BY device_name, event_time DESC
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<DeviceStatus>(sql);
        return results.ToList();
    }

    public async Task<List<DeviceStatus>> GetTodayEventsAsync()
    {
        const string sql = """
            SELECT
                device_name AS DeviceName,
                is_reachable AS IsReachable,
                event_time AS EventTime
            FROM device_events
            WHERE event_time >= CURRENT_DATE
            ORDER BY event_time DESC
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<DeviceStatus>(sql);
        return results.ToList();
    }
}

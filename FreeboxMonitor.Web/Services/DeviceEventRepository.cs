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

    public async Task InsertEventIfChangedAsync(string deviceName, bool isReachable)
    {
        await using var connection = new NpgsqlConnection(_connectionString);

        const string lastStateSql = """
            SELECT is_reachable
            FROM device_events
            WHERE device_name = @DeviceName
            ORDER BY event_time DESC
            LIMIT 1
            """;

        var lastState = await connection.QuerySingleOrDefaultAsync<bool?>(lastStateSql, new { DeviceName = deviceName });

        if (lastState is null || lastState != isReachable)
        {
            const string insertSql = """
                INSERT INTO device_events (device_name, is_reachable)
                VALUES (@DeviceName, @IsReachable)
                """;

            await connection.ExecuteAsync(insertSql, new { DeviceName = deviceName, IsReachable = isReachable });
        }
    }
}

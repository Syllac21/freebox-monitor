using Dapper;
using Npgsql;

namespace FreeboxMonitor.Web.Services;

public class TrackedDeviceRepository
{
    private readonly string _connectionString;

    public TrackedDeviceRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("FreeboxMonitorDb")!;
    }

    public async Task<List<string>> GetTrackedDeviceNamesAsync()
    {
        const string sql = "SELECT device_name FROM tracked_devices ORDER BY device_name";

        await using var connection = new NpgsqlConnection(_connectionString);
        var results = await connection.QueryAsync<string>(sql);
        return results.ToList();
    }

    public async Task AddTrackedDeviceAsync(string deviceName)
    {
        const string sql = """
            INSERT INTO tracked_devices (device_name)
            VALUES (@DeviceName)
            ON CONFLICT (device_name) DO NOTHING
            """;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { DeviceName = deviceName });
    }

    public async Task RemoveTrackedDeviceAsync(string deviceName)
    {
        const string sql = "DELETE FROM tracked_devices WHERE device_name = @DeviceName";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, new { DeviceName = deviceName });
    }
}

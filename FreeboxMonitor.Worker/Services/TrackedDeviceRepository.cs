using Dapper;
using Npgsql;

namespace FreeboxMonitor.Worker.Services;

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
}

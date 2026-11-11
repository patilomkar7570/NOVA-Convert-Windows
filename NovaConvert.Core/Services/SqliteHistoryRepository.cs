using Microsoft.Data.Sqlite;
using NovaConvert.Core.Interfaces;

namespace NovaConvert.Core.Services;

public class SqliteHistoryRepository : IHistoryRepository
{
    private readonly string _connectionString;

    public SqliteHistoryRepository(string? databasePath = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(appData, "NOVA Convert");
            Directory.CreateDirectory(dir);
            databasePath = Path.Combine(dir, "conversion_history.db");
        }
        _connectionString = $"Data Source={databasePath}";
    }

    public async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS ConversionHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TimestampUtc TEXT NOT NULL,
                SourceFilePath TEXT NOT NULL,
                OutputFilePath TEXT NOT NULL,
                SourceFormat TEXT NOT NULL,
                TargetFormat TEXT NOT NULL,
                SourceSizeBytes INTEGER NOT NULL,
                OutputSizeBytes INTEGER NOT NULL,
                DurationSeconds REAL NOT NULL,
                EngineUsed TEXT NOT NULL,
                Success INTEGER NOT NULL,
                ErrorMessage TEXT
            );
            CREATE INDEX IF NOT EXISTS IX_ConversionHistory_Timestamp ON ConversionHistory(TimestampUtc DESC);
        ";
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddEntryAsync(HistoryEntry entry)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO ConversionHistory (
                TimestampUtc, SourceFilePath, OutputFilePath, SourceFormat, TargetFormat,
                SourceSizeBytes, OutputSizeBytes, DurationSeconds, EngineUsed, Success, ErrorMessage
            ) VALUES (
                $time, $source, $output, $srcFmt, $tgtFmt, $srcSize, $tgtSize, $duration, $engine, $success, $error
            );
        ";
        cmd.Parameters.AddWithValue("$time", entry.TimestampUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$source", entry.SourceFilePath);
        cmd.Parameters.AddWithValue("$output", entry.OutputFilePath);
        cmd.Parameters.AddWithValue("$srcFmt", entry.SourceFormat);
        cmd.Parameters.AddWithValue("$tgtFmt", entry.TargetFormat);
        cmd.Parameters.AddWithValue("$srcSize", entry.SourceSizeBytes);
        cmd.Parameters.AddWithValue("$tgtSize", entry.OutputSizeBytes);
        cmd.Parameters.AddWithValue("$duration", entry.DurationSeconds);
        cmd.Parameters.AddWithValue("$engine", entry.EngineUsed);
        cmd.Parameters.AddWithValue("$success", entry.Success ? 1 : 0);
        cmd.Parameters.AddWithValue("$error", (object?)entry.ErrorMessage ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<HistoryEntry>> GetRecentEntriesAsync(int limit = 100)
    {
        var list = new List<HistoryEntry>();
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, TimestampUtc, SourceFilePath, OutputFilePath, SourceFormat, TargetFormat, SourceSizeBytes, OutputSizeBytes, DurationSeconds, EngineUsed, Success, ErrorMessage FROM ConversionHistory ORDER BY Id DESC LIMIT $limit;";
        cmd.Parameters.AddWithValue("$limit", limit);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new HistoryEntry
            {
                Id = reader.GetInt64(0),
                TimestampUtc = DateTime.TryParse(reader.GetString(1), out var dt) ? dt : DateTime.UtcNow,
                SourceFilePath = reader.GetString(2),
                OutputFilePath = reader.GetString(3),
                SourceFormat = reader.GetString(4),
                TargetFormat = reader.GetString(5),
                SourceSizeBytes = reader.GetInt64(6),
                OutputSizeBytes = reader.GetInt64(7),
                DurationSeconds = reader.GetDouble(8),
                EngineUsed = reader.GetString(9),
                Success = reader.GetInt32(10) == 1,
                ErrorMessage = reader.IsDBNull(11) ? null : reader.GetString(11)
            });
        }
        return list;
    }

    public async Task ClearHistoryAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM ConversionHistory;";
        await cmd.ExecuteNonQueryAsync();
    }
}

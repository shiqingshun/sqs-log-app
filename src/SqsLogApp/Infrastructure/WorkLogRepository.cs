using System.Globalization;
using Microsoft.Data.Sqlite;
using SqsLogApp.Models;

namespace SqsLogApp.Infrastructure;

public sealed class WorkLogRepository : IDisposable
{
    private readonly string _connectionString;

    public WorkLogRepository(string databasePath)
    {
        DatabasePath = Path.GetFullPath(databasePath);
        var directoryPath = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        _connectionString = $"Data Source={DatabasePath}";
        EnsureSchema();
    }

    public string DatabasePath { get; }

    public IReadOnlyList<WorkLogEntry> GetByDate(DateTime date)
        => QueryEntries(
            @"SELECT id, log_date, summary, detail, created_at, updated_at
              FROM work_log_entries
              WHERE log_date = $logDate
              ORDER BY updated_at DESC;",
            command => command.Parameters.AddWithValue("$logDate", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));

    public IReadOnlyList<WorkLogEntry> GetByMonth(DateTime month)
    {
        var monthStart = new DateTime(month.Year, month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        return QueryEntries(
            @"SELECT id, log_date, summary, detail, created_at, updated_at
              FROM work_log_entries
              WHERE log_date >= $startDate AND log_date <= $endDate
              ORDER BY log_date ASC, updated_at DESC;",
            command =>
            {
                command.Parameters.AddWithValue("$startDate", monthStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$endDate", monthEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            });
    }

    public IReadOnlyList<WorkLogEntry> Search(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        return QueryEntries(
            @"SELECT id, log_date, summary, detail, created_at, updated_at
              FROM work_log_entries
              WHERE summary LIKE $keyword OR detail LIKE $keyword
              ORDER BY log_date DESC, updated_at DESC;",
            command => command.Parameters.AddWithValue("$keyword", $"%{keyword.Trim()}%"));
    }

    public IReadOnlyList<WorkLogEntry> GetByRange(DateTime startDate, DateTime endDate)
    {
        var normalizedStart = startDate.Date;
        var normalizedEnd = endDate.Date;
        if (normalizedEnd < normalizedStart)
        {
            throw new ArgumentException("结束日期不能早于开始日期。");
        }

        return QueryEntries(
            @"SELECT id, log_date, summary, detail, created_at, updated_at
              FROM work_log_entries
              WHERE log_date >= $startDate AND log_date <= $endDate
              ORDER BY log_date ASC, updated_at ASC;",
            command =>
            {
                command.Parameters.AddWithValue("$startDate", normalizedStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                command.Parameters.AddWithValue("$endDate", normalizedEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            });
    }

    public IReadOnlyList<DateTime> GetLoggedDatesInMonth(DateTime month)
    {
        var monthStart = new DateTime(month.Year, month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"SELECT DISTINCT log_date
                                FROM work_log_entries
                                WHERE log_date >= $startDate AND log_date <= $endDate;";
        command.Parameters.AddWithValue("$startDate", monthStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$endDate", monthEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        using var reader = command.ExecuteReader();
        var dates = new List<DateTime>();
        while (reader.Read())
        {
            var value = reader.GetString(0);
            dates.Add(DateTime.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        return dates;
    }

    public long Add(DateTime logDate, string summary, string detail)
    {
        var now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO work_log_entries (log_date, summary, detail, created_at, updated_at)
                                VALUES ($logDate, $summary, $detail, $createdAt, $updatedAt);
                                SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("$logDate", logDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$summary", summary.Trim());
        command.Parameters.AddWithValue("$detail", detail);
        command.Parameters.AddWithValue("$createdAt", now);
        command.Parameters.AddWithValue("$updatedAt", now);

        var scalarValue = command.ExecuteScalar();
        return Convert.ToInt64(scalarValue, CultureInfo.InvariantCulture);
    }

    public void Update(long id, DateTime logDate, string summary, string detail)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"UPDATE work_log_entries
                                SET log_date = $logDate,
                                    summary = $summary,
                                    detail = $detail,
                                    updated_at = $updatedAt
                                WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$logDate", logDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$summary", summary.Trim());
        command.Parameters.AddWithValue("$detail", detail);
        command.Parameters.AddWithValue("$updatedAt", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));

        command.ExecuteNonQuery();
    }

    public void Delete(long id)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM work_log_entries WHERE id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
    }

    private void EnsureSchema()
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            @"CREATE TABLE IF NOT EXISTS work_log_entries (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                log_date TEXT NOT NULL,
                summary TEXT NOT NULL,
                detail TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
              );
              CREATE INDEX IF NOT EXISTS idx_work_log_entries_log_date
                  ON work_log_entries(log_date);
              CREATE INDEX IF NOT EXISTS idx_work_log_entries_updated_at
                  ON work_log_entries(updated_at);";
        command.ExecuteNonQuery();
    }

    private SqliteConnection CreateConnection() => new(_connectionString);

    private List<WorkLogEntry> QueryEntries(string sql, Action<SqliteCommand> bindParameters)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = sql;
        bindParameters(command);

        using var reader = command.ExecuteReader();
        var results = new List<WorkLogEntry>();
        while (reader.Read())
        {
            results.Add(new WorkLogEntry
            {
                Id = reader.GetInt64(0),
                LogDate = DateTime.ParseExact(reader.GetString(1), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                Summary = reader.GetString(2),
                Detail = reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                UpdatedAt = DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            });
        }

        return results;
    }
}

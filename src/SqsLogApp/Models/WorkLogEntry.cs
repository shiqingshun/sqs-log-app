namespace SqsLogApp.Models;

public sealed class WorkLogEntry
{
    public long Id { get; set; }

    public DateTime LogDate { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string Detail { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public override string ToString() => Summary;
}

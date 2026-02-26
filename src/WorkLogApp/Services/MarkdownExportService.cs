using System.Text;
using WorkLogApp.Models;

namespace WorkLogApp.Services;

public enum LogExportFormat
{
    Markdown,
    Txt
}

public sealed class MarkdownExportService
{
    public void Export(
        string outputPath,
        DateTime startDate,
        DateTime endDate,
        IReadOnlyList<WorkLogEntry> entries,
        LogExportFormat format,
        bool includeDetail)
    {
        var sortedEntries = entries
            .OrderBy(item => item.LogDate)
            .ThenBy(item => item.UpdatedAt)
            .ToList();

        var directoryPath = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var outputContent = format == LogExportFormat.Markdown
            ? BuildMarkdown(startDate, endDate, sortedEntries, includeDetail)
            : BuildTxt(sortedEntries, includeDetail);

        File.WriteAllText(outputPath, outputContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string BuildMarkdown(
        DateTime startDate,
        DateTime endDate,
        IReadOnlyList<WorkLogEntry> entries,
        bool includeDetail)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# 工作任务清单");
        builder.AppendLine();
        builder.AppendLine($"- 时间范围：{startDate:yyyy-MM-dd} ~ {endDate:yyyy-MM-dd}");
        builder.AppendLine($"- 导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine();

        if (entries.Count == 0)
        {
            builder.AppendLine("（该时间范围内无日志记录）");
            return builder.ToString();
        }

        foreach (var group in entries.GroupBy(item => item.LogDate.Date))
        {
            builder.AppendLine($"## {group.Key:yyyy-MM-dd}");
            foreach (var entry in group)
            {
                builder.AppendLine($"- **{EscapeInline(entry.Summary)}**");
                if (!includeDetail || string.IsNullOrWhiteSpace(entry.Detail))
                {
                    continue;
                }

                foreach (var detailLine in entry.Detail.Replace("\r", string.Empty).Split('\n'))
                {
                    builder.AppendLine($"  - {EscapeInline(detailLine)}");
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildTxt(IReadOnlyList<WorkLogEntry> entries, bool includeDetail)
    {
        var builder = new StringBuilder();
        var groupedEntries = entries
            .GroupBy(entry => entry.LogDate.Date)
            .OrderBy(group => group.Key);

        foreach (var group in groupedEntries)
        {
            builder.AppendLine(group.Key.ToString("yyyy-MM-dd"));
            foreach (var entry in group)
            {
                builder.Append('\t');
                builder.AppendLine(NormalizeTabText(entry.Summary));

                if (!includeDetail || string.IsNullOrWhiteSpace(entry.Detail))
                {
                    continue;
                }

                foreach (var detailLine in NormalizeTabText(entry.Detail).Split('\n', StringSplitOptions.None))
                {
                    if (string.IsNullOrWhiteSpace(detailLine))
                    {
                        continue;
                    }

                    builder.Append("\t\t");
                    builder.AppendLine(detailLine);
                }
            }
        }

        return builder.ToString();
    }

    private static string NormalizeTabText(string text)
        => text.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal)
            .Trim();

    private static string EscapeInline(string input)
        => input
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal);
}

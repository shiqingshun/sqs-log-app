using System.Text;
using SqsLogApp.Models;

namespace SqsLogApp.Services;

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
        builder.AppendLine("日期\t日志描述\t\t日志详情");
        foreach (var entry in entries)
        {
            builder.Append(entry.LogDate.ToString("yyyy-MM-dd"));
            builder.Append('\t');
            builder.Append(NormalizeTabText(entry.Summary));
            builder.Append('\t');
            builder.Append('\t');
            if (includeDetail)
            {
                builder.Append(NormalizeTabText(entry.Detail).Replace("\n", "\\n", StringComparison.Ordinal));
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string NormalizeTabText(string text)
        => text.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal)
            .TrimEnd();

    private static string EscapeInline(string input)
        => input
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal);
}

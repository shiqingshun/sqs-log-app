using System.Text;
using SqsLogApp.Models;

namespace SqsLogApp.Services;

public sealed class MarkdownExportService
{
    public void Export(string outputPath, DateTime startDate, DateTime endDate, IReadOnlyList<WorkLogEntry> entries)
    {
        var sortedEntries = entries
            .OrderBy(item => item.LogDate)
            .ThenBy(item => item.UpdatedAt)
            .ToList();

        var builder = new StringBuilder();
        builder.AppendLine("# 工作任务清单");
        builder.AppendLine();
        builder.AppendLine($"- 时间范围：{startDate:yyyy-MM-dd} ~ {endDate:yyyy-MM-dd}");
        builder.AppendLine($"- 导出时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine();

        if (sortedEntries.Count == 0)
        {
            builder.AppendLine("（该时间范围内无日志记录）");
        }
        else
        {
            foreach (var group in sortedEntries.GroupBy(item => item.LogDate.Date))
            {
                builder.AppendLine($"## {group.Key:yyyy-MM-dd}");
                foreach (var entry in group)
                {
                    builder.AppendLine($"- **{EscapeInline(entry.Summary)}**");
                    if (!string.IsNullOrWhiteSpace(entry.Detail))
                    {
                        foreach (var detailLine in entry.Detail.Replace("\r", string.Empty).Split('\n'))
                        {
                            builder.AppendLine($"  - {EscapeInline(detailLine)}");
                        }
                    }
                }

                builder.AppendLine();
            }
        }

        var directoryPath = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        File.WriteAllText(outputPath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string EscapeInline(string input)
        => input
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("*", "\\*", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("`", "\\`", StringComparison.Ordinal);
}

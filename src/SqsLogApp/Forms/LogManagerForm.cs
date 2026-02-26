using SqsLogApp.Infrastructure;
using SqsLogApp.Models;
using SqsLogApp.Services;

namespace SqsLogApp.Forms;

public sealed class LogManagerForm : Form
{
    private readonly WorkLogRepository _repository;
    private readonly MarkdownExportService _exportService;

    private readonly DateTimePicker _monthPicker;
    private readonly Label _monthTitleLabel;
    private readonly TableLayoutPanel _calendarGrid;
    private readonly CalendarCellView[] _calendarCells;
    private readonly TextBox _searchKeywordTextBox;
    private readonly DataGridView _searchResultsGrid;
    private readonly DateTimePicker _startDatePicker;
    private readonly DateTimePicker _endDatePicker;
    private readonly ComboBox _exportFormatComboBox;
    private readonly CheckBox _includeDetailCheckBox;
    private readonly TextBox _exportPathTextBox;

    private DateTime _currentCalendarMonth;

    public LogManagerForm(WorkLogRepository repository, MarkdownExportService exportService)
    {
        _repository = repository;
        _exportService = exportService;

        Text = "工作日志管理";
        Icon = global::SqsLogApp.AppBranding.AppIcon;
        this.EnableEscClose();
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 640);
        Size = new Size(2200, 1440);

        var tabControl = new TabControl
        {
            Dock = DockStyle.Fill
        };

        var calendarTab = new TabPage("日历视图");
        var searchTab = new TabPage("全局搜索");
        var exportTab = new TabPage("导出");

        tabControl.TabPages.Add(calendarTab);
        tabControl.TabPages.Add(searchTab);
        tabControl.TabPages.Add(exportTab);
        Controls.Add(tabControl);

        var calendarRoot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            ColumnCount = 1,
            RowCount = 3
        };
        calendarRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        calendarRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        calendarRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        calendarTab.Controls.Add(calendarRoot);

        var headerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

        var previousMonthButton = new Button
        {
            Text = "◀",
            Dock = DockStyle.Fill
        };
        previousMonthButton.Click += (_, _) => ChangeMonth(-1);

        _monthPicker = new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM",
            ShowUpDown = true,
            Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
        };
        _monthPicker.ValueChanged += (_, _) =>
        {
            var pickedMonth = new DateTime(_monthPicker.Value.Year, _monthPicker.Value.Month, 1);
            if (pickedMonth == _currentCalendarMonth)
            {
                return;
            }

            _currentCalendarMonth = pickedMonth;
            RenderCalendarMonth();
        };

        _monthTitleLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, 12F, FontStyle.Bold)
        };

        var nextMonthButton = new Button
        {
            Text = "▶",
            Dock = DockStyle.Fill
        };
        nextMonthButton.Click += (_, _) => ChangeMonth(1);

        headerPanel.Controls.Add(previousMonthButton, 0, 0);
        headerPanel.Controls.Add(_monthPicker, 1, 0);
        headerPanel.Controls.Add(_monthTitleLabel, 2, 0);
        headerPanel.Controls.Add(nextMonthButton, 3, 0);
        calendarRoot.Controls.Add(headerPanel, 0, 0);

        var weekdayHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7
        };

        for (var columnIndex = 0; columnIndex < 7; columnIndex++)
        {
            weekdayHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 7F));
        }

        var weekNames = new[] { "一", "二", "三", "四", "五", "六", "日" };
        for (var index = 0; index < weekNames.Length; index++)
        {
            weekdayHeader.Controls.Add(new Label
            {
                Text = $"周{weekNames[index]}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(61, 86, 112)
            }, index, 0);
        }

        calendarRoot.Controls.Add(weekdayHeader, 0, 1);

        _calendarGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 6,
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        };
        for (var columnIndex = 0; columnIndex < 7; columnIndex++)
        {
            _calendarGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 7F));
        }

        for (var rowIndex = 0; rowIndex < 6; rowIndex++)
        {
            _calendarGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 6F));
        }

        _calendarCells = new CalendarCellView[42];
        for (var cellIndex = 0; cellIndex < _calendarCells.Length; cellIndex++)
        {
            var cell = CreateCalendarCell();
            _calendarCells[cellIndex] = cell;
            _calendarGrid.Controls.Add(cell.Container, cellIndex % 7, cellIndex / 7);
        }

        calendarRoot.Controls.Add(_calendarGrid, 0, 2);

        var searchLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10)
        };
        searchLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        searchLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var searchTopPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        searchTopPanel.Controls.Add(new Label
        {
            Text = "关键字：",
            Margin = new Padding(0, 10, 0, 0),
            AutoSize = true
        });

        _searchKeywordTextBox = new TextBox
        {
            Width = 380
        };
        searchTopPanel.Controls.Add(_searchKeywordTextBox);

        var searchButton = new Button
        {
            Text = "搜索",
            AutoSize = true
        };
        searchButton.Click += (_, _) => PerformSearch();
        searchTopPanel.Controls.Add(searchButton);
        searchLayout.Controls.Add(searchTopPanel, 0, 0);

        _searchResultsGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false
        };
        _searchResultsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "日期",
            DataPropertyName = nameof(SearchResultRow.LogDate),
            Width = 120
        });
        _searchResultsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "描述",
            DataPropertyName = nameof(SearchResultRow.Summary),
            Width = 260
        });
        _searchResultsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "详情",
            DataPropertyName = nameof(SearchResultRow.Detail),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        searchLayout.Controls.Add(_searchResultsGrid, 0, 1);
        searchTab.Controls.Add(searchLayout);

        var exportLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 3,
            RowCount = 6
        };
        exportLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        exportLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        exportLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        exportLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        exportLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        exportLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        exportLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        exportLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        exportLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        exportLayout.Controls.Add(new Label
        {
            Text = "开始日期：",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        }, 0, 0);

        _startDatePicker = new DateTimePicker
        {
            Dock = DockStyle.Left,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd",
            Width = 150,
            Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)
        };
        exportLayout.Controls.Add(_startDatePicker, 1, 0);

        exportLayout.Controls.Add(new Label
        {
            Text = "结束日期：",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        }, 0, 1);

        _endDatePicker = new DateTimePicker
        {
            Dock = DockStyle.Left,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd",
            Width = 150,
            Value = DateTime.Today
        };
        exportLayout.Controls.Add(_endDatePicker, 1, 1);

        exportLayout.Controls.Add(new Label
        {
            Text = "导出格式：",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        }, 0, 2);

        _exportFormatComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 180
        };
        _exportFormatComboBox.Items.Add("Markdown (.md)");
        _exportFormatComboBox.Items.Add("TXT (.txt)");
        _exportFormatComboBox.SelectedIndex = 0;
        exportLayout.Controls.Add(_exportFormatComboBox, 1, 2);

        exportLayout.Controls.Add(new Label
        {
            Text = "导出详情：",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        }, 0, 3);

        _includeDetailCheckBox = new CheckBox
        {
            Text = "包含详情（Markdown 与 TXT 都生效）",
            Checked = true,
            Dock = DockStyle.Left
        };
        exportLayout.Controls.Add(_includeDetailCheckBox, 1, 3);

        exportLayout.Controls.Add(new Label
        {
            Text = "导出文件：",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        }, 0, 4);

        _exportPathTextBox = new TextBox
        {
            Dock = DockStyle.Fill
        };
        exportLayout.Controls.Add(_exportPathTextBox, 1, 4);

        var browseButton = new Button
        {
            Text = "浏览",
            Dock = DockStyle.Fill
        };
        browseButton.Click += (_, _) => PickExportPath();
        exportLayout.Controls.Add(browseButton, 2, 4);

        var exportButton = new Button
        {
            Text = "导出",
            AutoSize = true
        };
        exportButton.Click += (_, _) => ExportLogs();

        var exportButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight
        };
        exportButtonPanel.Controls.Add(exportButton);
        exportLayout.Controls.Add(exportButtonPanel, 1, 5);

        exportTab.Controls.Add(exportLayout);

        _startDatePicker.ValueChanged += (_, _) => UpdateDefaultExportPath();
        _endDatePicker.ValueChanged += (_, _) => UpdateDefaultExportPath();
        _exportFormatComboBox.SelectedIndexChanged += (_, _) => UpdateDefaultExportPath();

        _currentCalendarMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        UpdateDefaultExportPath();
        RefreshData();
    }

    public void RefreshData()
    {
        RenderCalendarMonth();
    }

    private void ChangeMonth(int offset)
    {
        _currentCalendarMonth = _currentCalendarMonth.AddMonths(offset);
        RenderCalendarMonth();
    }

    private void RenderCalendarMonth()
    {
        _monthTitleLabel.Text = $"{_currentCalendarMonth:yyyy年MM月}";
        if (_monthPicker.Value.Year != _currentCalendarMonth.Year || _monthPicker.Value.Month != _currentCalendarMonth.Month)
        {
            _monthPicker.Value = _currentCalendarMonth;
        }

        var summariesByDate = _repository.GetByMonth(_currentCalendarMonth)
            .GroupBy(entry => entry.LogDate.Date)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => SanitizeSummary(item.Summary)).ToList());

        var firstDay = new DateTime(_currentCalendarMonth.Year, _currentCalendarMonth.Month, 1);
        var startOffset = ((int)firstDay.DayOfWeek + 6) % 7;
        var firstCellDate = firstDay.AddDays(-startOffset);

        _calendarGrid.SuspendLayout();

        for (var cellIndex = 0; cellIndex < _calendarCells.Length; cellIndex++)
        {
            var cellDate = firstCellDate.AddDays(cellIndex).Date;
            summariesByDate.TryGetValue(cellDate, out var summaries);
            UpdateCalendarCell(_calendarCells[cellIndex], cellDate, cellDate.Month == _currentCalendarMonth.Month, summaries ?? []);
        }

        _calendarGrid.ResumeLayout();
    }

    private static CalendarCellView CreateCalendarCell()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(4),
            BackColor = Color.White
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var dayLabel = new Label
        {
            Text = string.Empty,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight
        };

        var summaryLabel = new Label
        {
            Text = string.Empty,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = Color.FromArgb(61, 86, 112),
            Font = new Font(SystemFonts.DefaultFont.FontFamily, 8.5F, FontStyle.Regular),
            AutoEllipsis = true
        };

        layout.Controls.Add(dayLabel, 0, 0);
        layout.Controls.Add(summaryLabel, 0, 1);
        panel.Controls.Add(layout);

        return new CalendarCellView(panel, dayLabel, summaryLabel);
    }

    private static void UpdateCalendarCell(
        CalendarCellView cell,
        DateTime date,
        bool isCurrentMonth,
        IReadOnlyList<string> summaries)
    {
        cell.Container.BackColor = isCurrentMonth ? Color.White : Color.FromArgb(245, 247, 249);
        cell.DayLabel.Text = date.ToString("dd");
        cell.DayLabel.Font = SystemFonts.DefaultFont;
        cell.DayLabel.ForeColor = isCurrentMonth ? Color.FromArgb(38, 59, 87) : Color.FromArgb(145, 152, 162);

        if (date.Date == DateTime.Today)
        {
            cell.DayLabel.ForeColor = Color.FromArgb(18, 122, 128);
        }

        cell.SummaryLabel.Text = BuildSummaryText(summaries);
    }

    private static string BuildSummaryText(IReadOnlyList<string> summaries)
    {
        if (summaries.Count == 0)
        {
            return string.Empty;
        }

        const int maxDisplayLines = 4;
        var lines = summaries
            .Where(summary => !string.IsNullOrWhiteSpace(summary))
            .Take(maxDisplayLines)
            .Select(summary => $"• {summary}")
            .ToList();

        if (summaries.Count > maxDisplayLines)
        {
            lines.Add("…");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string SanitizeSummary(string summary)
        => summary.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();

    private void PerformSearch()
    {
        var keyword = _searchKeywordTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            MessageBox.Show("请输入搜索关键字。", "全局搜索", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var results = _repository.Search(keyword)
            .Select(entry => new SearchResultRow
            {
                LogDate = entry.LogDate.ToString("yyyy-MM-dd"),
                Summary = entry.Summary,
                Detail = entry.Detail
            })
            .ToList();

        _searchResultsGrid.DataSource = results;
    }

    private void PickExportPath()
    {
        var selectedFormat = GetSelectedExportFormat();
        using var dialog = new SaveFileDialog
        {
            Filter = selectedFormat == LogExportFormat.Markdown
                ? "Markdown 文件 (*.md)|*.md|所有文件 (*.*)|*.*"
                : "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            FileName = Path.GetFileName(_exportPathTextBox.Text),
            InitialDirectory = Path.GetDirectoryName(_exportPathTextBox.Text)
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _exportPathTextBox.Text = dialog.FileName;
        }
    }

    private void ExportLogs()
    {
        if (_endDatePicker.Value.Date < _startDatePicker.Value.Date)
        {
            MessageBox.Show("结束日期不能早于开始日期。", "导出", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var outputPath = _exportPathTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            MessageBox.Show("请选择导出路径。", "导出", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedFormat = GetSelectedExportFormat();
        if (string.IsNullOrWhiteSpace(Path.GetExtension(outputPath)))
        {
            outputPath = $"{outputPath}{GetExtension(selectedFormat)}";
            _exportPathTextBox.Text = outputPath;
        }

        var entries = _repository.GetByRange(_startDatePicker.Value.Date, _endDatePicker.Value.Date);
        _exportService.Export(
            outputPath,
            _startDatePicker.Value.Date,
            _endDatePicker.Value.Date,
            entries,
            selectedFormat,
            _includeDetailCheckBox.Checked);
        MessageBox.Show("导出完成。", "导出", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private LogExportFormat GetSelectedExportFormat()
        => _exportFormatComboBox.SelectedIndex == 1
            ? LogExportFormat.Txt
            : LogExportFormat.Markdown;

    private void UpdateDefaultExportPath()
    {
        _exportPathTextBox.Text = CreateDefaultExportPath(GetSelectedExportFormat());
    }

    private string CreateDefaultExportPath(LogExportFormat format)
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        return Path.Combine(
            desktopPath,
            $"worklogs-{_startDatePicker.Value:yyyyMMdd}-{_endDatePicker.Value:yyyyMMdd}{GetExtension(format)}");
    }

    private static string GetExtension(LogExportFormat format)
        => format == LogExportFormat.Markdown ? ".md" : ".txt";

    private sealed class CalendarCellView(
        Panel container,
        Label dayLabel,
        Label summaryLabel)
    {
        public Panel Container { get; } = container;

        public Label DayLabel { get; } = dayLabel;

        public Label SummaryLabel { get; } = summaryLabel;
    }

    private sealed class SearchResultRow
    {
        public string LogDate { get; init; } = string.Empty;

        public string Summary { get; init; } = string.Empty;

        public string Detail { get; init; } = string.Empty;
    }
}

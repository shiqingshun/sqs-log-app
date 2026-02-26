using SqsLogApp.Infrastructure;
using SqsLogApp.Models;
using SqsLogApp.Services;

namespace SqsLogApp.Forms;

public sealed class LogManagerForm : Form
{
    private readonly WorkLogRepository _repository;
    private readonly MarkdownExportService _exportService;

    private readonly MonthCalendar _monthCalendar;
    private readonly ListView _dayEntriesListView;
    private readonly TextBox _dayDetailTextBox;
    private readonly TextBox _searchKeywordTextBox;
    private readonly DataGridView _searchResultsGrid;
    private readonly DateTimePicker _startDatePicker;
    private readonly DateTimePicker _endDatePicker;
    private readonly TextBox _exportPathTextBox;

    private List<WorkLogEntry> _selectedDayEntries = [];
    private DateTime _currentCalendarMonth;

    public LogManagerForm(WorkLogRepository repository, MarkdownExportService exportService)
    {
        _repository = repository;
        _exportService = exportService;

        Text = "工作日志管理";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 640);
        Size = new Size(1100, 720);

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

        var calendarLayout = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 290
        };
        calendarTab.Controls.Add(calendarLayout);

        _monthCalendar = new MonthCalendar
        {
            Dock = DockStyle.Top,
            MaxSelectionCount = 1
        };
        _monthCalendar.DateChanged += MonthCalendarOnDateChanged;
        calendarLayout.Panel1.Controls.Add(_monthCalendar);

        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        rightPanel.Controls.Add(new Label
        {
            Text = "当日记录：",
            AutoSize = true,
            Dock = DockStyle.Fill
        }, 0, 0);

        _dayEntriesListView = new ListView
        {
            Dock = DockStyle.Fill,
            FullRowSelect = true,
            View = View.Details,
            MultiSelect = false
        };
        _dayEntriesListView.Columns.Add("描述", 360);
        _dayEntriesListView.Columns.Add("更新时间", 180);
        _dayEntriesListView.SelectedIndexChanged += (_, _) => LoadSelectedDayEntryDetail();
        rightPanel.Controls.Add(_dayEntriesListView, 0, 1);

        _dayDetailTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true
        };
        rightPanel.Controls.Add(_dayDetailTextBox, 0, 2);
        calendarLayout.Panel2.Controls.Add(rightPanel);

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
            RowCount = 4
        };
        exportLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        exportLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        exportLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
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
            Value = DateTime.Today.AddDays(-7)
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
            Text = "导出文件：",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        }, 0, 2);

        _exportPathTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = CreateDefaultExportPath()
        };
        exportLayout.Controls.Add(_exportPathTextBox, 1, 2);

        var browseButton = new Button
        {
            Text = "浏览",
            Dock = DockStyle.Fill
        };
        browseButton.Click += (_, _) => PickExportPath();
        exportLayout.Controls.Add(browseButton, 2, 2);

        var exportButton = new Button
        {
            Text = "导出 Markdown",
            AutoSize = true
        };
        exportButton.Click += (_, _) => ExportLogs();

        var exportButtonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight
        };
        exportButtonPanel.Controls.Add(exportButton);
        exportLayout.Controls.Add(exportButtonPanel, 1, 3);

        exportTab.Controls.Add(exportLayout);

        _currentCalendarMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        RefreshData();
    }

    public void RefreshData()
    {
        RefreshCalendarBoldedDates();
        LoadDayEntries();
    }

    private void MonthCalendarOnDateChanged(object? sender, DateRangeEventArgs e)
    {
        var selectedMonth = new DateTime(_monthCalendar.SelectionStart.Year, _monthCalendar.SelectionStart.Month, 1);
        if (_currentCalendarMonth != selectedMonth)
        {
            _currentCalendarMonth = selectedMonth;
            RefreshCalendarBoldedDates();
        }

        LoadDayEntries();
    }

    private void RefreshCalendarBoldedDates()
    {
        var boldedDates = _repository.GetLoggedDatesInMonth(_currentCalendarMonth).ToArray();
        _monthCalendar.BoldedDates = boldedDates;
        _monthCalendar.UpdateBoldedDates();
    }

    private void LoadDayEntries()
    {
        _selectedDayEntries = _repository.GetByDate(_monthCalendar.SelectionStart.Date).ToList();
        _dayEntriesListView.Items.Clear();

        foreach (var entry in _selectedDayEntries)
        {
            var item = new ListViewItem(entry.Summary);
            item.SubItems.Add(entry.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            _dayEntriesListView.Items.Add(item);
        }

        if (_dayEntriesListView.Items.Count > 0)
        {
            _dayEntriesListView.Items[0].Selected = true;
        }
        else
        {
            _dayDetailTextBox.Clear();
        }
    }

    private void LoadSelectedDayEntryDetail()
    {
        if (_dayEntriesListView.SelectedIndices.Count == 0)
        {
            _dayDetailTextBox.Clear();
            return;
        }

        var selectedIndex = _dayEntriesListView.SelectedIndices[0];
        if (selectedIndex < 0 || selectedIndex >= _selectedDayEntries.Count)
        {
            _dayDetailTextBox.Clear();
            return;
        }

        _dayDetailTextBox.Text = _selectedDayEntries[selectedIndex].Detail;
    }

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
        using var dialog = new SaveFileDialog
        {
            Filter = "Markdown 文件 (*.md)|*.md|所有文件 (*.*)|*.*",
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

        var entries = _repository.GetByRange(_startDatePicker.Value.Date, _endDatePicker.Value.Date);
        _exportService.Export(outputPath, _startDatePicker.Value.Date, _endDatePicker.Value.Date, entries);
        MessageBox.Show("导出完成。", "导出", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private string CreateDefaultExportPath()
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        return Path.Combine(
            desktopPath,
            $"worklogs-{_startDatePicker.Value:yyyyMMdd}-{_endDatePicker.Value:yyyyMMdd}.md");
    }

    private sealed class SearchResultRow
    {
        public string LogDate { get; init; } = string.Empty;

        public string Summary { get; init; } = string.Empty;

        public string Detail { get; init; } = string.Empty;
    }
}

using SqsLogApp.Infrastructure;
using SqsLogApp.Models;

namespace SqsLogApp.Forms;

public sealed class LogEditorForm : Form
{
    private readonly WorkLogRepository _repository;
    private readonly DateTimePicker _datePicker;
    private readonly ListBox _entriesListBox;
    private readonly TextBox _summaryTextBox;
    private readonly TextBox _detailTextBox;

    private long? _currentEntryId;

    public LogEditorForm(WorkLogRepository repository)
    {
        _repository = repository;
        Text = "日志编辑";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(850, 560);
        Size = new Size(980, 620);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var topPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        topPanel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "日志日期：",
            Margin = new Padding(0, 10, 0, 0)
        });

        _datePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd",
            Width = 140
        };
        _datePicker.ValueChanged += (_, _) =>
        {
            LoadEntries();
            StartNewEntry();
        };
        topPanel.Controls.Add(_datePicker);
        root.Controls.Add(topPanel, 0, 0);

        var bodySplitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 260
        };
        root.Controls.Add(bodySplitContainer, 0, 1);

        _entriesListBox = new ListBox
        {
            Dock = DockStyle.Fill
        };
        _entriesListBox.SelectedIndexChanged += (_, _) => LoadSelectedEntry();
        bodySplitContainer.Panel1.Controls.Add(_entriesListBox);

        var editPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5
        };
        editPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        editPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        editPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        editPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        editPanel.Controls.Add(new Label
        {
            Text = "描述（单行）：",
            AutoSize = true
        }, 0, 0);

        _summaryTextBox = new TextBox
        {
            Dock = DockStyle.Fill
        };
        editPanel.Controls.Add(_summaryTextBox, 0, 1);

        editPanel.Controls.Add(new Label
        {
            Text = "详情（多行）：",
            AutoSize = true
        }, 0, 2);

        _detailTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        editPanel.Controls.Add(_detailTextBox, 0, 3);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var saveButton = new Button
        {
            Text = "保存",
            AutoSize = true
        };
        saveButton.Click += (_, _) => SaveEntry();

        var deleteButton = new Button
        {
            Text = "删除",
            AutoSize = true
        };
        deleteButton.Click += (_, _) => DeleteEntry();

        var newButton = new Button
        {
            Text = "新增",
            AutoSize = true
        };
        newButton.Click += (_, _) => StartNewEntry();

        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(deleteButton);
        buttonPanel.Controls.Add(newButton);
        editPanel.Controls.Add(buttonPanel, 0, 4);

        bodySplitContainer.Panel2.Controls.Add(editPanel);
        Controls.Add(root);
    }

    public void PrepareForDate(DateTime date)
    {
        _datePicker.Value = date.Date;
        LoadEntries();
        StartNewEntry();
    }

    private void LoadEntries()
    {
        var entries = _repository.GetByDate(_datePicker.Value.Date).ToList();
        _entriesListBox.DataSource = null;
        _entriesListBox.DataSource = entries;
        _entriesListBox.DisplayMember = nameof(WorkLogEntry.Summary);
    }

    private void LoadSelectedEntry()
    {
        if (_entriesListBox.SelectedItem is not WorkLogEntry selectedEntry)
        {
            return;
        }

        _currentEntryId = selectedEntry.Id;
        _summaryTextBox.Text = selectedEntry.Summary;
        _detailTextBox.Text = selectedEntry.Detail;
    }

    private void StartNewEntry()
    {
        _entriesListBox.ClearSelected();
        _currentEntryId = null;
        _summaryTextBox.Clear();
        _detailTextBox.Clear();
        _summaryTextBox.Focus();
    }

    private void SaveEntry()
    {
        var summary = _summaryTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(summary))
        {
            MessageBox.Show("描述不能为空。", "日志编辑", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var detail = _detailTextBox.Text;
        long selectedId;
        if (_currentEntryId is null)
        {
            selectedId = _repository.Add(_datePicker.Value.Date, summary, detail);
        }
        else
        {
            selectedId = _currentEntryId.Value;
            _repository.Update(selectedId, _datePicker.Value.Date, summary, detail);
        }

        LoadEntries();
        SelectEntryById(selectedId);
    }

    private void DeleteEntry()
    {
        if (_currentEntryId is null)
        {
            return;
        }

        var dialogResult = MessageBox.Show("确认删除当前日志吗？", "日志编辑", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (dialogResult != DialogResult.Yes)
        {
            return;
        }

        _repository.Delete(_currentEntryId.Value);
        LoadEntries();
        StartNewEntry();
    }

    private void SelectEntryById(long entryId)
    {
        for (var index = 0; index < _entriesListBox.Items.Count; index++)
        {
            if (_entriesListBox.Items[index] is WorkLogEntry entry && entry.Id == entryId)
            {
                _entriesListBox.SelectedIndex = index;
                return;
            }
        }
    }
}

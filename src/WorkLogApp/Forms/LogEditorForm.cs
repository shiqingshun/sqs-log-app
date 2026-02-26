using WorkLogApp.Infrastructure;
using WorkLogApp.Models;

namespace WorkLogApp.Forms;

public sealed class LogEditorForm : Form
{
    private const int DefaultListVisibleRows = 5;

    private readonly WorkLogRepository _repository;
    private readonly ListBox _entriesListBox;
    private readonly TextBox _summaryTextBox;
    private readonly TextBox _detailTextBox;
    private readonly Button _saveButton;
    private readonly DateTimePicker _datePicker;
    private readonly ToolStripMenuItem _editMenuItem;
    private readonly ToolStripMenuItem _deleteMenuItem;

    private DateTime _selectedDate = DateTime.Today;

    private long? _currentEntryId;

    public LogEditorForm(WorkLogRepository repository)
    {
        _repository = repository;
        Text = "日志编辑";
        Icon = global::WorkLogApp.AppBranding.AppIcon;
        this.EnableEscClose();
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 500);
        Size = new Size(860, 560);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));

        var addSection = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        addSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
        addSection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

        var addLeftPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Padding = new Padding(0, 4, 10, 0)
        };
        addLeftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
        addLeftPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        addLeftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        addLeftPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        addLeftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        addLeftPanel.Controls.Add(new Label
        {
            Text = "日期：",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        }, 0, 0);
        _datePicker = new DateTimePicker
        {
            Dock = DockStyle.Left,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd",
            Width = 180,
            Margin = new Padding(0, 6, 0, 6)
        };
        _datePicker.ValueChanged += (_, _) => OnDateChanged();
        addLeftPanel.Controls.Add(_datePicker, 1, 0);

        addLeftPanel.Controls.Add(new Label
        {
            Text = "描述：",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        }, 0, 1);

        _summaryTextBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 6, 0, 6)
        };
        _summaryTextBox.KeyDown += SummaryTextBoxOnKeyDown;
        addLeftPanel.Controls.Add(_summaryTextBox, 1, 1);

        addLeftPanel.Controls.Add(new Label
        {
            Text = "详情：",
            TextAlign = ContentAlignment.TopLeft,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 0)
        }, 0, 2);

        _detailTextBox = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            ScrollBars = ScrollBars.Vertical,
            Margin = new Padding(0, 8, 0, 0)
        };
        addLeftPanel.Controls.Add(_detailTextBox, 1, 2);

        var addRightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(6)
        };

        _saveButton = new Button
        {
            Text = "保存",
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 24F, FontStyle.Bold)
        };
        _saveButton.Click += (_, _) => SaveEntry();
        addRightPanel.Controls.Add(_saveButton);

        addSection.Controls.Add(addLeftPanel, 0, 0);
        addSection.Controls.Add(addRightPanel, 1, 0);
        root.Controls.Add(addSection, 0, 0);

        var listSection = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        listSection.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        listSection.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        listSection.Controls.Add(new Label
        {
            Text = "当日日志记录（右键菜单可编辑/删除）",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var listPanel = new Panel
        {
            Dock = DockStyle.Fill
        };
        _entriesListBox = new ListBox
        {
            Dock = DockStyle.Top,
            IntegralHeight = false
        };
        _entriesListBox.Height = _entriesListBox.ItemHeight * DefaultListVisibleRows + 8;
        _entriesListBox.MouseDown += EntriesListBoxOnMouseDown;
        listPanel.Controls.Add(_entriesListBox);
        listSection.Controls.Add(listPanel, 0, 1);
        root.Controls.Add(listSection, 0, 1);

        var contextMenu = new ContextMenuStrip();
        _editMenuItem = new ToolStripMenuItem("编辑", global::WorkLogApp.AppBranding.CreateMenuIcon(global::WorkLogApp.AppMenuIconKind.Edit));
        _editMenuItem.Click += (_, _) => EditSelectedEntry();
        _deleteMenuItem = new ToolStripMenuItem("删除", global::WorkLogApp.AppBranding.CreateMenuIcon(global::WorkLogApp.AppMenuIconKind.Delete));
        _deleteMenuItem.Click += (_, _) => DeleteSelectedEntry();
        contextMenu.Items.AddRange([_editMenuItem, _deleteMenuItem]);
        contextMenu.Opening += (_, _) => UpdateContextMenuState();
        _entriesListBox.ContextMenuStrip = contextMenu;

        Controls.Add(root);

        PrepareForDate(DateTime.Today);
    }

    public void PrepareForDate(DateTime date)
    {
        _selectedDate = date.Date;
        _datePicker.Value = _selectedDate;
        LoadEntries();
        StartNewEntry();
    }

    private void OnDateChanged()
    {
        _selectedDate = _datePicker.Value.Date;
        LoadEntries();
        StartNewEntry();
    }

    private void LoadEntries()
    {
        var entries = _repository.GetByDate(_selectedDate).ToList();
        _entriesListBox.DataSource = null;
        _entriesListBox.DataSource = entries;
        _entriesListBox.DisplayMember = nameof(WorkLogEntry.Summary);
    }

    private void StartNewEntry()
    {
        _entriesListBox.ClearSelected();
        _currentEntryId = null;
        _summaryTextBox.Clear();
        _detailTextBox.Clear();
        FocusSummaryInput();
    }

    public void FocusSummaryInput()
    {
        if (!IsHandleCreated || IsDisposed)
        {
            return;
        }

        BeginInvoke(() =>
        {
            if (!Visible)
            {
                return;
            }

            _summaryTextBox.Focus();
            _summaryTextBox.SelectionStart = _summaryTextBox.TextLength;
        });
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
        var isNewEntry = _currentEntryId is null;
        if (_currentEntryId is null)
        {
            selectedId = _repository.Add(_selectedDate, summary, detail);
        }
        else
        {
            selectedId = _currentEntryId.Value;
            _repository.Update(selectedId, _selectedDate, summary, detail);
        }

        LoadEntries();
        if (isNewEntry)
        {
            Close();
            return;
        }

        SelectEntryById(selectedId);
    }

    private void SummaryTextBoxOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
        SaveEntry();
    }

    private void EditSelectedEntry()
    {
        if (!TryGetSelectedEntry(out var selectedEntry))
        {
            return;
        }

        _currentEntryId = selectedEntry.Id;
        _summaryTextBox.Text = selectedEntry.Summary;
        _detailTextBox.Text = selectedEntry.Detail;
        _summaryTextBox.Focus();
        _summaryTextBox.SelectionStart = _summaryTextBox.TextLength;
    }

    private void DeleteSelectedEntry()
    {
        if (!TryGetSelectedEntry(out var selectedEntry))
        {
            return;
        }

        var dialogResult = MessageBox.Show("确认删除当前日志吗？", "日志编辑", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (dialogResult != DialogResult.Yes)
        {
            return;
        }

        _repository.Delete(selectedEntry.Id);
        LoadEntries();

        if (_currentEntryId == selectedEntry.Id)
        {
            StartNewEntry();
        }
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

    private bool TryGetSelectedEntry(out WorkLogEntry selectedEntry)
    {
        if (_entriesListBox.SelectedItem is WorkLogEntry entry)
        {
            selectedEntry = entry;
            return true;
        }

        selectedEntry = null!;
        return false;
    }

    private void EntriesListBoxOnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        var index = _entriesListBox.IndexFromPoint(e.Location);
        if (index == ListBox.NoMatches)
        {
            _entriesListBox.ClearSelected();
            return;
        }

        _entriesListBox.SelectedIndex = index;
    }

    private void UpdateContextMenuState()
    {
        var hasSelection = _entriesListBox.SelectedItem is WorkLogEntry;
        _editMenuItem.Enabled = hasSelection;
        _deleteMenuItem.Enabled = hasSelection;
    }
}

using SqsLogApp.Infrastructure;
using SqsLogApp.Models;

namespace SqsLogApp.Forms;

public sealed class SettingsForm : Form
{
    private readonly string _databasePath;
    private readonly CheckBox _autoStartCheckBox;
    private readonly TextBox _hotkeyTextBox;

    public SettingsForm(AppConfig currentConfig)
    {
        _databasePath = currentConfig.DatabasePath;

        Text = "设置";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(460, 210);

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 4,
            ColumnCount = 2
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        table.Controls.Add(new Label
        {
            Text = "全局快捷键：",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        }, 0, 0);

        _hotkeyTextBox = new TextBox
        {
            Text = currentConfig.Hotkey,
            Dock = DockStyle.Fill
        };
        table.Controls.Add(_hotkeyTextBox, 1, 0);

        table.Controls.Add(new Label
        {
            Text = "系统自启动：",
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        }, 0, 1);

        _autoStartCheckBox = new CheckBox
        {
            Checked = currentConfig.AutoStart,
            Text = "启用（注册表 Run）",
            Dock = DockStyle.Left
        };
        table.Controls.Add(_autoStartCheckBox, 1, 1);

        table.Controls.Add(new Label
        {
            Text = "示例：Win+Shift+L，修改后保存将立即生效。",
            AutoSize = true,
            Dock = DockStyle.Fill
        }, 1, 2);

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
        saveButton.Click += (_, _) => SaveConfiguration();

        var cancelButton = new Button
        {
            Text = "取消",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(cancelButton);
        table.Controls.Add(buttonPanel, 1, 3);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Controls.Add(table);
    }

    public AppConfig? SavedConfig { get; private set; }

    private void SaveConfiguration()
    {
        var hotkeyText = _hotkeyTextBox.Text.Trim();
        if (!HotkeyParser.TryParse(hotkeyText, out var hotkeyDefinition))
        {
            MessageBox.Show("快捷键格式无效，请使用类似 Win+Shift+L 的组合。", "设置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SavedConfig = new AppConfig
        {
            Hotkey = HotkeyParser.ToDisplayText(hotkeyDefinition),
            AutoStart = _autoStartCheckBox.Checked,
            DatabasePath = _databasePath
        };

        DialogResult = DialogResult.OK;
        Close();
    }
}

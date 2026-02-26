using SqsLogApp.Forms;
using SqsLogApp.Infrastructure;
using SqsLogApp.Models;
using SqsLogApp.Services;

namespace SqsLogApp;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly YamlConfigService _configService;
    private readonly AutostartService _autostartService;
    private readonly GlobalHotkeyListener _hotkeyListener;
    private readonly MarkdownExportService _exportService;

    private AppConfig _config;
    private WorkLogRepository _repository;

    private LogEditorForm? _logEditorForm;
    private LogManagerForm? _logManagerForm;
    private bool _isExiting;

    public TrayApplicationContext()
    {
        _configService = new YamlConfigService();
        _autostartService = new AutostartService();
        _exportService = new MarkdownExportService();
        _config = _configService.Load();
        _repository = new WorkLogRepository(_config.DatabasePath);
        _hotkeyListener = new GlobalHotkeyListener(ShowLogEditorWindow);

        _notifyIcon = CreateNotifyIcon();
        ApplyConfiguration();
    }

    private NotifyIcon CreateNotifyIcon()
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.ImageScalingSize = new Size(16, 16);
        contextMenu.Items.Add(new ToolStripMenuItem("编辑日志", AppBranding.CreateMenuIcon(AppMenuIconKind.Edit), (_, _) => ShowLogEditorWindow()));
        contextMenu.Items.Add(new ToolStripMenuItem("管理日志", AppBranding.CreateMenuIcon(AppMenuIconKind.Manage), (_, _) => ShowLogManagerWindow()));
        contextMenu.Items.Add(new ToolStripMenuItem("设置", AppBranding.CreateMenuIcon(AppMenuIconKind.Settings), (_, _) => ShowSettingsWindow()));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(new ToolStripMenuItem("退出", AppBranding.CreateMenuIcon(AppMenuIconKind.Exit), (_, _) => ExitApplication()));

        var icon = new NotifyIcon
        {
            Icon = AppBranding.AppIcon,
            Visible = true,
            Text = "工作日志记录工具",
            ContextMenuStrip = contextMenu
        };
        icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowLogEditorWindow();
            }
        };
        return icon;
    }

    private void ApplyConfiguration()
    {
        if (!HotkeyParser.TryParse(_config.Hotkey, out var hotkeyDefinition))
        {
            throw new InvalidOperationException($"配置中的快捷键无效：{_config.Hotkey}");
        }

        if (!_hotkeyListener.Register(hotkeyDefinition))
        {
            MessageBox.Show(
                $"全局快捷键 {HotkeyParser.ToDisplayText(hotkeyDefinition)} 注册失败，请在设置中修改。",
                "快捷键注册失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        _autostartService.SetEnabled(_config.AutoStart, Application.ExecutablePath);
    }

    private void ShowLogEditorWindow()
    {
        if (_isExiting)
        {
            return;
        }

        if (_logEditorForm is null || _logEditorForm.IsDisposed)
        {
            _logEditorForm = new LogEditorForm(_repository);
        }

        _logEditorForm.PrepareForDate(DateTime.Today);
        if (!_logEditorForm.Visible)
        {
            _logEditorForm.Show();
        }

        _logEditorForm.WindowState = FormWindowState.Normal;
        _logEditorForm.BringToFront();
        _logEditorForm.Activate();
        _logEditorForm.FocusSummaryInput();
    }

    private void ShowLogManagerWindow()
    {
        if (_isExiting)
        {
            return;
        }

        if (_logManagerForm is null || _logManagerForm.IsDisposed)
        {
            _logManagerForm = new LogManagerForm(_repository, _exportService);
        }

        _logManagerForm.RefreshData();
        if (!_logManagerForm.Visible)
        {
            _logManagerForm.Show();
        }

        _logManagerForm.WindowState = FormWindowState.Normal;
        _logManagerForm.BringToFront();
        _logManagerForm.Activate();
    }

    private void ShowSettingsWindow()
    {
        using var settingsForm = new SettingsForm(_config);
        if (settingsForm.ShowDialog() != DialogResult.OK || settingsForm.SavedConfig is null)
        {
            return;
        }

        var updatedConfig = settingsForm.SavedConfig;
        _config = updatedConfig;
        _configService.Save(_config);

        if (!string.Equals(_repository.DatabasePath, _config.DatabasePath, StringComparison.OrdinalIgnoreCase))
        {
            _repository.Dispose();
            _repository = new WorkLogRepository(_config.DatabasePath);

            if (_logEditorForm is { IsDisposed: false })
            {
                _logEditorForm.Close();
                _logEditorForm = null;
            }

            if (_logManagerForm is { IsDisposed: false })
            {
                _logManagerForm.Close();
                _logManagerForm = null;
            }
        }

        ApplyConfiguration();
    }

    private void ExitApplication()
    {
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _notifyIcon.Visible = false;

        if (_logEditorForm is { IsDisposed: false })
        {
            _logEditorForm.Close();
        }

        if (_logManagerForm is { IsDisposed: false })
        {
            _logManagerForm.Close();
        }

        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkeyListener.Dispose();
            _notifyIcon.Dispose();
            _logEditorForm?.Dispose();
            _logManagerForm?.Dispose();
            _repository.Dispose();
        }

        base.Dispose(disposing);
    }
}

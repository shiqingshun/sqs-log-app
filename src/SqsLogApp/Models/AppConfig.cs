namespace SqsLogApp.Models;

public sealed class AppConfig
{
    public string Hotkey { get; set; } = "Win+Shift+L";

    public bool AutoStart { get; set; }

    public string DatabasePath { get; set; } = AppPaths.DefaultDatabasePath;
}

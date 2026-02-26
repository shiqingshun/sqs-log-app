using Microsoft.Win32;

namespace WorkLogApp.Infrastructure;

public sealed class AutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "work-log-app";

    public void SetEnabled(bool enabled, string executablePath)
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (enabled)
        {
            runKey.SetValue(AppName, $"\"{executablePath}\"");
            return;
        }

        if (runKey.GetValue(AppName) is not null)
        {
            runKey.DeleteValue(AppName);
        }
    }
}

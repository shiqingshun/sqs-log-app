using Microsoft.Win32;

namespace SqsLogApp.Infrastructure;

public sealed class AutostartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "SqsLogApp";

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

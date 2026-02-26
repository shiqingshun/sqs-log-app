using System.Runtime.InteropServices;

namespace SqsLogApp.Infrastructure;

internal sealed class GlobalHotkeyListener : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0x3120;

    private readonly Action _onHotkeyPressed;
    private bool _isRegistered;

    public GlobalHotkeyListener(Action onHotkeyPressed)
    {
        _onHotkeyPressed = onHotkeyPressed;
        CreateHandle(new CreateParams());
    }

    public bool Register(HotkeyDefinition definition)
    {
        Unregister();
        _isRegistered = RegisterHotKey(
            Handle,
            HotkeyId,
            (uint)definition.Modifiers,
            (uint)definition.Key);

        return _isRegistered;
    }

    public void Unregister()
    {
        if (!_isRegistered)
        {
            return;
        }

        UnregisterHotKey(Handle, HotkeyId);
        _isRegistered = false;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam.ToInt32() == HotkeyId)
        {
            _onHotkeyPressed();
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        Unregister();
        DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

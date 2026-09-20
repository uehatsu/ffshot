using Windows.Win32;
using Windows.Win32.Foundation;

namespace FFShot.Hotkeys;

/// <summary>非表示ウィンドウで RegisterHotKey / WM_HOTKEY を扱う。UI スレッドで使うこと。</summary>
internal sealed class HotkeyManager : NativeWindow, IDisposable
{
    private readonly Dictionary<int, HotkeyBinding> _registered = new();
    private bool _disposed;

    public event Action<int>? HotkeyPressed;

    public HotkeyManager()
    {
        CreateHandle(new CreateParams());
    }

    public IReadOnlyDictionary<int, HotkeyBinding> Registered => _registered;

    /// <summary>登録に成功したら true。既に同じ id が登録済みなら差し替える。</summary>
    public bool Register(int id, HotkeyBinding binding)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Unregister(id);
        if (binding.IsEmpty)
        {
            return false;
        }

        var ok = PInvoke.RegisterHotKey(new HWND(Handle), id, binding.ToWin32Modifiers(), binding.VirtualKey);
        if (ok)
        {
            _registered[id] = binding;
        }
        return ok;
    }

    public void Unregister(int id)
    {
        if (_registered.Remove(id) && Handle != IntPtr.Zero)
        {
            PInvoke.UnregisterHotKey(new HWND(Handle), id);
        }
    }

    public void UnregisterAll()
    {
        foreach (var id in _registered.Keys.ToArray())
        {
            Unregister(id);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == (int)PInvoke.WM_HOTKEY)
        {
            HotkeyPressed?.Invoke((int)m.WParam);
            return;
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        UnregisterAll();
        DestroyHandle();
    }
}

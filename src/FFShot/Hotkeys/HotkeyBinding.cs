using System.Text;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace FFShot.Hotkeys;

[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Ctrl = 2,
    Shift = 4,
    Win = 8,
}

/// <summary>"Ctrl+Shift+F12" のような文字列と RegisterHotKey 引数の相互変換。</summary>
public readonly record struct HotkeyBinding(HotkeyModifiers Modifiers, Keys Key)
{
    public static readonly HotkeyBinding None = new(HotkeyModifiers.None, Keys.None);

    public bool IsEmpty => Key == Keys.None;

    public static bool TryParse(string? text, out HotkeyBinding binding)
    {
        binding = None;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var mods = HotkeyModifiers.None;
        var key = Keys.None;
        foreach (var raw in text.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl" or "control":
                    mods |= HotkeyModifiers.Ctrl;
                    break;
                case "shift":
                    mods |= HotkeyModifiers.Shift;
                    break;
                case "alt":
                    mods |= HotkeyModifiers.Alt;
                    break;
                case "win" or "windows":
                    mods |= HotkeyModifiers.Win;
                    break;
                default:
                    if (key != Keys.None || !TryParseKey(raw, out key))
                    {
                        return false;
                    }
                    break;
            }
        }

        if (key == Keys.None)
        {
            return false;
        }

        binding = new HotkeyBinding(mods, key);
        return true;
    }

    private static bool TryParseKey(string raw, out Keys key)
    {
        // 数字は "D0".."D9" として Keys に定義されている
        if (raw.Length == 1 && char.IsDigit(raw[0]))
        {
            raw = "D" + raw;
        }

        if (Enum.TryParse(raw, ignoreCase: true, out key) && !IsModifierKey(key) && key != Keys.None)
        {
            key &= Keys.KeyCode;
            return true;
        }

        key = Keys.None;
        return false;
    }

    public static bool IsModifierKey(Keys key)
    {
        var code = key & Keys.KeyCode;
        return code is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu
            or Keys.LWin or Keys.RWin;
    }

    /// <summary>WinForms の KeyEventArgs からバインディングを組み立てる。修飾キーだけの場合は null。</summary>
    public static HotkeyBinding? FromKeyEvent(KeyEventArgs e, bool winPressed = false)
    {
        var code = e.KeyCode & Keys.KeyCode;
        if (code == Keys.None || IsModifierKey(code))
        {
            return null;
        }

        var mods = HotkeyModifiers.None;
        if (e.Control) mods |= HotkeyModifiers.Ctrl;
        if (e.Shift) mods |= HotkeyModifiers.Shift;
        if (e.Alt) mods |= HotkeyModifiers.Alt;
        if (winPressed) mods |= HotkeyModifiers.Win;
        return new HotkeyBinding(mods, code);
    }

    internal HOT_KEY_MODIFIERS ToWin32Modifiers()
    {
        HOT_KEY_MODIFIERS m = HOT_KEY_MODIFIERS.MOD_NOREPEAT;
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) m |= HOT_KEY_MODIFIERS.MOD_ALT;
        if (Modifiers.HasFlag(HotkeyModifiers.Ctrl)) m |= HOT_KEY_MODIFIERS.MOD_CONTROL;
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) m |= HOT_KEY_MODIFIERS.MOD_SHIFT;
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) m |= HOT_KEY_MODIFIERS.MOD_WIN;
        return m;
    }

    public uint VirtualKey => (uint)(Key & Keys.KeyCode);

    public override string ToString()
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        if (Modifiers.HasFlag(HotkeyModifiers.Ctrl)) sb.Append("Ctrl+");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) sb.Append("Shift+");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) sb.Append("Alt+");
        if (Modifiers.HasFlag(HotkeyModifiers.Win)) sb.Append("Win+");
        sb.Append(KeyName(Key));
        return sb.ToString();
    }

    private static string KeyName(Keys key)
    {
        var code = key & Keys.KeyCode;
        if (code is >= Keys.D0 and <= Keys.D9)
        {
            return ((char)('0' + (code - Keys.D0))).ToString();
        }
        return code.ToString();
    }
}

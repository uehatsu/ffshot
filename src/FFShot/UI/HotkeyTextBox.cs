using FFShot.Hotkeys;
using FFShot.Resources;

namespace FFShot.UI;

/// <summary>キー入力をそのままバインディングとして取り込む読み取り専用テキストボックス。</summary>
internal sealed class HotkeyTextBox : TextBox
{
    private HotkeyBinding _binding = HotkeyBinding.None;

    public HotkeyTextBox()
    {
        ReadOnly = true;
        BackColor = SystemColors.Window;
        ShortcutsEnabled = false;
        PlaceholderText = Strings.Hotkey_Placeholder;
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public HotkeyBinding Binding
    {
        get => _binding;
        set
        {
            _binding = value;
            Text = value.ToString();
        }
    }

    protected override bool IsInputKey(Keys keyData) => true;

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Tab などをフォームに奪われないようにする
        return false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;

        var code = e.KeyCode & Keys.KeyCode;
        if (code is Keys.Back or Keys.Delete)
        {
            Binding = HotkeyBinding.None;
            return;
        }

        var winPressed = (ModifierKeys & Keys.LWin) != 0 || IsWinDown();
        var b = HotkeyBinding.FromKeyEvent(e, winPressed);
        if (b is { } value)
        {
            Binding = value;
        }
        else
        {
            // 修飾キーのみ: 途中経過を表示
            Text = new HotkeyBinding(CurrentModifiers(e, winPressed), Keys.None).ToString() + "…";
        }
    }

    private static HotkeyModifiers CurrentModifiers(KeyEventArgs e, bool win)
    {
        var m = HotkeyModifiers.None;
        if (e.Control) m |= HotkeyModifiers.Ctrl;
        if (e.Shift) m |= HotkeyModifiers.Shift;
        if (e.Alt) m |= HotkeyModifiers.Alt;
        if (win) m |= HotkeyModifiers.Win;
        return m;
    }

    private static bool IsWinDown()
    {
        return (Windows.Win32.PInvoke.GetAsyncKeyState((int)Keys.LWin) & 0x8000) != 0
            || (Windows.Win32.PInvoke.GetAsyncKeyState((int)Keys.RWin) & 0x8000) != 0;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        e.Handled = true;

        // PrintScreen は Windows が KeyDown を送らず KeyUp しか来ないので、ここで拾う
        if ((e.KeyCode & Keys.KeyCode) == Keys.PrintScreen)
        {
            var b = HotkeyBinding.FromKeyEvent(e, IsWinDown());
            if (b is { } value)
            {
                Binding = value;
            }
            return;
        }

        // 修飾キーだけ押して離した場合は元の表示に戻す
        if (Text.EndsWith('…'))
        {
            Text = _binding.ToString();
        }
    }

    /// <summary>テスト用に KeyDown / KeyUp を外から流し込む。</summary>
    internal void SimulateKeyDown(KeyEventArgs e) => OnKeyDown(e);
    internal void SimulateKeyUp(KeyEventArgs e) => OnKeyUp(e);
}

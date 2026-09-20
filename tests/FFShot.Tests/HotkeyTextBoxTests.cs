using FFShot.Hotkeys;
using FFShot.UI;

namespace FFShot.Tests;

public class HotkeyTextBoxTests
{
    [Fact]
    public void KeyDown_SetsBinding()
    {
        using var box = new HotkeyTextBox();
        box.SimulateKeyDown(new KeyEventArgs(Keys.Control | Keys.Shift | Keys.F11));
        Assert.Equal("Ctrl+Shift+F11", box.Binding.ToString());
        Assert.Equal("Ctrl+Shift+F11", box.Text);
    }

    [Fact]
    public void PrintScreen_IsCapturedOnKeyUp()
    {
        // PrintScreen は KeyDown が来ないので KeyUp だけで設定できること
        using var box = new HotkeyTextBox();
        box.SimulateKeyUp(new KeyEventArgs(Keys.PrintScreen));
        Assert.Equal(new HotkeyBinding(HotkeyModifiers.None, Keys.PrintScreen), box.Binding);

        box.SimulateKeyUp(new KeyEventArgs(Keys.Shift | Keys.PrintScreen));
        Assert.Equal("Shift+PrintScreen", box.Binding.ToString());
    }

    [Fact]
    public void ModifierOnly_DoesNotChangeBinding()
    {
        using var box = new HotkeyTextBox();
        box.SimulateKeyDown(new KeyEventArgs(Keys.F5));
        box.SimulateKeyDown(new KeyEventArgs(Keys.Control | Keys.ControlKey));
        Assert.EndsWith("…", box.Text);
        box.SimulateKeyUp(new KeyEventArgs(Keys.ControlKey));
        Assert.Equal("F5", box.Text);
        Assert.Equal(Keys.F5, box.Binding.Key);
    }

    [Fact]
    public void Backspace_Clears()
    {
        using var box = new HotkeyTextBox();
        box.SimulateKeyDown(new KeyEventArgs(Keys.F5));
        box.SimulateKeyDown(new KeyEventArgs(Keys.Back));
        Assert.True(box.Binding.IsEmpty);
        Assert.Equal("", box.Text);
    }
}

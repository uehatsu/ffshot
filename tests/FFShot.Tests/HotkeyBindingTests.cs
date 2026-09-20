using FFShot.Hotkeys;

namespace FFShot.Tests;

public class HotkeyBindingTests
{
    [Theory]
    [InlineData("Ctrl+Shift+F12", HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, Keys.F12)]
    [InlineData("alt+p", HotkeyModifiers.Alt, Keys.P)]
    [InlineData("Win+PrintScreen", HotkeyModifiers.Win, Keys.PrintScreen)]
    [InlineData("F5", HotkeyModifiers.None, Keys.F5)]
    [InlineData("Ctrl+1", HotkeyModifiers.Ctrl, Keys.D1)]
    [InlineData(" Control + Shift + A ", HotkeyModifiers.Ctrl | HotkeyModifiers.Shift, Keys.A)]
    public void TryParse_ParsesValidStrings(string text, HotkeyModifiers mods, Keys key)
    {
        Assert.True(HotkeyBinding.TryParse(text, out var b));
        Assert.Equal(mods, b.Modifiers);
        Assert.Equal(key, b.Key);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Ctrl+Shift")]
    [InlineData("Ctrl+Foo")]
    [InlineData("A+B")]
    [InlineData("ShiftKey")]
    public void TryParse_RejectsInvalidStrings(string? text)
    {
        Assert.False(HotkeyBinding.TryParse(text, out var b));
        Assert.True(b.IsEmpty);
    }

    [Theory]
    [InlineData("Ctrl+Shift+F12")]
    [InlineData("Alt+P")]
    [InlineData("Ctrl+Shift+Alt+Win+Z")]
    [InlineData("Ctrl+1")]
    public void ToString_RoundTrips(string text)
    {
        Assert.True(HotkeyBinding.TryParse(text, out var b));
        Assert.Equal(text, b.ToString());
    }

    [Fact]
    public void ToString_UsesCanonicalOrder()
    {
        Assert.True(HotkeyBinding.TryParse("Alt+Shift+Ctrl+X", out var b));
        Assert.Equal("Ctrl+Shift+Alt+X", b.ToString());
    }

    [Fact]
    public void Win32Modifiers_IncludeNoRepeat()
    {
        Assert.True(HotkeyBinding.TryParse("Ctrl+Alt+K", out var b));
        var m = b.ToWin32Modifiers();
        Assert.True(m.HasFlag(Windows.Win32.UI.Input.KeyboardAndMouse.HOT_KEY_MODIFIERS.MOD_CONTROL));
        Assert.True(m.HasFlag(Windows.Win32.UI.Input.KeyboardAndMouse.HOT_KEY_MODIFIERS.MOD_ALT));
        Assert.True(m.HasFlag(Windows.Win32.UI.Input.KeyboardAndMouse.HOT_KEY_MODIFIERS.MOD_NOREPEAT));
        Assert.False(m.HasFlag(Windows.Win32.UI.Input.KeyboardAndMouse.HOT_KEY_MODIFIERS.MOD_SHIFT));
        Assert.Equal((uint)Keys.K, b.VirtualKey);
    }
}

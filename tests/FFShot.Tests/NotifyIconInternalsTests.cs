using System.Reflection;

namespace FFShot.Tests;

public class NotifyIconInternalsTests
{
    [Fact]
    public void ShowContextMenu_PrivateMethod_Exists()
    {
        // TrayApplicationContext が左クリックでメニューを出すために反射で呼んでいる内部メソッド。
        // 無くなればフォールバック（座標指定表示）に切り替わるが、気づけるようにここで検知する。
        var m = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(m);
        Assert.Empty(m.GetParameters());
    }
}

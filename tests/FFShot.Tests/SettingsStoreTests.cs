using FFShot.Settings;

namespace FFShot.Tests;

public class SettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ffshot-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileMissing()
    {
        var store = new SettingsStore(Path.Combine(_dir, "settings.json"));
        var s = store.Load();
        Assert.Equal("Ctrl+Shift+F12", s.HotkeyFullScreen);
        Assert.Equal(CaptureBackendKind.Gdi, s.Backend);
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var store = new SettingsStore(Path.Combine(_dir, "settings.json"));
        var s = new AppSettings
        {
            HotkeyFullScreen = "Alt+P",
            Backend = CaptureBackendKind.Direct3D,
            SaveFolder = @"C:\shots",
            IncludeCursor = true,
        };
        store.Save(s);

        var loaded = store.Load();
        Assert.Equal("Alt+P", loaded.HotkeyFullScreen);
        Assert.Equal(CaptureBackendKind.Direct3D, loaded.Backend);
        Assert.Equal(@"C:\shots", loaded.SaveFolder);
        Assert.True(loaded.IncludeCursor);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenJsonIsBroken()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "settings.json");
        File.WriteAllText(path, "{ not json");
        var s = new SettingsStore(path).Load();
        Assert.Equal(CaptureBackendKind.Gdi, s.Backend);
    }

    [Fact]
    public void EnumIsSerializedAsString()
    {
        var path = Path.Combine(_dir, "settings.json");
        new SettingsStore(path).Save(new AppSettings { Backend = CaptureBackendKind.Direct3D });
        Assert.Contains("\"Direct3D\"", File.ReadAllText(path));
    }
}

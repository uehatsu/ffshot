using FFShot.Hotkeys;
using FFShot.Settings;

namespace FFShot.UI;

internal sealed class SettingsForm : Form
{
    private readonly HotkeyTextBox _hkFull = new();
    private readonly HotkeyTextBox _hkActive = new();
    private readonly ComboBox _backend = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _folder = new();
    private readonly TextBox _pattern = new();
    private readonly CheckBox _allMonitors = new() { Text = "全画面撮影で全モニターを結合する", AutoSize = true };
    private readonly CheckBox _cursor = new() { Text = "マウスカーソルを含める", AutoSize = true };
    private readonly CheckBox _notify = new() { Text = "保存時に通知を表示する", AutoSize = true };
    private readonly CheckBox _startup = new() { Text = "Windows ログオン時に自動起動する", AutoSize = true };

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public AppSettings Result { get; private set; }

    public SettingsForm(AppSettings current)
    {
        Result = current.Clone();

        Text = "ffshot 設定";
        Icon = App.TrayIconFactory.CreateWindowIcon();
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(480, 470);
        Font = new Font("Yu Gothic UI", 9f);

        _backend.Items.AddRange(["通常 (GDI)", "DirectX (Direct3D / Desktop Duplication)"]);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(12),
            AutoSize = true,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var row = 0;
        AddRow(grid, row++, "全画面のホットキー", _hkFull);
        AddRow(grid, row++, "アクティブウィンドウのホットキー", _hkActive);
        AddRow(grid, row++, "取得方式", _backend);

        var browse = new Button { Text = "参照...", AutoSize = true };
        browse.Click += (_, _) => BrowseFolder();
        AddRow(grid, row++, "保存先フォルダ", _folder, browse);
        AddRow(grid, row++, "ファイル名パターン", _pattern);
        var hint = new Label
        {
            Text = "{yyyyMMdd_HHmmss} のように { } 内に日時書式を書けます。拡張子 .png は自動付与。",
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 0, 3, 8),
        };
        grid.Controls.Add(hint, 1, row);
        grid.SetColumnSpan(hint, 2);
        row++;

        foreach (var cb in new[] { _allMonitors, _cursor, _notify, _startup })
        {
            grid.Controls.Add(cb, 1, row);
            grid.SetColumnSpan(cb, 2);
            row++;
        }

        var startupHint = new Label
        {
            Text = App.Elevation.IsElevated
                ? "管理者権限で動作中: タスクスケジューラに登録し、ログオン時に管理者権限で起動します。"
                : "通常権限で動作中: Run キーに登録し、通常権限で起動します。\n" +
                  "管理者権限のアプリ（ゲームなど）でもホットキーを効かせるには、先にトレイメニューの「管理者として再起動」を行ってから設定してください。",
            AutoSize = true,
            MaximumSize = new Size(430, 0),
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(20, 0, 3, 8),
        };
        grid.Controls.Add(startupHint, 1, row);
        grid.SetColumnSpan(startupHint, 2);
        row++;

        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "キャンセル", DialogResult = DialogResult.Cancel, AutoSize = true };
        ok.Click += (_, _) => Apply();
        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            AutoSize = true,
            Padding = new Padding(12, 0, 12, 12),
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        Controls.Add(grid);
        Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = cancel;

        LoadFrom(current);
    }

    private static void AddRow(TableLayoutPanel grid, int row, string label, Control input, Control? extra = null)
    {
        grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 6, 12, 6) }, 0, row);
        input.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        grid.Controls.Add(input, 1, row);
        if (extra is not null)
        {
            grid.Controls.Add(extra, 2, row);
        }
        else
        {
            grid.SetColumnSpan(input, 2);
        }
    }

    private void LoadFrom(AppSettings s)
    {
        _hkFull.Binding = HotkeyBinding.TryParse(s.HotkeyFullScreen, out var f) ? f : HotkeyBinding.None;
        _hkActive.Binding = HotkeyBinding.TryParse(s.HotkeyActiveWindow, out var a) ? a : HotkeyBinding.None;
        _backend.SelectedIndex = s.Backend == CaptureBackendKind.Direct3D ? 1 : 0;
        _folder.Text = s.SaveFolder;
        _pattern.Text = s.FileNamePattern;
        _allMonitors.Checked = s.CaptureAllMonitors;
        _cursor.Checked = s.IncludeCursor;
        _notify.Checked = s.ShowNotification;
        _startup.Checked = s.RunAtStartup;
    }

    private void Apply()
    {
        Result = new AppSettings
        {
            HotkeyFullScreen = _hkFull.Binding.ToString(),
            HotkeyActiveWindow = _hkActive.Binding.ToString(),
            Backend = _backend.SelectedIndex == 1 ? CaptureBackendKind.Direct3D : CaptureBackendKind.Gdi,
            SaveFolder = string.IsNullOrWhiteSpace(_folder.Text) ? new AppSettings().SaveFolder : _folder.Text.Trim(),
            FileNamePattern = string.IsNullOrWhiteSpace(_pattern.Text) ? new AppSettings().FileNamePattern : _pattern.Text.Trim(),
            CaptureAllMonitors = _allMonitors.Checked,
            IncludeCursor = _cursor.Checked,
            ShowNotification = _notify.Checked,
            RunAtStartup = _startup.Checked,
        };
    }

    private void BrowseFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "スクリーンショットの保存先",
            UseDescriptionForTitle = true,
            SelectedPath = Environment.ExpandEnvironmentVariables(_folder.Text),
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _folder.Text = dlg.SelectedPath;
        }
    }
}

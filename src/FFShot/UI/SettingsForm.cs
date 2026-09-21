using FFShot.Hotkeys;
using FFShot.Resources;
using FFShot.Settings;

namespace FFShot.UI;

internal sealed class SettingsForm : Form
{
    private readonly HotkeyTextBox _hkFull = new();
    private readonly HotkeyTextBox _hkActive = new();
    private readonly ComboBox _backend = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _folder = new();
    private readonly TextBox _pattern = new();
    private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TableLayoutPanel _grid;
    private readonly FlowLayoutPanel _buttons;
    private readonly CheckBox _allMonitors = new() { Text = Strings.Settings_AllMonitors, AutoSize = true };
    private readonly CheckBox _cursor = new() { Text = Strings.Settings_IncludeCursor, AutoSize = true };
    private readonly CheckBox _notify = new() { Text = Strings.Settings_ShowNotification, AutoSize = true };
    private readonly CheckBox _startup = new() { Text = Strings.Settings_RunAtStartup, AutoSize = true };

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public AppSettings Result { get; private set; }

    public SettingsForm(AppSettings current)
    {
        Result = current.Clone();

        Text = Strings.Settings_Title;
        Icon = App.TrayIconFactory.CreateWindowIcon();
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        // 96 DPI を基準にすると WinForms が表示スケール（125%〜）に合わせて枠・余白・サイズを拡大する。
        // AutoScaleDimensions を指定しないと拡大率 1 とみなされ、文字だけ大きくなって崩れる。
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(500, 540);
        Font = new Font("Yu Gothic UI", 9f);

        _backend.Items.AddRange([Strings.Settings_Backend_Gdi, Strings.Settings_Backend_Direct3D]);
        _language.Items.AddRange([Strings.Settings_Language_Auto, Strings.Settings_Language_Japanese, Strings.Settings_Language_English]);

        var grid = _grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(12),
            AutoSize = true,
            AutoScroll = true, // 画面が低くて収まらない場合のみスクロール
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var row = 0;
        AddRow(grid, row++, Strings.Settings_HotkeyFullScreen, _hkFull);
        AddRow(grid, row++, Strings.Settings_HotkeyActiveWindow, _hkActive);
        AddRow(grid, row++, Strings.Settings_Backend, _backend);

        var browse = new Button { Text = Strings.Settings_Browse, AutoSize = true };
        browse.Click += (_, _) => BrowseFolder();
        AddRow(grid, row++, Strings.Settings_SaveFolder, _folder, browse);
        AddRow(grid, row++, Strings.Settings_FileNamePattern, _pattern);
        var hint = new Label
        {
            Text = Strings.Settings_FileNameHint,
            AutoSize = true,
            Dock = DockStyle.Fill, // セル幅で折り返す
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
            Text = App.Elevation.IsElevated ? Strings.Settings_StartupHintElevated : Strings.Settings_StartupHintNormal,
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(20, 0, 3, 8),
        };
        grid.Controls.Add(startupHint, 1, row);
        grid.SetColumnSpan(startupHint, 2);
        row++;

        AddRow(grid, row++, Strings.Settings_Language, _language);
        var languageHint = new Label
        {
            Text = Strings.Settings_LanguageHint,
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 0, 3, 8),
        };
        grid.Controls.Add(languageHint, 1, row);
        grid.SetColumnSpan(languageHint, 2);
        row++;

        var ok = new Button { Text = Strings.Settings_OK, DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = Strings.Settings_Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
        ok.Click += (_, _) => Apply();
        var buttons = _buttons = new FlowLayoutPanel
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

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        FitToContent();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        FitToContent();
    }

    /// <summary>
    /// DPI 拡大の適用後に、内容に合わせて幅と高さを決める。
    /// 幅: ラベル列が広くても入力列が最低限の幅を保つように広げる。
    /// 高さ: 言語や折り返しで変わる行数に合わせる。画面に収まらなければスクロールにする。
    /// </summary>
    private void FitToContent()
    {
        // 測定中にスクロールバーが出て幅を奪わないよう、いったん無効にする
        _grid.AutoScroll = false;
        PerformLayout();

        var widths = _grid.GetColumnWidths();
        if (widths.Length == 3)
        {
            // 入力列は最低 280 論理px、かつチェックボックスの文言が切れない幅を確保する
            var minInputWidth = LogicalToDeviceUnits(280);
            foreach (var cb in new[] { _allMonitors, _cursor, _notify, _startup })
            {
                minInputWidth = Math.Max(minInputWidth, cb.PreferredSize.Width + cb.Margin.Horizontal);
            }
            var inputWidth = ClientSize.Width - widths[0] - widths[2] - _grid.Padding.Horizontal;
            if (inputWidth < minInputWidth)
            {
                ClientSize = new Size(ClientSize.Width + (minInputWidth - inputWidth), ClientSize.Height);
            }
        }

        // 折り返しは幅が決まってから確定するので、高さは 2 回測る
        var wanted = MeasureWantedHeight();
        ClientSize = new Size(ClientSize.Width, wanted);
        wanted = MeasureWantedHeight();

        var chrome = Height - ClientSize.Height;
        var maxClientHeight = Screen.FromControl(this).WorkingArea.Height - chrome - LogicalToDeviceUnits(40);
        var fits = wanted <= maxClientHeight;
        _grid.AutoScroll = !fits;
        ClientSize = new Size(ClientSize.Width, Math.Max(LogicalToDeviceUnits(300), fits ? wanted : maxClientHeight));
    }

    private int MeasureWantedHeight()
    {
        PerformLayout();
        return _grid.GetPreferredSize(new Size(ClientSize.Width, 0)).Height + _buttons.Height + LogicalToDeviceUnits(2);
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
        _language.SelectedIndex = (int)s.Language;
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
            Language = (UiLanguage)Math.Max(0, _language.SelectedIndex),
        };
    }

    private void BrowseFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = Strings.Settings_FolderDialogTitle,
            UseDescriptionForTitle = true,
            SelectedPath = Environment.ExpandEnvironmentVariables(_folder.Text),
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _folder.Text = dlg.SelectedPath;
        }
    }
}

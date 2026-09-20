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
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(500, 540);
        Font = new Font("Yu Gothic UI", 9f);

        _backend.Items.AddRange([Strings.Settings_Backend_Gdi, Strings.Settings_Backend_Direct3D]);
        _language.Items.AddRange([Strings.Settings_Language_Auto, Strings.Settings_Language_Japanese, Strings.Settings_Language_English]);

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
            MaximumSize = new Size(450, 0),
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
            MaximumSize = new Size(450, 0),
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
            MaximumSize = new Size(450, 0),
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 0, 3, 8),
        };
        grid.Controls.Add(languageHint, 1, row);
        grid.SetColumnSpan(languageHint, 2);
        row++;

        var ok = new Button { Text = Strings.Settings_OK, DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = Strings.Settings_Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
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

        // 言語によって文言の行数が変わるので、高さは内容に合わせて決める
        PerformLayout();
        var gridHeight = grid.GetPreferredSize(new Size(ClientSize.Width, 0)).Height;
        ClientSize = new Size(ClientSize.Width, gridHeight + buttons.Height);
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

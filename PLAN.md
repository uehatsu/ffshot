# ffshot 実装計画

常駐型スクリーンショットツール（Windows / C#）。

## 要件（確定）

1. 起動時にタスクトレイに常駐する
2. ショートカットキーを設定できる
3. ショートカット入力で PNG 形式でスクリーンショットを保存
4. 通常（GDI）に加えて DirectX(Direct3D) 経由でも取得できる
5. 「アクティブウィンドウ」「全画面」を切り替え可能（それぞれ別ホットキー割当可）

## 技術選定

| 項目 | 選定 | 理由 |
|---|---|---|
| ランタイム | .NET 10 (`net10.0-windows10.0.19041.0`) | Windows 側に SDK 10.0.101 が導入済み |
| UI | WinForms | `NotifyIcon` でトレイ常駐が最短。設定画面 1 枚なら十分 |
| Win32 呼び出し | `Microsoft.Windows.CsWin32`（ソースジェネレータ） | 手書き P/Invoke の誤りを避ける |
| グローバルホットキー | `RegisterHotKey` + 非表示 `NativeWindow` で `WM_HOTKEY` 受信 | 標準的で依存なし |
| 通常キャプチャ | GDI (`Graphics.CopyFromScreen` / BitBlt) | 互換性重視のフォールバック |
| Direct3D キャプチャ | DXGI Desktop Duplication (`IDXGIOutputDuplication`) via `Vortice.Direct3D11` / `Vortice.DXGI` | Direct3D 11 デバイスで画面をテクスチャとして取得。GDI で黒くなる DirectX 全画面（ボーダーレス）アプリも取れる。SharpDX は開発終了のため Vortice を使う |
| PNG 出力 | `System.Drawing.Bitmap.Save(ImageFormat.Png)` | Windows 限定アプリなので `System.Drawing.Common` で足りる |
| 設定保存 | `%APPDATA%\ffshot\settings.json`（System.Text.Json） | |
| DPI | app.manifest で PerMonitorV2 | 座標ずれ防止に必須 |
| 多重起動防止 | 名前付き `Mutex` | |

### Direct3D 方式の補足

- **Desktop Duplication** を採用。全画面はモニター出力そのもの、アクティブウィンドウは同じフレームを DWM のウィンドウ矩形（`DWMWA_EXTENDED_FRAME_BOUNDS`）で切り出す。
- 代替案 **Windows.Graphics.Capture**（WinRT, D3D11 ベース）は「他ウィンドウに隠れていても対象ウィンドウだけ取れる」利点があるが、WinRT 相互運用が増える。まず Desktop Duplication で実装し、必要なら Phase 5 で追加する。
- Desktop Duplication は RDP セッション等で失敗することがあるため、失敗時は GDI にフォールバックし通知する。

## アーキテクチャ

```
ffshot.sln
├─ src/FFShot/                     net10.0-windows, WinForms
│   ├─ Program.cs                  Mutex, ApplicationContext 起動
│   ├─ App/TrayApplicationContext.cs   NotifyIcon, メニュー, 各部品の配線
│   ├─ Hotkeys/
│   │   ├─ HotkeyBinding.cs        Modifiers + Key, 文字列 <-> 構造体
│   │   └─ HotkeyManager.cs        RegisterHotKey / WM_HOTKEY -> イベント
│   ├─ Capture/
│   │   ├─ CaptureTarget.cs        enum { FullScreen, ActiveWindow }
│   │   ├─ ICaptureBackend.cs      Bitmap Capture(CaptureTarget)
│   │   ├─ GdiCaptureBackend.cs
│   │   ├─ DesktopDuplicationBackend.cs   D3D11 + DXGI
│   │   └─ WindowInfo.cs           前面ウィンドウ矩形, 所属モニター取得
│   ├─ Output/
│   │   ├─ FileNamer.cs            "ffshot_yyyyMMdd_HHmmss.png", 重複回避
│   │   └─ PngWriter.cs
│   ├─ Settings/
│   │   ├─ AppSettings.cs          下記スキーマ
│   │   └─ SettingsStore.cs        読込 / 保存 / 既定値
│   ├─ UI/
│   │   ├─ SettingsForm.cs
│   │   └─ HotkeyTextBox.cs        KeyDown を捕まえてバインディング表示
│   └─ app.manifest                PerMonitorV2 DPI
└─ tests/FFShot.Tests/             xunit: FileNamer, HotkeyBinding, SettingsStore
```

### 設定スキーマ（settings.json）

```json
{
  "hotkeyFullScreen":   "Ctrl+Shift+F12",
  "hotkeyActiveWindow": "Ctrl+Shift+F11",
  "backend":            "Gdi",            // "Gdi" | "Direct3D"
  "saveFolder":         "%USERPROFILE%\\Pictures\\ffshot",
  "fileNamePattern":    "ffshot_{yyyyMMdd_HHmmss}",
  "includeCursor":      false,
  "showNotification":   true,
  "runAtStartup":       false
}
```

### トレイメニュー

全画面を撮影 / アクティブウィンドウを撮影 / 設定… / 保存先を開く / 終了

### 撮影フロー

1. `WM_HOTKEY` 受信 → どちらのターゲットか判定
2. `ICaptureBackend.Capture(target)` を実行（設定のバックエンド、失敗時 GDI にフォールバック）
3. `FileNamer` でパス生成 → PNG 保存
4. バルーン通知（保存先パス）。失敗時はエラー通知

## 実装フェーズ

| Phase | 内容 | 完了条件 |
|---|---|---|
| 1 | ソリューション作成、トレイ常駐、多重起動防止、設定の読み書き、終了メニュー | 起動するとトレイに出て終了できる |
| 2 | `HotkeyManager`、設定画面でホットキー変更、登録失敗時の通知 | 設定したキーでイベントが飛ぶ |
| 3 | GDI バックエンド（全画面・アクティブウィンドウ）、PNG 保存、通知 | 両ホットキーで PNG が保存される |
| 4 | Desktop Duplication バックエンド、設定でバックエンド切替、GDI フォールバック | DirectX 全画面アプリが黒くならず撮れる |
| 5 | 仕上げ：Windows 起動時登録（Run キー）、カーソル合成、複数モニター、テスト、README | |

## 決めておきたい点（既定値で進めます）

- **全画面の範囲**: 既定は「アクティブウィンドウが乗っているモニター 1 枚」。全モニター結合は Phase 5 のオプション。
- **「起動時に常駐」**: アプリ起動＝トレイ常駐と解釈。Windows ログオン時の自動起動は設定項目（既定 OFF）。
- **UI**: WinForms。WPF/WinUI3 が良ければ Phase 1 前に変更。
- **D3D 方式**: Desktop Duplication。隠れたウィンドウの単体取得が必要なら Windows.Graphics.Capture を追加。

## ビルド・検証

- WSL からは `"/mnt/c/Program Files/dotnet/dotnet.exe" build` で Windows 側 SDK を使う（WSL に dotnet なし）
- GUI 動作確認は Windows 側で手動。非 GUI 部分（ファイル名、ホットキー文字列、設定）は xunit で自動化

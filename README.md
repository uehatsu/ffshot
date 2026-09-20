# ffshot

タスクトレイに常駐し、ホットキーで PNG スクリーンショットを保存する Windows アプリです。

- 全画面 / アクティブウィンドウをそれぞれ別のホットキーで撮影
- 取得方式を「通常 (GDI)」と「DirectX (Direct3D / DXGI Desktop Duplication)」から選択
  - GDI で真っ黒になる DirectX 全画面（ボーダーレス）アプリは Direct3D 方式で撮影できます
  - Direct3D 方式が使えない環境（リモートデスクトップ等）では自動的に GDI にフォールバックします
- 設定は `%APPDATA%\ffshot\settings.json` に保存

## 動作環境

- Windows 10 1809 以降 / Windows 11
- .NET 10 ランタイム（ビルドには .NET 10 SDK）

## ビルド・実行

```powershell
dotnet build ffshot.sln
dotnet run --project src/FFShot
dotnet test ffshot.sln
```

配布用に単一 exe を作る場合:

```powershell
dotnet publish src/FFShot -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

WSL から Windows 側の SDK を使う場合は `"/mnt/c/Program Files/dotnet/dotnet.exe"` を呼びます。

## 使い方

起動するとトレイにカメラアイコンが出ます。ダブルクリックまたは右クリック → 「設定...」で以下を変更できます。

| 項目 | 既定値 |
|---|---|
| 全画面のホットキー | `Ctrl+Shift+F12` |
| アクティブウィンドウのホットキー | `Ctrl+Shift+F11` |
| 取得方式 | 通常 (GDI) |
| 保存先フォルダ | `%USERPROFILE%\Pictures\ffshot` |
| ファイル名パターン | `ffshot_{yyyyMMdd_HHmmss}` |
| 全画面撮影で全モニターを結合する | オフ（アクティブウィンドウのあるモニターのみ） |
| マウスカーソルを含める | オフ |
| 保存時に通知を表示する | オン |
| Windows ログオン時に自動起動する | オフ |

ファイル名パターンの `{ }` 内には .NET の日時書式を書けます。`{target}` は `full` / `window` に展開されます。同名ファイルがある場合は `_1`, `_2` … が付きます。

ホットキーの入力欄では、押したキーの組み合わせがそのまま設定されます。Backspace で解除できます。他アプリと競合して登録できなかった場合はトレイ通知でお知らせします。

## 構成

```
src/FFShot/
  App/        TrayApplicationContext（トレイ・配線）, CaptureService, StartupRegistration
  Hotkeys/    HotkeyBinding（文字列変換）, HotkeyManager（RegisterHotKey）
  Capture/    ICaptureBackend, GdiCaptureBackend, DesktopDuplicationBackend, WindowInfo, CursorOverlay
  Output/     FileNamer, PngWriter
  Settings/   AppSettings, SettingsStore
  UI/         SettingsForm, HotkeyTextBox
tests/FFShot.Tests/   xunit（画面を実際に撮るテストを含むため Windows 上で実行）
```

## 既知の制限

- Direct3D 方式は HDR 有効時（R16G16B16A16_FLOAT）や回転したディスプレイに未対応です。その場合は GDI にフォールバックします。
- 隠れているウィンドウの中身は撮れません（画面に見えている内容を切り出します）。

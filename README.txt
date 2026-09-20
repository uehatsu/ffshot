First Fox Screenshot (ffshot)
=============================

タスクトレイに常駐し、ホットキーで PNG スクリーンショットを保存する Windows アプリです。
A Windows app that lives in the system tray and saves PNG screenshots when you press a hotkey.

詳細はこちら / Full documentation:
  https://github.com/uehatsu/ffshot


[日本語]

使い方
  1. ffshot.exe をダブルクリックして起動します。タスクトレイにキツネのアイコンが出ます。
     初回は SmartScreen の警告が出ることがあります。「詳細情報」→「実行」で起動できます。
  2. Ctrl+Shift+F12 で全画面、Ctrl+Shift+F11 でアクティブウィンドウを撮影します。
     保存先は「ピクチャ\ffshot」です。
  3. トレイアイコンをクリックするとメニュー、ダブルクリックすると設定画面が開きます。
     ホットキー、取得方式（通常 / DirectX）、保存先、言語などを変更できます。

ヒント
  - DirectX の全画面（ボーダーレス）ゲームが真っ黒に写る場合は、取得方式を DirectX にしてください。
  - 管理者権限で動くアプリ（PlayOnline Viewer など）を撮る場合は、
    トレイメニューの「管理者として再起動」を使ってください。
  - 設定は %APPDATA%\ffshot\settings.json に保存されます。
  - アンインストールは、設定で自動起動をオフにし、トレイの「終了」で止めてからフォルダを削除するだけです。


[English]

Usage
  1. Double-click ffshot.exe. A fox icon appears in the system tray.
     SmartScreen may warn the first time; click "More info" and then "Run anyway".
  2. Press Ctrl+Shift+F12 to capture the full screen, or Ctrl+Shift+F11 for the active window.
     Files are saved to Pictures\ffshot.
  3. Click the tray icon for the menu, or double-click it for the settings dialog.
     You can change the hotkeys, capture method (Normal / DirectX), save folder, language and more.

Tips
  - If a DirectX full-screen (borderless) game comes out black, switch the capture method to DirectX.
  - To capture apps that run as administrator (e.g. PlayOnline Viewer), use
    "Restart as administrator" in the tray menu.
  - Settings are stored in %APPDATA%\ffshot\settings.json.
  - To uninstall, turn off autostart in the settings, choose "Exit" in the tray menu, and delete the folder.


Copyright (c) 2026 Hatsuhito UENO

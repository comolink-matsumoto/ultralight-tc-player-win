# UltraLight TC Player for Windows

個人利用向けの軽量タイムコード確認・IN / OUT取得ツールです。

H.264のmp4動画を主対象に、動画を見ながら現在位置の `hh:mm:ss:ff` を確認し、IN / OUTからffmpeg切り出しコマンドをコピーすることを目的にしています。

タイムコードは `現在位置 / フル尺` の形式で表示します。

```text
TC 00:01:23:12 / 00:10:30:00
```

## 開発環境

- Windows 11
- .NET SDK 10.0.300 以上
- Visual Studio Community 2022 または `dotnet` CLI
- ffmpeg 7.1.1 以上

## ビルド

```powershell
dotnet build
```

## 実行

```powershell
dotnet run
```

## 公開用ビルド

Windows 11 x64向けの自己完結型・単一EXEを生成します。.NETランタイムは同梱し、WPFの互換性を優先してトリミングは行いません。

```powershell
dotnet publish -c Release
```

出力先:

```text
bin\Release\net10.0-windows\win-x64\publish\
```

公開版はコード署名を行わないため、ダウンロード時や初回起動時にWindows SmartScreenの警告が表示される場合があります。配布元とSHA-256チェックサムを確認したうえで実行してください。

## MVP操作

- `Open`: 動画ファイルを開く
- ドラッグ＆ドロップ: 動画ファイルを開く
- `Space`: 再生 / 一時停止
- タイムラインをクリック: クリックした位置へシーク
- タイムラインのつまみをドラッグ: 任意の位置へシーク
- `Right Arrow`: 目安の1フレーム送り
- `Left Arrow`: 目安の1フレーム戻し
- `I`: 現在位置をINに設定
- `O`: 現在位置をOUTに設定
- `Esc`: 選択中のINまたはOUTをクリア
- `Delete`: IN / OUTを両方クリア
- `Clear In` / `Clear Out` / `Clear All`: IN / OUTをクリア
- `Copy`: IN / OUTが揃った時にffmpegコマンドをコピー

## UI

- 現在位置とフル尺を `hh:mm:ss:ff / hh:mm:ss:ff` 形式で表示します。
- Play / PauseボタンとMute / Mutedボタンは、表示切り替え時もサイズが変わりません。
- タイムコード・FPSとIN / OUT操作を段分けし、狭いウィンドウ幅でも表示が重なりにくい構成にしています。

## ffmpegコマンド

IN / OUTが揃うと、以下の形式でコマンドを生成します。

```powershell
ffmpeg -ss 00:00:10.000 -to 00:00:25.000 -i "C:\Videos\sample.mp4" -c copy "C:\Videos\sample-cut.mp4"
```

出力ファイル名は、再生中のソースファイル名から `source-cut.ext` として生成します。

## 既知の制限

- FPSはWindowsのファイルプロパティから取得を試み、取得できない場合は30fpsとして扱います。
- タイムコードはプレイヤー位置とFPSから算出する確認用表示です。
- 29.97 / 59.94 fpsのドロップフレーム表記には未対応です。
- VFR動画では表示やフレーム移動に誤差が出る可能性があります。
- 左右キーのフレーム移動は `1 / fps` 秒ぶんシークする目安操作です。
- `-c copy` の切り出しは高速・無劣化ですが、キーフレーム位置の都合で切り出し位置が表示位置と少しずれる可能性があります。
- MVPではアプリ内でffmpegは実行せず、コマンドのコピーまで行います。

## ライセンス

Copyright (c) 2026 k8gma2mo10

このプロジェクトは[Apache License 2.0](LICENSE)で公開しています。

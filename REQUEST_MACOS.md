# macOS版 UltraLight TC Player 開発依頼書

## 目的

Windows版として作成した軽量動画プレーヤーを参考に、macOS版の **UltraLight TC Player** を作成したいです。

このアプリは一般配布や多機能プレーヤーを目的としたものではなく、個人利用向けの **軽量な動画確認・IN / OUT取得ツール** です。

主目的は、動画を見ながらIN / OUT位置の `hh:mm:ss:ff` を取得し、ffmpegで切り出すためのコマンドをコピーすることです。

## 前提

- 対象OSはmacOSです。
- 個人利用目的です。
- App Store配布は考えていません。
- 有料ライブラリは使わない方針です。
- 主な再生動画はH.264のmp4です。
- H.265 / HEVC再生はマストではありません。
- UIの完成度より、まずは軽く動いて実用できることを優先します。

## Windows版で実装済みの機能

Windows版では、C# + WPFで以下を実装済みです。

- 動画ファイルを開く
- ドラッグ＆ドロップで動画を開く
- 動画を自動再生する
- `Space` で再生 / 一時停止
- `Right Arrow` で目安の1フレーム送り
- `Left Arrow` で目安の1フレーム戻し
- 現在位置を `hh:mm:ss:ff` で常時表示
- FPSを取得し、タイムコード計算に反映
- FPS取得に失敗した場合は30fps扱い
- `I` で現在位置をINに設定
- `O` で現在位置をOUTに設定
- `Esc` で選択中のINまたはOUTをクリア
- `Delete` でIN / OUTを両方クリア
- `Clear In` / `Clear Out` / `Clear All` ボタン
- シークバー
- 音量スライダー
- ミュートボタン
- IN / OUTが揃った時だけffmpegコマンドを生成
- ffmpegコマンドをコピー
- 出力ファイル名は `source-cut.ext`
- `Command` 切替で `Cut` / `GIF` のコマンド生成を切り替え
- GIF FPS入力に対応。整数 `1` から `30`、初期値は `12`
- GIF横サイズプルダウンに対応。`320 / 480 / 640 / 960 / 1280 / 1920`、初期値は `640`
- GIF最大色数入力に対応。整数 `4` から `256`、初期値は `216`
- IN / OUTが揃った時だけGIF生成用ffmpegコマンドを生成
- GIF出力ファイル名は `source_{colors}.gif`

## macOS版で目指したいMVP

macOS版でも、まずは以下をMVPとしてください。

- macOSでアプリが起動する
- H.264のmp4動画を開ける
- ドラッグ＆ドロップで動画を開ける
- 動画を再生できる
- `Space` で再生 / 一時停止できる
- `Right Arrow` で目安の1フレーム送りができる
- `Left Arrow` で目安の1フレーム戻しができる
- 現在位置を `hh:mm:ss:ff` で常時表示できる
- FPSを自動取得し、タイムコード計算とフレーム移動に反映できる
- `I` でINを設定できる
- `O` でOUTを設定できる
- `Esc` で選択中のINまたはOUTをクリアできる
- `Delete` でIN / OUTを両方クリアできる
- `Clear In` / `Clear Out` / `Clear All` ボタンがある
- シークバーがある
- 音量スライダーがある
- ミュートボタンがある
- IN / OUTが揃った時だけffmpegコマンドを生成できる
- ffmpegコマンドをコピーできる
- 出力ファイル名を再生中のソースファイル名から `source-cut.ext` として動的に生成できる

上記MVPとは別に、Windows版で実装済みのGIF生成用ffmpegコマンド仕様もmacOS版の対象に含めてください。

## 推奨技術

macOS版では、Windows版の制約は引き継がなくて構いません。

以下を優先候補として検討してください。

- Swift
- SwiftUI
- AVFoundation
- AVKit

UI実装の都合で必要であれば、AppKitを部分的に使っても構いません。

ただし、初期版では過剰なアーキテクチャや複雑な設定画面は避けてください。

## 避けたい実装

- Electron
- WebView中心の動画プレーヤー
- 重い常駐型アプリ
- プレイリスト機能
- メディアライブラリ
- 動画変換機能
- 字幕編集
- 波形表示
- カラー調整
- 複数動画比較
- クラウド同期
- ネットワーク再生
- アプリ内でのffmpeg実行

## タイムコード仕様

- 表示形式は `hh:mm:ss:ff`
- FPSは動画ファイルから取得してください
- 想定FPS:
  - 23.976
  - 24
  - 25
  - 29.97
  - 30
  - 59.94
  - 60
- 29.97 / 59.94 fpsのドロップフレーム表記は初期版では不要です
- VFR動画は初期版では厳密対応不要です
- タイムコードは放送用途の厳密なTCではなく、プレイヤー位置とFPSから算出する確認用表示で構いません

計算方針:

```text
totalFrames = round(currentSeconds * fps)
frameBase = round(fps)

hour = totalFrames / (frameBase * 60 * 60)
minute = (totalFrames % (frameBase * 60 * 60)) / (frameBase * 60)
second = (totalFrames % (frameBase * 60)) / frameBase
frame = totalFrames % frameBase
```

## フレーム送り / 戻し仕様

- 厳密なデコードフレーム単位でなくて構いません
- `1 / fps` 秒ぶんシークする目安操作で構いません
- `Right Arrow`: 現在位置 + `1 / fps`
- `Left Arrow`: 現在位置 - `1 / fps`
- フレーム送り / 戻し時は一時停止して構いません
- ただし、画面表示はコマ送りのように更新されることを重視します

## IN / OUT仕様

- `I`: 現在位置をINに設定
- `O`: 現在位置をOUTに設定
- `I` または `O` で設定した側を選択中として扱う
- `Esc`: 選択中のINまたはOUTだけをクリア
- `Delete`: IN / OUTを両方クリア
- `Clear In`: INだけクリア
- `Clear Out`: OUTだけクリア
- `Clear All`: IN / OUTを両方クリア

## ffmpegコマンド仕様

アプリ内でffmpegを実行する必要はありません。

IN / OUTが揃った時だけ、以下のようなコマンドを生成してコピーできるようにしてください。

```bash
ffmpeg -ss 00:00:10.000 -to 00:00:25.000 -i "/Users/me/Videos/sample.mp4" -c copy "/Users/me/Videos/sample-cut.mp4"
```

仕様:

- 入力ファイルは現在再生中のソースファイルのフルパスを使う
- 出力ファイルは同じフォルダに作る
- 出力ファイル名は `source-cut.ext`
- 例:
  - `sample.mp4` -> `sample-cut.mp4`
  - `sample.mov` -> `sample-cut.mov`
- ffmpeg用の時間は `hh:mm:ss.fff`
- 画面表示用の時間は `hh:mm:ss:ff`
- `-c copy` は高速・無劣化だが、キーフレーム位置の都合で切り出し位置が表示位置と少しずれる可能性がある
- この制限はREADMEに明記する

## GIF生成コマンド仕様

アプリ内でGIF生成を実行する必要はありません。

IN / OUTが揃った時だけ、通常の切り出しとは別に、以下のようなGIF生成用ffmpegコマンドを生成してコピーできるようにしてください。

```bash
ffmpeg -ss 00:00:10.000 -to 00:00:25.000 -i "/Users/me/Videos/sample.mp4" -filter_complex "[0:v]fps=12,scale=640:-2:flags=lanczos,split[a][b];[a]palettegen=max_colors=216:reserve_transparent=0[p];[b][p]paletteuse" -an -loop 0 "/Users/me/Videos/sample_216.gif"
```

仕様:

- 入力ファイルは現在再生中のソースファイルのフルパスを使う
- 出力ファイルは同じフォルダに作る
- 出力ファイル名は、元動画名に最大色数を付けて `.gif` とする
- `-cut` は付けない
- 例:
  - `sample.mp4`、最大色数216 -> `sample_216.gif`
  - `sample.mov`、最大色数128 -> `sample_128.gif`
- `-n` は付けない
- `-y` も付けない
- 同名ファイルが既にある場合の上書き確認は、ffmpeg実行時のプロンプト側に任せる
- GIF FPSは整数 `1` から `30` まで指定可能にする
- GIF FPSの初期値は `12`
- GIF横サイズはプルダウンで指定可能にする
- GIF横サイズの初期値は `640`
- GIF横サイズの選択肢は `320 / 480 / 640 / 960 / 1280 / 1920`
- GIF最大色数は整数 `4` から `256` まで指定可能にする
- GIF最大色数の初期値は `216`
- 横動画・縦動画のどちらも、横幅を選択値に固定する
- 高さはアスペクト比を維持して自動計算し、余白や黒帯は追加しない
- scale指定は `scale={width}:-2:flags=lanczos` を使う
- palettegenには `max_colors={colors}:reserve_transparent=0` を指定する
- GIF生成は再エンコードのため、FPS、横サイズ、最大色数の指定によって画質とファイルサイズが変わる

## UI方針

- 映像表示領域を主役にする
- コントロールは最小限にする
- タイムコードは視認性を優先する
- タイムコードには等幅数字または等幅フォントを使う
- UIはmacOS標準アプリらしく自然で軽い見た目にする
- QuickTime Playerの完全模倣は不要
- ただし、「体験の軽さ」は重視する

表示したい情報:

- 現在タイムコード
- FPS
- INタイムコード
- OUTタイムコード
- ffmpegコマンド欄
- Copyボタン
- GIF FPS入力
- GIF横サイズプルダウン
- GIF最大色数入力

## READMEに書いてほしいこと

- ビルド手順
- 実行手順
- 操作方法
- ffmpegコマンド生成仕様
- GIF生成コマンド仕様
- 既知の制限事項
- 今後の改善候補

## 既知の制限として扱ってよいこと

- タイムコードは確認用表示であり、放送用途の厳密TCではない
- ドロップフレーム表記には未対応
- VFR動画では誤差が出る可能性がある
- フレーム送り / 戻しは `1 / fps` 秒シークの目安操作
- `-c copy` 切り出しはキーフレーム位置の影響で切り出し位置がずれる可能性がある
- GIF生成は再エンコードのため、FPS、横サイズ、最大色数の指定によって画質とファイルサイズが変わる

## 作業の進め方

1. macOS向けのプロジェクト構成を確認する
2. SwiftUI + AVFoundation / AVKitでMVPを組めるか判断する
3. 必要ならAppKit併用を検討する
4. まず動画を開いて再生できる状態を作る
5. タイムコード表示を追加する
6. キーボード操作を追加する
7. IN / OUT取得を追加する
8. ffmpegコマンド生成とコピーを追加する
9. GIF生成コマンド生成とコピーを追加する
10. READMEに実行方法と制限事項を整理する

## 判断基準

優先順位は以下です。

1. 軽量性
2. H.264 mp4再生の安定性
3. IN / OUT取得とffmpegコマンド生成の実用性
4. タイムコード表示の視認性
5. フレーム送り / 戻しの操作感
6. UIの美しさ

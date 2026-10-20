# RE:BOUND

[English](README.md), [Korean](README.ko.md), [Japanese](README.ja.md)

下降してチャージし、上昇して攻撃する重力シューティングと軌道制作ワークショップです。

| プロジェクト情報 | 内容 |
|---|---|
| 開発状況 | プレイ可能なプロトタイプ |
| 使用技術 | C#, Unity 2D, Local content |

[開発事例を見る](https://ysl-ryudo.github.io/ja/work/rebound/), [Portfolio](https://ysl-ryudo.github.io/ja/)

## ゲーム全体で担当した実装

軌道式ツールに加え、飛行・戦闘・成長・ラン状態・保存・コンテンツワークショップまで開発しました。式パーサーはゲームの一部であり、プロジェクト全体ではありません。

**プレイの流れ**

ラン開始 → 飛行と反動 → ウェーブ・ボス戦 → 成長選択 → クリア・失敗 → 保存とコンテンツ制作

ソースリンクは確認した版に固定し、担当リンクは自分のマージ以外の変更を示します。共同ファイルは単独著作を意味しません。非公開ソースには閲覧権限が必要です。

### 飛行と反動戦闘

飛行と戦闘ワールドの反動操作を実装しました。反動攻撃は敵・敵弾に作用し、連鎖では命中済み対象を記録します。CombatWorldが敵・弾・ボス段階を管理します。

[PlayerFlight.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/PlayerFlight.cs), [担当変更 1ffa420](https://github.com/YSL-RyuDo/rebound/commit/1ffa420944e866e69b4422903bf1095ef0855494), [CombatWorld.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/CombatWorld.cs), [担当変更 6fbc48d](https://github.com/YSL-RyuDo/rebound/commit/6fbc48dfb088b5d7a6fef3d45694d976b95d3721)

### 成長選択と能力値再計算

経験値閾値、保留レベルアップ、重み付き強化候補を実装しました。最大段階・効果なしの候補を除き、有効な強化が尽きた場合の代替報酬を用意します。能力値はUI側で加算を重ねず、基礎値と強化段階から再計算します。

[Progression.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/Progression.cs), [担当変更 1ffa420](https://github.com/YSL-RyuDo/rebound/commit/1ffa420944e866e69b4422903bf1095ef0855494)

### ラン状態と保存境界

メニュー・物語・プレイ・一時停止・レベルアップ・結果・ワークショップの状態を実装しました。停止系状態はゲーム時間を制御します。テストランは最高点やクリア記録を更新せず、試行と通常進行を分けます。

[GameSession.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/GameSession.cs), [担当変更 79d77f2](https://github.com/YSL-RyuDo/rebound/commit/79d77f2e3996f7e5249117a453d76027073f8af1), [SaveManager.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/SaveManager.cs), [担当変更 cfc789a](https://github.com/YSL-RyuDo/rebound/commit/cfc789aea5dbd9e2e34847cfed4c3dbb2bb0ff53)

### コンテンツと軌道の制作

データ駆動ゲームとともに、ワークショップの編集・プレビュー・入出力を実装しました。軌道言語で動きを制限した数式として記述できます。固定パターンや任意スクリプトとの違いは後述します。

[ModWorkshop.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/ModWorkshop.cs), [担当変更 6fbc48d](https://github.com/YSL-RyuDo/rebound/commit/6fbc48dfb088b5d7a6fef3d45694d976b95d3721), [WorkshopProject.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/WorkshopProject.cs), [担当変更 6fbc48d](https://github.com/YSL-RyuDo/rebound/commit/6fbc48dfb088b5d7a6fef3d45694d976b95d3721)

## 担当した内容

落下による充電・上昇攻撃、戦闘ウェーブ、強化選択、ボス進行、設定保存を実装しました。

ローカルZIP MODの検証と、弾道・敵・ウェーブ・強化の制作ツールを実装しました。

パラメーター・数式による弾道編集と、ゲーム・制作ツール共通の弾道評価を追加しました。

## 貢献の根拠

[v1.2 source baseline](https://github.com/YSL-RyuDo/rebound/tree/6fbc48dfb088b5d7a6fef3d45694d976b95d3721)

コミットは記載した変更の根拠であり、貢献率の点数ではありません。外部素材・チームメンバーの貢献は区別します。

## 実装と設計

重力シューティングと制作ツールで、作成した軌道を同じように解釈する必要があります。ローカルコンテンツはゲームへの反映前に検証します。

落下で充電・上昇で攻撃するループと、軌道・敵・ウェーブ・強化の制作ツールを実装しました。パラメーター・数式軌道の評価器をゲームとツールで共有し、ローカルZIPを検証します。

## コードの動作と実装構造の理由

確認した実装が扱う制約と利点・代償を説明します。設計効果の説明はコード分析であり、比較する全代案を当時試作したという意味ではありません。担当リンクは共同制作内の変更を示し、ソースの閲覧権限は維持します。

### ユーザー軌道に制限付きの式言語を使う理由

**実装の流れ**

TrajectoryFormulaは許可した変数、演算子、関数の式を構文木へ解析します。文字数、ノード数、深さを制限し、解析結果をキャッシュして非有限値や範囲外の座標を拒否します。検証は寿命区間をサンプリングし、実行時にも戻り値を確認します。

**この構造が必要な理由**

軌道に必要なのは座標計算であり、任意のC#実行ではありません。小さい言語なら対応動作を明示し、解析・評価構造に制限を設けられます。キャッシュは毎更新の再解析を減らし、実行時検査は有限回のサンプリングが見落とす値を扱います。

**代案と現在の実装の折合い**

固定軌道の一覧は検証しやすい反面、表現が限られます。任意スクリプトは自由ですが実行・検証範囲が大幅に増えます。現在の構文木はその中間で、小さい言語内の数式を組み合わせます。

**利点・代償と検証範囲**

汎用性を抑える代わりに実装・検証対象を狭める選択です。関数追加にはパーサーと評価器の変更が必要です。サンプリングは全時刻の連続性や正しさの証明ではなく、利用する式が大幅に増える場合はキャッシュ方針も再検討します。

[rebound/TrajectoryFormula.cs](https://github.com/YSL-RyuDo/rebound/blob/52994738ab20e2c209c5d359b96527cc268417f2/Assets/Rebound/Full/TrajectoryFormula.cs), [rebound/TrajectoryFormulaTests.cs](https://github.com/YSL-RyuDo/rebound/blob/52994738ab20e2c209c5d359b96527cc268417f2/Assets/Rebound/Tests/EditMode/TrajectoryFormulaTests.cs)

### 強化選択が実際に効果を持つ条件

**実装の流れ**

Progressionはレベル・最大段階・能力差分で候補を絞り、重みで最大3件を選び、不足分を得点報酬にします。世代番号で古い候補への入力を拒否でき、保留レベルアップを一つずつ消費します。

**この構造が必要な理由**

段階上限前でも最終能力がクランプされれば無効です。再計算値の比較で実際の効果を確認できます。複数レベルアップや遅いUI入力には、どの候補集合へのクリックかを区別する規則も必要です。

**代案と現在の実装の折合い**

段階だけの確認は軽い一方、能力上限を見逃します。差分加算は再計算を減らしますが順序や重複加算の問題が生じます。基礎データと段階からの再計算は追いやすい代わりにプレビュー計算が増えます。

**利点・代償と検証範囲**

世代検査は呼出側が番号を渡す場合に有効です。全強化上限、複数保留、古いクリック、選択前の無効化が検証点です。得点への代替は確率バランスの証明ではありません。

[rebound/Progression.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/Progression.cs)

### 制作テストと通常進行を区別する

**実装の流れ**

GameSessionは制作内容の実行にテストモードを使います。FinishRunはその場合最高点・クリア記録を更新せず、通常ランで保存します。ワークショップへ入る前に進行中ランを解消します。

**この構造が必要な理由**

制作中は意図的に簡単・極端・未完成な内容を試します。その結果を通常実績へ保存すると編集実験とプレイヤー進行が混ざります。

**代案と現在の実装の折合い**

別実行ファイルは広く隔離できますが反復が遅くなります。別テスト保存にも寿命管理が必要です。現在は同一ランタイムのモード検査で進行保存を除外し、軽量な代わりに全保存経路の確認が重要です。

**利点・代償と検証範囲**

これは進行データの境界で、OSのサンドボックスではありません。テスト成功・失敗の前後と通常プレイ復帰後の保存比較が必要です。

[rebound/GameSession.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/GameSession.cs), [rebound/SaveManager.cs (master)](https://github.com/YSL-RyuDo/rebound/blob/30e289a89f8e24fae16bfc7fcdcb15ae965c7901/Assets/Rebound/Full/SaveManager.cs)

## 実行・コードの確認

[ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)記載のUnity版で開いてください。パッケージロックとシリアライズ済み素材を維持し、Unityを開く前に対象ブランチを選んでください。

Unity scene: [Assets/Rebound/Scenes/REBOUND.unity](Assets/Rebound/Scenes/REBOUND.unity)

## 開発・検証範囲

2026 ~ 10 ~ 19の記録ではEditMode 22件・PlayMode 23件成功とWindowsビルドを確認しています。本README作業での再実行結果ではありません。遠隔MODサービスは未接続で、長時間の手動バランス・アクセシビリティ検証や極端な弾道の連続衝突は検証範囲外です。

## 詳細資料

[既存の詳細開発・利用・検証資料（韓国語）](docs/DEVELOPMENT_REFERENCE.ko.md)

[VALIDATION.md](VALIDATION.md)

[Docs/WORKSHOP_GUIDE.md](Docs/WORKSHOP_GUIDE.md)

[Docs/TRAJECTORY_VERSIONS.md](Docs/TRAJECTORY_VERSIONS.md)

[Docs/RESOURCES.md](Docs/RESOURCES.md)

## 関連リポジトリ

[rebound](https://github.com/YSL-RyuDo/rebound), 非公開・アクセス権が必要

# Smash Prototype

大乱闘スマッシュブラザーズにインスパイアされた、オンライン対戦対応の2Dプラットフォームファイティングゲームです。  
Unity 6 と Photon Fusion 2 を用いて、ネットワーク対戦機能を一から実装しました。

---

## 目次

- [プレイ動画 / スクリーンショット](#プレイ動画--スクリーンショット)
- [ゲーム概要](#ゲーム概要)
- [操作方法](#操作方法)
- [実装機能](#実装機能)
- [技術スタック](#技術スタック)
- [アーキテクチャ](#アーキテクチャ)
- [工夫した点](#工夫した点)
- [ゲームの流れ](#ゲームの流れ)
- [セットアップ](#セットアップ)

---

## プレイ動画 / スクリーンショット

### ローカル対戦
[![デモ動画のサムネイル](https://img.youtube.com/vi/DW82-Re9IUk/0.jpg)](https://www.youtube.com/watch?v=DW82-Re9IUk)
### 通信対戦
[![デモ動画のサムネイル](https://img.youtube.com/vi/動画ID/0.jpg)](https://www.youtube.com/watch?v=動画ID)
---

## ゲーム概要

| 項目 | 内容 |
|------|------|
| ジャンル | 2Dプラットフォームファイティング |
| プレイ人数 | 1vs1（ローカル対戦 / オンライン対戦） |
| 開発期間 | 現在も開発中 |
| エンジン | Unity 6000.3.11f1 |
| ネットワーク | Photon Fusion 2（Shared Mode） |

### ゲームシステム

- **ストックシステム** : 残機3でスタート。残機が0になると敗北
- **ダメージ蓄積** : ダメージが蓄積するほど吹き飛びが増加（スマブラ方式）
- **ノックバック計算** : `最終吹き飛び = 基礎吹き飛び × (蓄積ダメージ / 10 + 1)`
- **ブラストゾーン** : ステージ外周の見えない壁。超えると撃墜判定

---

## 操作方法

| アクション | キー |
|----------|------|
| 左右移動 | A / D |
| ジャンプ（2段ジャンプ対応） | Space |
| 横攻撃 | J（＋方向キー） |
| 上攻撃 | ↑ ＋ J |
| 必殺技（横） | K（＋方向キー） |
| 必殺技（上）・復帰 | ↑ ＋ K |
| ガード | L |

---

## 実装機能

### プレイヤー挙動
- [x] 左右移動・2段ジャンプ
- [x] 方向入力による攻撃の出し分け（横・上・通常）
- [x] 必殺技（飛び道具・復帰技）
- [x] ガード（シールド縮小・回復・シールドブレイク）
- [x] しりもち落下（復帰技使用後の無防備状態）

### 対戦システム
- [x] ダメージ蓄積とノックバックスケーリング
- [x] ストック管理・リスポーン
- [x] 当たり判定（ヒットボックス / シールド判別）
- [x] シールドブレイク演出（3秒スタン）

### カメラ
- [x] 2プレイヤーの中間点追従
- [x] プレイヤー間距離に応じた動的ズーム
- [x] ステージ端での移動制限

### オンライン対戦（Photon Fusion 2）
- [x] ルーム作成・参加（ルーム名入力方式）
- [x] 待機画面でのREADY同期
- [x] 攻撃・ダメージ・ストックのネットワーク同期
- [x] 切断時の自動勝利判定
- [x] スプライト向き同期

### UI・シーン
- [x] リアルタイムダメージ表示（白→赤のカラーグラデーション）
- [x] ストックアイコン表示
- [x] キャラクターセレクト画面
- [x] リザルト画面（勝者表示）

---

## 技術スタック

| 分類 | 技術 |
|------|------|
| ゲームエンジン | Unity 6000.3.11f1 |
| プログラミング言語 | C# |
| ネットワーキング | Photon Fusion 2（Shared Mode） |
| レンダリング | Universal Render Pipeline（URP） |
| UI | TextMesh Pro |
| アニメーション補助 | DOTween |
| ローカルマルチ検証 | ParrelSync |
| バージョン管理 | Git / GitHub |

---

## アーキテクチャ

### スクリプト構成

```
Assets/Scripts/
├── Player/
│   ├── ActorController.cs       # 入力受付・移動・ジャンプ制御
│   ├── PlayerStatus.cs          # HP / ストック / シールド / スタン状態管理
│   └── HitArea.cs               # ヒットボックス衝突処理・攻撃権限チェック
├── Button/
│   ├── FighterCombat.cs         # 攻撃実行・アニメーション・ネットワーク同期
│   ├── GuardShield.cs           # シールドのスケール・見た目制御
│   └── KnockbackCalculator.cs   # ノックバック計算式
├── Manager/
│   ├── GameManager.cs           # ローカル対戦ゲームループ
│   ├── OnlineGameManager.cs     # オンライン対戦ゲームループ
│   ├── UIManager.cs             # HUD更新（ダメージ / ストック）
│   ├── WaitingRoomManager.cs    # 待機室のREADY同期
│   ├── CharacterSelectManager.cs
│   ├── ResultManager.cs
│   └── GameDataContainer.cs     # シーン跨ぎデータ保持（DontDestroyOnLoad）
├── Network/
│   ├── NetworkLauncher.cs       # Photon Fusionセッション管理
│   ├── NetworkPlayerRegistrar.cs  # プレイヤー生成時の登録処理
│   ├── NetworkSpriteFlipSync.cs   # スプライト向き同期
│   └── PlayerLobbyInfo.cs       # 待機室でのキャラ選択 / READY状態同期
├── DynamicCamera.cs             # 動的ズームカメラ
└── SceneController.cs           # シーン遷移ユーティリティ
```

### ゲームの流れ

```
タイトル画面
    └→ モード選択
         ├→ [トレーニング] → 対戦場面（ローカル2P） → リザルト
         └→ [オンライン]  → キャラ選択 → 待機室（READY同期）
                                └→ 対戦場面（Photon Fusion同期） → リザルト
```

---

## 工夫した点

### 1. オフライン・オンライン共通の設計

`PlayerStatus` や `FighterCombat` では `useNetworked` フラグを持たせ、同一スクリプトでローカルとオンラインの両方に対応しました。オフラインは直接メソッド呼び出し＋コルーチン、オンラインは Networked プロパティ＋RPC 呼び出しに切り替わります。

### 2. ネットワーク権限の明確化

攻撃ヒット判定は「攻撃した側（StateAuthority）」が処理し、ダメージ適用は「被弾した側（StateAuthority）」が行うよう責任を分離しました。これによりチート耐性と整合性を両立しています。

### 3. Rigidbody2D の使い分け

ローカルプレイヤーは `Dynamic`（物理シミュレーション）、リモートプレイヤーは `Kinematic`（NetworkTransform による位置同期）に設定し、物理演算の競合を回避しました。

### 4. スマブラ式ノックバック計算

シンプルな数式 `finalForce = baseKnockback × (damage / 10 + 1)` でダメージ蓄積に応じた吹き飛びスケーリングを実現しました。100% 時には基礎値の 11 倍の吹き飛びが発生します。

### 5. 動的ズームカメラ

2 プレイヤーの距離を毎フレーム計算し、正射影サイズ（Orthographic Size）を SmoothDamp で滑らかに変化させることで、離れると引き、近づくと寄るカメラ演出を実装しました。

---

## セットアップ

### 動作要件

- Unity 6000.3.11f1
- Photon Fusion 2 SDK（要 App ID 設定）

### 🎮 プレイ方法
以下のいずれかの方法でゲームを体験できます。

**A. エディタで実行する場合（開発者向け）**
1. リポジトリをクローン: `git clone https://github.com/nasu-cell/Smash-Prototype.git`
2. Unity Hub でプロジェクトを開く
3. [Photon Dashboard](https://dashboard.photonengine.com/) で App ID を取得し、`Assets/Photon/Fusion/Resources/NetworkProjectConfig` に設定
4. `TitleScene` をビルドに追加して実行

**B. ビルド済みファイルを実行する場合（プレイヤー向け）**
1. [こちら（Releasesページ）](https://github.com/nasu-cell/Smash-Prototype/releases) から最新のビルドファイルをダウンロード
2. **Smash-Prototype.zip** をダウンロードし、展開します。
3. 展開したフォルダ内の **.exeファイル** をダブルクリックして起動してください。
---

## 作者

- **GitHub** : [github.com/nasu-cell](https://github.com/nasu-cell)
- **メール** : shii101218@gmail.com

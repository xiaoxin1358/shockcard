# Shockcard Demo 开发文档

## 1. 项目定位

本项目是 Godot 4 + C# 的 2D 横版物理冲刺 Demo。

核心玩法闭环：

1. 玩家拖拽施力并消耗能量。
2. 碰撞敌人触发击杀与掉牌。
3. 收集 5 张扑克自动结算牌型并对 Boss 造成伤害。
4. Boss 在 75/50/25 阶段阈值触发地图重置。
5. 胜负后进入 3 选 1 Buff，选择后继续下一局。

## 2. 运行入口

1. 主场景：res://Main.tscn
2. 入口配置：project.godot 中 run/main_scene = res://Main.tscn

## 3. 场景与脚本关系

### 3.1 主场景 Main.tscn

Main.tscn 分层：

1. World：地形、墙体、出生点。
2. Runtime：运行时实体容器。
3. Systems：全局管理器与系统节点。
4. UI：HUD 画布层。

节点与脚本：

1. Main -> MainController.cs
2. World/Walls -> Walls.cs
3. Systems/MapResetController -> MapResetManager.cs
4. Systems/RoundSettlementController -> RoundSettlementController.cs
5. Systems/SpawnManager -> SpawnManager.cs
6. Systems/EnergyManager -> EnergyManager.cs
7. Systems/CardManager -> CardManager.cs
8. Systems/BossManager -> BossManager.cs
9. Systems/GameManager -> GameManager.cs
10. Systems/FeedbackManager -> FeedbackManager.cs

Runtime 关键容器：

1. Runtime/Player
2. Runtime/Enemies
3. Runtime/Obstacles
4. Runtime/BossInstance
5. Runtime/Drops

### 3.2 实体子场景 scenes

1. scenes/Player.tscn
   - 根节点 Player -> PlayerController.cs
   - 子节点 DragGuide -> DragIndicatorUI.cs
   - 子节点 Visual -> PlayerDebugVisual.cs
2. scenes/Enemy.tscn
   - 根节点 Enemy -> EnemyController.cs
3. scenes/Chaser.tscn
   - 根节点 Chaser -> ChaserEnemy.cs
4. scenes/ChaserArcher.tscn
   - 根节点 ChaserArcher -> ChaserEnemy.cs
5. scenes/ChaserLancer.tscn
   - 根节点 ChaserLancer -> ChaserEnemy.cs
6. scenes/ChaserPawn.tscn
   - 根节点 ChaserPawn -> ChaserEnemy.cs
7. scenes/Obstacle.tscn
   - 根节点 Obstacle -> ObstacleController.cs
   - 子节点 Wood -> WoodBoard.cs
   - 子节点 Stone -> StoneBoard.cs
8. scenes/Boss.tscn
   - 根节点 Boss -> BossController.cs
9. scenes/CardDrop.tscn
   - 根节点 CardDrop -> CardDrop.cs

### 3.3 UI 场景

ui/HUD.tscn：

1. 根节点 HUD -> HUD.cs
2. TopBar/EnergyPanel -> EnergyBarUI.cs
3. TopBar/DeckPanel -> CardDisplayUI.cs
4. PhaseBanner -> PhaseHintUI.cs

### 3.4 测试场景

1.tscn 使用的是外部 gd 脚本 node_2d.gd，不参与主玩法链路。

## 4. 新增 Buff 系统关系

### 4.1 资源关系

1. data/cards/buff_catalog.tres
   - 汇总 BuffCardData 资源列表。
2. data/cards/overload_propulsion.tres
3. data/cards/energy_battery.tres
4. data/cards/energy_recovery.tres
5. data/cards/chain_shockwave.tres
6. data/cards/low_friction_surface.tres

### 4.2 运行时关系

1. RoundSettlementController.cs
   - 管理局末 3 选 1、已拥有 Buff 集合、重开触发。
2. GameplayEventBus.cs
   - 提供 OnEnemyKilled、OnPlayerShoot、OnCollision 事件总线。
3. CardEffectRuntime.cs
   - 订阅事件并执行 Buff 效果。
4. HUD.cs
   - Continue 改为发信号，由结算控制器决定是选卡还是重开。
5. Main.tscn
   - Systems/RoundSettlementController 绑定 BuffCatalog 资源。

生命周期：

1. Buff 一旦获得，在本次进程内跨局持续生效。
2. 仅在退出游戏进程后清空。

## 5. 全脚本职责与关系清单

### 5.1 核心战斗链路

1. scripts/PlayerController.cs
   - 玩家拖拽施力、移动衰减、碰撞处理、血量、相机控制。
   - 发布 OnPlayerShoot 与 OnCollision 事件。
2. scripts/EnergyManager.cs
   - 能量消耗、恢复、能量变化信号。
3. scripts/EnemyController.cs
   - 普通敌人击杀逻辑、掉牌生成、击杀事件发布。
4. scripts/ChaserEnemy.cs
   - 追击敌人 AI、接触惩罚、死亡爆炸、击杀事件发布。
5. scripts/CardDrop.cs
   - 掉牌自动飞入 HUD 卡槽并完成收集。
6. scripts/CardManager.cs
   - 扑克牌收集、5 张自动结算、HandSettled 事件。
7. scripts/PokerHandEvaluator.cs
   - 5 张扑克牌型识别与倍率计算。
8. scripts/BossController.cs
   - Boss 血量、阶段切换、死亡事件。
9. scripts/BossManager.cs
   - 扑克牌结算伤害转 Boss 扣血，阶段变化触发重置。
10. scripts/GameManager.cs
    - 全局胜负状态、重开流程、与 HUD 的胜负展示对接。
11. scripts/FeedbackManager.cs
    - 震屏、短停顿、结算和受击反馈。

### 5.2 地图与重置

1. scripts/SpawnManager.cs
   - 运行时敌人/障碍/掉落清理与重生。
2. scripts/MapResetManager.cs
   - 阶段重置请求入口，调用 SpawnManager。
3. scripts/Walls.cs
   - 围墙物理参数与边界行为封装。
4. scripts/MainController.cs
   - Main 根节点控制器（当前为轻量壳，承载扩展点）。

### 5.3 障碍系统

1. scripts/ObstacleBase.cs
   - 障碍通用破坏阈值逻辑与 Broken/HitButNotBroken 信号。
2. scripts/WoodBoard.cs
   - 木板实现，低阈值可破坏。
3. scripts/StoneBoard.cs
   - 石板实现，高阈值可破坏。
4. scripts/ObstacleController.cs
   - 木板/石板切换容器和激活逻辑。

### 5.4 UI 系统

1. scripts/HUD.cs
   - HUD 总控，胜负面板与 Buff 选择面板，发出 ContinueRequested 与 BuffChoiceSelected。
2. scripts/EnergyBarUI.cs
   - 绑定 EnergyManager 展示能量条。
3. scripts/CardDisplayUI.cs
   - 牌槽显示、结算高亮与提示动画。
4. scripts/PhaseHintUI.cs
   - Boss 阶段提示动画。
5. scripts/DragIndicatorUI.cs
   - 玩家拖拽线视觉增强。
6. scripts/PlayerDebugVisual.cs
   - 玩家调试可视化组件。

### 5.5 数据与枚举

1. scripts/CardData.cs
   - 扑克花色、点数、CardData 结构。
2. scripts/GameResultState.cs
   - 全局状态枚举。
3. scripts/BuffCardData.cs
   - Buff 卡数据资源结构。
4. scripts/BuffCardCatalog.cs
   - Buff 卡池资源容器。

### 5.6 Buff 事件驱动模块

1. scripts/RoundSettlementController.cs
   - 局末 3 选 1、去重、Buff 持久化（本次进程内）、重开衔接。
2. scripts/GameplayEventBus.cs
   - 游戏事件总线，统一信号入口。
3. scripts/CardEffectRuntime.cs
   - Buff 效果执行器（过载推进、能量电池、能量回收、连锁冲击、低摩擦）。

### 5.7 调试与遗留脚本

1. scripts/DebugManager.cs
   - 调试开关、调试面板文本、碰撞形状可视化。
2. scripts/HudController.cs
   - 旧 HUD 控制器，当前主链路不使用（已由 HUD.cs 取代）。
3. scripts/MapResetController.cs
   - 旧重置控制器壳，当前主链路不使用（使用 MapResetManager.cs）。

## 6. 关键事件链路

### 6.1 扑克牌结算链

1. EnemyController 或 ChaserEnemy 触发击杀。
2. CardManager 收卡并在满 5 张后结算。
3. PokerHandEvaluator 输出倍率。
4. BossManager 将倍率转换为 Boss 伤害。
5. BossController 触发阶段或死亡事件。

### 6.2 Buff 事件链

1. PlayerController 发布 OnPlayerShoot、OnCollision。
2. EnemyController/ChaserEnemy 发布 OnEnemyKilled。
3. CardEffectRuntime 订阅事件并执行 Buff。
4. RoundSettlementController 管理 Buff 获取和跨局生效。

### 6.3 重开链

1. HUD ContinueRequested。
2. RoundSettlementController 决定直接重开或先弹 3 选 1。
3. GameManager.RequestRestart 执行全局重置。
4. CardEffectRuntime.ApplyRunStartBuffs 应用开局类 Buff。

## 7. 文档维护约定

后续修改场景或脚本时，请同步更新以下信息：

1. 场景脚本挂载关系（第 3 节）。
2. 全脚本职责清单（第 5 节）。
3. 关键事件链路（第 6 节）。

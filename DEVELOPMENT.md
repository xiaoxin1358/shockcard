# Shockcard Demo 开发文档

## 1. 项目定位

本项目是 Godot 4 + C# 的 2D 最小可玩战斗 Demo。
核心体验是：拖拽冲刺 -> 撞击敌人掉牌 -> 5 张自动结算 -> 对 Boss 造成伤害 -> Boss 阶段切换重置地图 -> 胜负判定。

## 2. 运行入口

- 主场景：res://Main.tscn
- 项目入口配置：project.godot 中 run/main_scene = res://Main.tscn

## 3. 场景结构

Main.tscn 主要分四层：

- World：静态地理和出生点
- Runtime：运行时实体容器（玩家、敌人、障碍、Boss、掉落）
- Systems：管理器层（战斗、重置、卡牌、能量、反馈、胜负）
- UI：HUD

关键节点：

- World/SpawnPoints/PlayerSpawn
- World/SpawnPoints/EnemySpawns
- World/SpawnPoints/ObstacleSpawns
- Runtime/PlayerInstance
- Runtime/Enemies
- Runtime/Obstacles
- Runtime/BossInstance
- Runtime/Drops

## 4. 系统与职责

### 4.1 玩家移动与能量

- PlayerController.cs：拖拽施加冲量、滑行阻尼、碰撞保速。
- EnergyManager.cs：能量状态与消耗/恢复。

规则：

- 拖拽释放时按冲量扣能量。
- 能量不足时不施加新加速度。

### 4.2 敌人和掉牌

- EnemyController.cs：玩家碰到敌人即击杀。
- CardDrop.cs：掉牌对象自动飞入顶部卡槽。

### 4.3 卡牌与结算

- CardData.cs：花色与点数结构。
- CardManager.cs：收集、结算触发、事件分发。
- PokerHandEvaluator.cs：5 张标准牌型判定与倍率输出。

牌型支持：

- 高牌、一对、两对、三条、顺子、同花、葫芦、四条、同花顺

### 4.4 Boss 与阶段

- BossController.cs：血量、受伤、阶段（75/50/25）、死亡事件。
- BossManager.cs：把牌型倍率转伤害，监听阶段并通知地图重置。

### 4.5 地图重置与生成

- MapResetManager.cs：重置请求入口，处理阶段切换触发。
- SpawnManager.cs：清理/重生敌人与障碍，支持玩家位置两种策略。

已实现的玩家位置策略：

- 保留当前位置
- 回到出生点

### 4.6 障碍系统

- ObstacleBase.cs：通用速度阈值破坏逻辑与事件。
- WoodBoard.cs：低阈值，容易破坏。
- StoneBoard.cs：高阈值，需要高速。
- ObstacleController.cs：木板/石板切换容器。

### 4.7 HUD 模块化

- HUD.cs：HUD 总控。
- EnergyBarUI.cs：能量条。
- CardDisplayUI.cs：牌槽与结算提示。
- BossHpBarUI.cs：Boss 血条。
- PhaseHintUI.cs：阶段提示。
- DragIndicatorUI.cs：拖拽方向与力度线条增强。

### 4.8 胜负状态机

- GameResultState.cs：Running/Won/Lost/Stopped。
- GameManager.cs：统一胜负判定与流程锁。

规则：

- 能量耗尽且 Boss 未击败 -> 失败
- Boss 击败 -> 胜利

### 4.9 手感反馈

- FeedbackManager.cs：轻量 hit stop + camera shake。

反馈入口：

- 敌人撞击
- 障碍破坏（木板/石板不同强度）
- 牌型结算
- Boss 受击

## 5. 关键事件流

### 5.1 战斗主循环

1. 玩家拖拽冲刺并消耗能量
2. 撞敌掉牌，牌自动飞入 HUD
3. 收满 5 张自动结算牌型
4. BossManager 接收结算倍率并对 Boss 扣血
5. Boss 跨阶段阈值触发地图重置
6. SpawnManager 清理并重生敌人与障碍
7. 循环直到胜利或失败

### 5.2 重置链路

BossController.PhaseChanged
-> BossManager.OnBossPhaseChanged
-> MapResetManager.RequestResetByBossPhase
-> SpawnManager.ResetRuntime

### 5.3 胜负链路

- EnergyManager.EnergyChanged -> GameManager 失败判定
- BossController.Defeated -> GameManager 胜利判定
- GameManager -> HUD.ShowGameResult

## 6. 可调参数建议

### 玩家

- MaxDragDistance
- ImpulsePerPixel
- MaxSpeed
- SlideDamping
- CollisionRetain

### 能量

- MaxEnergy
- EnemyKillRestoreAmount
- SettlementRestoreAmount
- EnergyCostPerImpulse

### Boss

- MaxHp
- BaseSettlementDamage（BossManager）

### 重置

- KeepPlayerPositionOnPhaseReset（MapResetManager）
- EnemySpawnCount / ObstacleSpawnCount（SpawnManager）

### 反馈

- MaxShakePixels
- ShakeRecoverSpeed
- 各事件的 hit stop 时长与强度（FeedbackManager 内）

## 7. 当前完成度

已具备完整可运行闭环：

- 拖拽冲刺与能量消耗
- 撞击敌人掉牌与自动收集
- 5 张自动牌型结算并对 Boss 伤害
- Boss 阶段切换重置地图
- 胜负判定与 HUD 提示
- 基础手感反馈（震屏 + 短停顿）

## 8. 已知技术债

1. 仍保留少量旧脚本壳（例如旧版控制器文件），当前不在主链路使用，建议后续清理。
2. hit stop 使用全局 TimeScale，简单有效，但会影响所有逻辑与 UI。后续可升级为局部停顿。
3. 牌型判定目前是固定 5 张模型，未做 6+ 张取最优 5 张扩展。

## 9. 推荐下一步

1. 增加一键重开流程（重置能量、Boss、Runtime 对象、HUD）。
2. 为牌型判定补固定样例测试，确保回归稳定。
3. 将反馈系统升级为分层停顿（战斗层停，UI层不停）。
4. 清理未使用脚本与节点，降低维护成本。

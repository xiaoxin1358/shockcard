# Shockcard

一个基于 Godot 4.6 + C# 的 2D 横版物理冲刺 Demo。

核心玩法是把“拖拽施力的动量手感”和“扑克结算的策略反馈”结合在一起，形成高密度短局循环。

English summary: A 2D side-scrolling physics dash prototype built with Godot 4.6 and C#, featuring drag-to-boost movement, enemy collision kills, poker-hand settlement, and boss phase resets.

## 演示

- Demo GIF: `./assets/demo.gif`（占位，后续替换）
- Gameplay Screenshots: `./assets/screenshots/`（占位，后续补充）

## 核心特性

- 拖拽施力移动：拖拽长度映射加速度，带有速度衰减与碰撞反馈。
- 能量管理：施力会消耗能量，能量系统影响进攻节奏。
- 碰撞击杀与掉牌：敌人被击杀后掉落扑克，自动收集进入卡槽。
- 5 张自动结算：满 5 张后自动识别牌型并结算伤害倍率。
- Boss 阶段机制：Boss 在 75% / 50% / 25% 血线触发阶段切换与地图重置。
- 局后 3 选 1 Buff：每局结算后提供 Buff 选择，强化后继续下一局。

## 快速开始

### 环境要求

- Godot 4.6（C# / .NET 版本）
- .NET 8.0（Android 目标使用 net9.0）

### 本地运行

1. 用 Godot 4.6 打开项目根目录。
2. 等待 C# 项目加载完成。
3. 直接运行主场景（已在项目配置中设置）：`res://Main.tscn`
4. 在编辑器中按 F5 或点击运行按钮启动。

### 关键配置

- 主场景入口：`project.godot` -> `run/main_scene = "res://Main.tscn"`
- C# 工程配置：`shockcard.csproj`（Godot.NET.Sdk 4.6.1，net8.0）

## 玩法循环

1. 玩家拖拽施力并消耗能量。
2. 撞击敌人触发击杀，生成掉牌。
3. 收集 5 张扑克后自动结算牌型。
4. 牌型倍率转化为对 Boss 的伤害。
5. Boss 阶段变化触发地图重置。
6. 胜负后进入 3 选 1 Buff，选择后继续下一局。

## 项目结构

```text
shockcard/
├─ Main.tscn                  # 主场景入口
├─ project.godot              # 引擎与运行配置
├─ shockcard.csproj           # C# 项目配置
├─ scenes/                    # Player/Enemy/Boss/Obstacle/CardDrop 子场景
├─ scripts/                   # 核心玩法与系统脚本
├─ data/cards/                # Buff 资源与卡池
├─ ui/                        # HUD 场景
└─ DEVELOPMENT.md             # 详细开发文档
```

## 系统架构（概要）

- Player 系统：`PlayerController` 负责拖拽施力、移动衰减、碰撞与反馈触发。
- Energy 系统：`EnergyManager` 负责能量消耗与恢复。
- Enemy 系统：`EnemyController` / `ChaserEnemy` 负责击杀、掉牌、追击逻辑。
- Card 系统：`CardManager` + `PokerHandEvaluator` 负责收卡与牌型结算。
- Boss 系统：`BossController` + `BossManager` 负责血量、阶段、受伤结算。
- Map Reset：`MapResetManager` + `SpawnManager` 负责阶段重置与重生清理。
- Settlement/Buff：`RoundSettlementController` + `CardEffectRuntime` 负责局末选卡与 Buff 生效。
- HUD：`HUD` 及子 UI 组件负责显示与交互信号。

完整场景挂载关系、脚本职责与事件链路见：`DEVELOPMENT.md`

## 素材与许可

### 第三方素材

- Kenney Tiny Town（目录：`kenney_tiny-town/`）
  - 许可：CC0（可商用，建议保留出处说明）
- Tiny Swords（目录：`Tiny Swords/`）
  - 许可：以资源包内附带声明为准

### 项目代码许可

当前仓库尚未提供明确的 `LICENSE` 文件。

建议后续补充开源许可证（如 MIT）以便明确代码使用边界。

## 开发文档

- 详细开发文档：`DEVELOPMENT.md`
  - 包含场景分层、脚本职责、事件链路与文档维护约定。

## 项目状态

- 当前阶段：MVP 玩法验证
- 目标：优先保证可玩性与核心循环稳定，再逐步打磨内容与表现

---

如果你想参与开发，建议先阅读 `DEVELOPMENT.md`，再从 `Main.tscn` 和 `scripts/` 目录开始追踪系统调用关系。

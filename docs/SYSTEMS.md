# 可扩展系统：事件、动画、故事、声音、经济、武器、修仙

本文对应 8 点扩展需求。实现原则是**模拟层先行**：所有规则都在 `Assets/Scripts/Sim`（无 UnityEngine 依赖），Unity 层只负责输入、表现、动画和 UI。这样规则可测试、可存档，也方便之后替换素材或加联机。

## 1. 事件与统计

核心：`Sim/GameEvents.cs`

- `GameEventType`：Kill / Loot / Trade / Quest / Dialogue / Noise / Weapon / Technique / Story / Escort / Damage / Craft。
- `GameEvent`：类型、键、文本、数量、游戏日/分钟、自增 Id。
- `RunStats`：
  - 存活秒数、游戏内分钟、步行/疾行距离
  - 击杀、爆头、拾取、晶核、交易、任务、对话、声响、射击、换弹、术法、伤害、死亡
  - `Counters`：按 `kill.walker`、`kill.police` 等键分类
  - `Events`：最多 120 条，存档保留最近 60 条

写入点：

```csharp
sim.Stats.Tick(dt, gameMinutes);                 // WorldSim.Update
sim.OnZombieKilled(def);                         // ZombieAI.Die -> GameRoot
sim.RecordDamageToZombie(damage, headshot);
sim.EmitNoise("gunshot", x, z, radius, 1f);
sim.RecordEvent(GameEventType.Trade, "buy.trader_system", "购入绷带", 1);
```

展示：手机「行迹」App（`PhoneApps.BuildStatsApp`）、死亡结算、消息 App。
扩展：新增统计键不需要改 UI；直接 `Stats.Add("key", n)`，行迹页按需加一行。

## 2. 动画能力

核心：`Game/CharacterAnimation.cs`

所有角色（玩家、僵尸、NPC）共用一个组件。它有两种模式：

1. **正式素材模式**：子物体上有 `Animator` 时，只写参数，由 Animator Controller 播动画。
2. **灰盒模式**：没有 Animator 时，用程序化关节摆动显示走路/跑步/攻击/撕咬/受击/死亡/坐下/交谈/入定/瞄准/换弹。

Animator 参数契约：

| 类型 | 名称 | 用途 |
| --- | --- | --- |
| Float | `Speed` | 当前移动速度 |
| Float | `StateTime` | 当前状态时长 |
| Bool | `Moving` / `Running` | 移动/疾行 |
| Bool | `Dead` | 死亡 |
| Bool | `Sitting` / `Talking` / `Meditating` / `Aiming` | 状态 |
| Trigger | `Attack` / `Bite` / `Hit` / `Cast` / `Reload` | 一次性动作 |

状态枚举：`Idle, Walk, Run, Attack, Bite, Hit, Death, Sit, Talk, Meditate, Aim, Reload, Cast`。

调用示例：

```csharp
anim.SetLocomotion(speed, moving, sprinting);
anim.Play(CharacterAnimState.Attack, 0.35f);
anim.Play(CharacterAnimState.Bite, 0.42f);
anim.SetMeditating(sim.Meditating);
anim.SetDead();
```

替换素材时只需要：

- 角色 prefab 上放 Animator，参数名按上表；
- `CharacterAnimation.Setup` 传 visual/躯干/头/四肢（灰盒用；有 Animator 时仍保留引用无妨）；
- 武器挂点保持在右手 `WeaponPivot` 或 Animator 的 hand bone。

## 3. 故事脚本、NPC 对话、带领与安全屋

核心：`Sim/Story.cs` + `Content.cs` 中的 `NpcDef / DialogueDef / DialogueNodeDef / DialogueChoiceDef`。

### 当前剧情竖切片

- 阿禾（守夜人，安全屋内）：
  - 初次对话：解释水、夜、诊所；选择后设置 `met_ahe`，接取「第一夜」。
  - 第一夜完成后：可让她**跟随**，接取「诊所的路」。
  - 跟随中再对话：可让她**留下等待**。
  - 诊所任务完成后：解锁后续对话与古井线索。
- 老陈（杂货商，杂货铺内）：
  - 对话可打开 NPC 交易；只有旧币。
  - 提供警尸/军尸/古井的传闻。

### 对话动作（数据）

`DialogueChoiceDef.Action` 支持 `|` 串联：

| 动作 | 效果 |
| --- | --- |
| `flag:met_ahe` | 设置故事标记；会驱动 `visit/talk` 任务目标 |
| `quest:first_night` | 接取任务 |
| `follow` / `wait` | 阿禾跟随/原地等待 |
| `trade:trader_laochen` | 对话结束后打开交易 UI |
| `trust:2` | 提高信任（预留给后续剧情） |
| `give:bandage:2` | 给物品 |

条件：`RequiresFlag`、`RequiresItem`。

### NPC 行为

`Game/NpcController.cs`：

- `Following`：跟随玩家，保持 2.2m；玩家停，NPC 面向玩家。
- `Wait`：原地等待，面向玩家。
- `Safehouse`：在 `HomeX/HomeZ` 附近小范围游走；停下时进入坐姿。
- 枪声等大声响：5 秒警觉，转向声源；阿禾会提醒“枪声会把它们都引过来”。
- 对话中：`Talking` 动画，面向玩家。

扩展：新增 NPC 只需在 `Content.CreateDemo` 加 `NpcDef` + `DialogueDef`；`WorldBuilder.BuildNpcs` 自动生成。后续可把同一结构导出为 JSON/ScriptableObject。

## 4. 声源与感知

核心：`Sim/Noise.cs` + `WorldSim.EmitNoise` + `Game/AudioDirector.cs`。

`NoiseEvent`：来源、X/Z、半径、响度、游戏时间。`NoiseSystem` 保存最近 40 条并广播 `Emitted`。

| 行为 | 来源键 | 半径 | 响度 |
| --- | --- | --- | --- |
| 走路脚步 | `footstep` | 4m | 0.25 |
| 疾行脚步 | `footstep` | 8m | 0.55 |
| 近战 | `melee` | 2.5–5m | 0.55 |
| 撕咬 | `bite` | 6m | 0.4 |
| 换弹 | `reload` | 5m | 0.4 |
| 手枪 | `gunshot` | 30m | 1.0 |
| 猎枪 | `gunshot` | 42m | 1.0 |
| 霰弹枪 | `gunshot` | 40m | 1.0 |

消费方：

- `ZombieAI.ScanNoise()`：在半径内听到最新声响 → 去声源调查 7 秒；枪声会打断游荡。
- `NpcController.ScanNoise()`：转向声源，枪声触发一次提醒。
- `AudioDirector`：订阅同一事件，播放占位程序化音效（枪声/脚步/撕咬/拾取/UI）。替换为真实 AudioClip 时玩法不变。

扩展：新增 `NoiseEmitter` 组件或直接 `sim.EmitNoise`；所有 AI 自动接入。

## 5. 交易系统

核心：`Sim/Economy.cs`

- `Wallet`：旧币 / 灵石（`coin` / `spirit_stone`）。
- 晶核是**物品货币** `crystal_core`，由僵尸掉落，天机碑只收它。
- `TraderDef.Currency` 决定代币：
  - `trader_system`：`crystal_core`（系统）
  - `trader_laochen`：`coin`（NPC）
- `TradeOfferDef`：买价、卖价、库存（`-1` 无限）。
- `TradeSystem.Buy/Sell` 负责重量、库存、钱包/物品扣除、统计、事件。

UI：

- 手机「交易」App：查看天机碑货单、打开商栈。
- 通用 `TradeUI`：任意 trader，显示买/卖/库存/代币；NPC 对话里选择「看看货」打开。

扩展：新增商人 = 一个 `TraderDef`；新增货币 = `Wallet` 字段或物品货币，`CurrencyName/CurrencyCount` 自动适配。

## 6. 掉落系统

核心：`Sim/Economy.cs` 的 `LootSystem`，数据在 `Content.LootTables`。

- 每个 `ZombieDef.LootTable` 指定表。
- `LootEntry` 支持物品（Min/Max/Chance）或货币（Amount/Min/Max）。
- 击杀 → `GameRoot.OnZombieKilled` → `Sim.RollLoot` → `ItemPickup.Create` 在地上生成可拾取物。

当前表：

| 类型 | 关键掉落 |
| --- | --- |
| 行尸 | 旧币、绷带、罐头、废铁、少量晶核、皮夹克 |
| 警尸 | 9mm 子弹、手枪、防弹背心、警帽、晶核 |
| 军尸 | 步枪弹、猎枪、防弹背心、晶核 |
| 猎尸 | 霰弹、霰弹枪、皮夹克、晶核 |
| 巨尸 | 大量晶核、废铁、旧币 |

拾取：`PickupInteractable` 按 E；物品进背包，旧币进钱包；记录 Loot 统计/事件。

扩展：新增掉落表、改概率/数量即可；不需要改击杀代码。

## 7. 武器系统

核心：`Sim/WeaponSystem.cs`、`Content.WeaponDef`、`PlayerController`。

- 近战：棒球棍、猎刀；伤害/范围/冷却/体力/噪声。
- 枪械：手枪、猎枪、霰弹枪；
  - `AmmoItem`、`Magazine`、`ReloadTime`、`NoiseRadius`、`Pellets`、`Spread`
  - 弹匣状态存 `Player.Magazines`，换弹消耗背包弹药
  - 左键射击：射线 + 散布 + 多弹丸 + 曳光；命中僵尸结算伤害
  - `F` 换弹；`HUD` 显示 `弹匣/容量 弹药`
  - 枪声进入噪声总线，会吸引半径内僵尸
- 掉落来源：警尸/军尸/猎尸；诊所/仓库的医疗柜/军械箱也可搜到。
- 服装：皮夹克/防弹背心/警帽，`Armor` 参与伤害减免；手机行囊可装备。

扩展：新枪 = 一个 `ItemDef` + `WeaponDef`；不用改输入逻辑。

## 8. 修仙：功法自主升级与身法

核心：`Player.TechniqueLevels`、`WorldSim.UpgradeTechnique`、`TechniqueDef`。

- 功法等级 1–5；升级消耗**悟性**（任务奖励获得）。
- 被动数值按等级缩放（护甲、气血、回气、入定效率等）。
- 主动术法伤害/持续按等级缩放。
- 新增身法：
  - **御风步**：持续 6 秒（随等级延长），移动/疾行速度 ×1.75；`2` 键或手机修行页施展。
  - **踏云诀**：筑基后可学，速度 ×2.35，接近旧时代车辆；`3` 键。
- 身法消耗灵气、有冷却；`WorldSim.SpeedBuffUntil` 计时，`PlayerController` 直接使用 `sim.MoveSpeed/SprintSpeed`。
- 手机「修行」页有「施展」「升级」按钮；任务奖励 `insight`。

扩展：新功法 = `TechniqueDef`；身法只需 `Active.Kind = "movement"` + `SpeedMult/SprintMult/Duration`，移动代码自动生效。

## 9. 存档

`SaveData` 版本 2，新增：

- `StatsSave`（统计 + 事件）
- `Wallet`（旧币/灵石）
- `Insight`、`TechniqueLevels`、`Magazines`、`Clothing`
- `Npcs`（跟随/等待/信任/安全屋）
- `Flags`（故事标记）
- 原有任务、物品、功法、冷却、时间

旧存档（Version 1）缺字段时使用默认值；新字段为空时初始化默认。

## 10. 验证现状

```bash
dotnet run --project scripts/unity-sim-tests/tests.csproj   # 52/52 PASSED
./tools/unity-check/check.sh                                  # 0 warning / 0 error
```

Play Mode 冒烟（`Assets/Editor/PlayModeSmoke.cs`）自动进入游戏并验证：

- 4 栋建筑、11 个不同星级的僵尸、2 个 NPC
- 对话打开/关闭、交易打开、行迹 App
- 手枪开火/弹匣/换弹、噪声半径、掉落表
- 功法学习/升级/施展、身法 Buff
- 对话选择/剧情标记
- 击杀/掉落/统计/存档

截图：`docs/screenshots/unity-{game,dialogue,trade,stats,phone}.png`。

# 劫余问道 · Ashen Dao

2.5D 俯视、可进入建筑、有视野范围、手机式拟真 UI 的僵尸生存 + 修仙 + 任务游戏。当前唯一实现为 **Unity 6.4 / URP**。

![Unity Play Mode](docs/screenshots/unity-game.png)

![手机 UI](docs/screenshots/unity-phone.png)

## 打开工程

1. Unity Hub 添加 **`unity/AshenDao`**（不是仓库根目录）。
2. 使用 **6000.4.2f1**（Unity 6.4）打开。
3. 打开 `Assets/Scenes/SampleScene.unity`，按 Play。

场景本身只有相机/平行光/Volume；`Bootstrap` 会在场景加载后自动创建 `GameRoot`、玩家、小镇、NPC、UI，不需要手动拖 Prefab。

首次打开会重新生成 `Library`、导入 URP 包并编译，耗时取决于机器。

## 操作

| 键 | 行为 |
| --- | --- |
| WASD | 相对相机移动 |
| Shift | 疾行 |
| 鼠标左键 | 近战挥击 / 开枪 |
| E | 搜索 / 采集 / 交谈 / 拾取 |
| F | 换弹 |
| R | 入定 / 收功 |
| Q / 1 | 剑气斩 |
| 2 / 3 | 御风步 / 踏云诀 |
| Tab | 手机 |
| Esc | 关闭对话 / 交易 / 手机 |
| F5 / F9 | 存档 / 读档 |
| F8 | 删除存档并重新开始 |

## 工程结构

```
unity/AshenDao/
  Assets/
    Scenes/SampleScene.unity      # 仅相机/灯光/Volume
    Scripts/
      Sim/                        # 纯 C# 模拟（无 UnityEngine 依赖）
        WorldSim.cs               # 时间/需求/灵气/修为/破境/物品
        Content.cs                # 物品/功法/境界/任务/掉落/商人/NPC/对话
        QuestSystem.cs            # 任务接取/进度/奖励
        Story.cs                  # 剧情标记/对话/NPC 状态
        Economy.cs                # 钱包/交易/掉落
        WeaponSystem.cs           # 弹匣/换弹/枪械
        Noise.cs                  # 声源总线
        GameEvents.cs             # 统计/事件时间线
        SaveData.cs               # 存档 DTO
      Game/                       # Unity 表现/交互
        GameRoot.cs               # 启动、输入、系统装配
        WorldBuilder.cs           # 程序化小镇/建筑/僵尸/NPC
        PlayerController.cs       # 移动/近战/枪械/交互/术法
        ZombieAI.cs               # 感知/追击/撕咬/听觉
        NpcController.cs          # 跟随/等待/安全屋/对话
        CharacterAnimation.cs     # 动画契约 + 灰盒程序化动作
        BuildingController.cs     # 入室屋顶/前墙淡出
        TopDownCamera.cs          # 正交相机/视野/特效
        AudioDirector.cs          # 程序化占位音效
        SaveSystem.cs             # persistentDataPath JSON
      UI/                         # UGUI
        PhoneUI.cs / PhoneApps.cs # 手机壳/主屏/行迹/交易/修行等
        HudUI.cs                  # 气血/灵气/弹药/死亡
        DialogueUI.cs             # NPC 对话
        TradeUI.cs                # 通用交易
        UiFactory.cs              # 构建工具/圆角/字体
    Resources/Fonts/              # Noto Sans SC 子集（OFL）
  Packages/                       # URP 17 / UGUI 2 / 内置模块
  ProjectSettings/
docs/                             # Unity 设计与素材规格
scripts/unity-sim-tests/          # 纯 C# 断言（dotnet）
tools/unity-check/                # Unity 脚本编译检查
```

### 运行时链路

```
SampleScene
  └─ Bootstrap (RuntimeInitializeOnLoad)
      └─ GameRoot
          ├─ ContentDatabase / WorldSim
          ├─ WorldBuilder → 建筑/僵尸/NPC/容器/拾取
          ├─ PlayerController / ZombieAI / NpcController
          ├─ TopDownCamera + VisionController + DayNight
          ├─ PhoneUI / HudUI / DialogueUI / TradeUI
          └─ AudioDirector / SaveSystem
```

- **Sim** 是唯一状态源，不引用 `UnityEngine`；任务、交易、剧情、掉落、声源、统计都在这里。
- **Game** 只处理输入、AI 表现、相机、建筑淡出、动画和音频。
- **UI** 直接读 `WorldSim`，不复制第二套状态。
- 存档由 `WorldSim.ExportSave/ImportSave` 生成 DTO，`SaveSystem` 写 JSON。

## 已实现

- 2.5D 正交俯视、平滑跟随、可进入的 4 栋建筑
- 46 射线视野扇形 + Spot Light + 僵尸 Linecast 感知
- 近战 + 手枪/猎枪/霰弹枪；弹匣/换弹/散布/曳光/枪声
- 11 个不同星级僵尸：行尸/疾行/警尸/军尸/猎尸/巨尸
- 掉落：枪械/弹药/衣物/晶核；拾取、装备、护甲
- 交易：天机碑收晶核，老陈收旧币；买卖/库存/存档
- 剧情：阿禾/老陈、跟随/等待/安全屋、诊所护送
- 声源：脚步/近战/撕咬/换弹/枪声；吸引僵尸、NPC 警觉
- 修仙：7 条任务链、功法 1–5 层、悟性升级、御风步/踏云诀
- 手机 8 个 App：任务/修行/背包/地图/行迹/交易/消息/设置
- 统计与事件时间线、死亡结算、JSON 存档

## 验证

```bash
# 纯 C# 玩法断言（无需 Unity 许可证）
./tools/unity-check/test-sim.sh

# 用 Unity 6.4 托管 DLL 编译 Assets/Scripts
UNITY_APP=/path/to/Unity.app ./tools/unity-check/check.sh
```

当前结果：

- 编译：**0 warning / 0 error**
- 纯 C#：**52/52** 通过
- Play Mode：4 栋建筑、11 个僵尸、2 个 NPC；对话/交易/行迹/枪械/噪声/功法/剧情/掉落均验证，**0 个游戏错误**

Play Mode 冒烟报告：[docs/unity-smoke.json](docs/unity-smoke.json)。

## 文档

- [Unity 落地方案](docs/UNITY.md)：2.5D、视野、建筑、手机 UI、验证
- [可扩展系统](docs/SYSTEMS.md)：统计/动画/剧情/声源/交易/掉落/枪械/修仙
- [故事脚本竖切片](docs/STORY_SCRIPT.md)
- [Unity 素材规格](docs/UNITY_ASSET_SPEC.md)：3D 模块/角色/动画/贴图/手机 UI/音频
- [手机 UI 设计稿](docs/phone-ui-mockup.html)

## 当前边界

- 灰盒资产：建筑/角色/枪械是基础几何体，按素材规格替换。
- 动画/音频为契约 + 占位；补 Animator/AudioClip 即可，不改玩法。
- 剧情为竖切片，不是完整剧本。
- NPC 跟随没有 NavMesh，复杂室内可能卡门。
- 未做触屏/手柄、性能 profiling、联机。

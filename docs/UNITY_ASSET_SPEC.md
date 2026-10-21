# Unity 素材规格（3D + 手机 UI）

当前 Unity 工程是**灰盒**：建筑、角色、家具都由 Cube/Capsule 拼成，只用于验证 2.5D 镜头、进入建筑、视野和手机 UI。本文是替换为正式资产时的契约。

## 1. 世界尺度

| 项 | 值 |
| --- | --- |
| 网格 | 1 Unity 单位 = 1 米 |
| 楼层高度 | 3.0m |
| 墙厚 | 0.2m |
| 门洞 | 1.7m 宽 × 2.3m 高 |
| 角色身高 | 1.8m（胶囊碰撞半径 0.34m） |
| 相机 | 正交，Size 8.5，俯角约 53°，固定 yaw |
| 建筑 | 8×6m ~ 14×10m |
| 道路 | 7m 宽 |

**所有模块必须贴 1m 网格**。不要把墙做成 1.03m 后靠缩放拼——碰撞、门洞和 AI 寻路都会错位。

## 2. 建筑模块（替换灰盒）

| 模块 | 尺寸 (m) | 说明 |
| --- | --- | --- |
| `Wall_1m` | 1.0 × 3.0 × 0.2 | 基础墙段，可横向拼接 |
| `Wall_2m` | 2.0 × 3.0 × 0.2 | 减少面数 |
| `Wall_Window` | 2.0 × 3.0 × 0.2 | 带窗洞；玻璃单独材质 |
| `DoorFrame` | 1.7 × 2.3 × 0.26 | 门框；中间留空 |
| `Door_Open` | 1.6 × 2.1 × 0.06 | 打开的门板，贴左门柱 |
| `Floor_2x2` | 2.0 × 0.1 × 2.0 | 木/瓷砖，可平铺 |
| `Roof_2x2` | 2.0 × 0.16 × 2.0 | 必须支持透明淡出 |
| `Counter` | 2.0 × 1.1 × 0.7 | 商店柜台 |
| `Shelf` | 1.6 × 2.2 × 0.4 | 货架 |
| `Bed` | 1.2 × 0.55 × 2.0 | 床 |
| `Cabinet` | 1.2 × 2.2 × 0.5 | 医疗柜 |
| `Crate` | 0.9 × 0.9 × 0.9 | 可搜索容器，盖子独立（便于开盖动画） |

要求：

- 墙/屋顶的 **pivot 在底部中心**；门框 pivot 在地面中心。
- 屋顶与前墙材质必须允许透明（`MaterialFactory.TransparentLit` 或自定义 shader），否则 `BuildingController` 淡出会失效（它会在最后直接禁用 Renderer，但仍希望有过渡）。
- 墙体左右边缘的法线/UV 连续，拼接不露缝。
- 不烘焙室内阴影；室内用点光源。
- 窗户玻璃用 emissive + 低不透明度；不要烘焙夜景。

## 3. 角色

| 项 | 建议 |
| --- | --- |
| 身高 | 1.8m，脚底在 y=0 |
| 朝向 | 模型正面朝 +Z（Unity 前向） |
| 骨骼 | Humanoid（Mixamo 兼容） |
| 面数 | LOD0 8k–15k 三角面；LOD1 3k–5k |
| 材质 | 1 套 PBR：albedo / normal / mask；皮肤与衣服可共用 |
| 动画 | idle / walk / run / attack / hit / death / meditate |
| 动画帧率 | 30fps 烘焙；根运动关闭（位移由 CharacterController 驱动） |

角色替换点：

- 玩家：`PlayerController.BuildVisual()` 里的原型几何体 → 实例化 `Player.prefab`；保留 `WeaponPivot` 手部挂点。
- 僵尸：`ZombieAI.Setup()` 的原型体 → `Zombie.prefab`；`TakeDamage`/`Die` 改调 Animator。

不要用 2D 精灵做 3D 场景角色（除非明确做 billboard 风格）；混用会造成排序和阴影不一致。

### 3.1 动画契约（必须匹配）

正式角色需要 Animator Controller，参数名按 `Game/CharacterAnimation.cs`：

- Float：`Speed`、`StateTime`
- Bool：`Moving`、`Running`、`Dead`、`Sitting`、`Talking`、`Meditating`、`Aiming`
- Trigger：`Attack`、`Bite`、`Hit`、`Cast`、`Reload`

状态：Idle / Walk / Run / Attack / Bite / Hit / Death / Sit / Talk / Meditate / Aim / Reload / Cast。

动画剪辑建议：

| 剪辑 | 帧数/时长 | 说明 |
| --- | --- | --- |
| idle | 60–120 帧循环 | 呼吸、轻微晃动 |
| walk | 24–30 帧循环 | 无根运动 |
| run | 18–24 帧循环 | 前倾 |
| attack | 20–28 帧 | 近战挥击；通知命中由代码结算 |
| bite | 24–30 帧 | 僵尸前扑撕咬 |
| hit | 12–18 帧 | 受击后仰 |
| death | 40–60 帧 | 倒地；可停在最后一帧 |
| sit / talk / meditate | 循环 | 安全屋/对话/入定 |
| aim / reload | 循环/一次性 | 枪械 |

- 关闭 Root Motion；位移由 CharacterController 驱动。
- 手部留 `WeaponPivot`/hand socket；枪械/近战模型挂上去。
- 没有 Animator 时，灰盒会程序化摆腿/手臂；所以可以先不做动画资产，代码行为已具备。

## 4. 材质与贴图

| 类别 | 分辨率 | 说明 |
| --- | --- | --- |
| 建筑 trim sheet | 2048×2048 | 墙/地板/屋顶共用一张，减少材质数 |
| 英雄角色 | 2048×2048 | albedo + normal + mask |
| 普通道具 | 1024×1024 | 可用图集 |
| 小道具/杂物 | 512×512 | |
| UI 图标 | 128×128 或 256×256 | 透明 PNG |

- URP Lit（Metallic/Smoothness）；金属度用 mask 贴图，不要全图统一。
- sRGB：albedo/emission 开；normal/mask 关。
- 不要烘焙大范围 AO 到墙面（会与动态光照打架）；局部污渍可以。
- 发光（窗、灵脉、路灯）用 Emission，HDR 强度 1–4。
- 手机 UI 不用 3D 材质。

## 5. 道具

| 道具 | 尺寸 (m) | 备注 |
| --- | --- | --- |
| 汽车 | 1.9 × 1.6 × 4.0 | 碰撞用 BoxCollider，不要用 MeshCollider |
| 树 | 3–6m 高 | 树干碰撞用 Capsule |
| 路灯 | 4m 高 | 灯泡 emissive + Point Light |
| 路障/垃圾桶 | 1m 级 | |
| 灵脉石龛 | 2–3m | 发光材质 + 点光 |

灰盒中的车/树/路灯都在 `WorldBuilder` 里程序化生成；正式资产建议做成 Prefab，再让 `WorldBuilder` 从 `ScriptableObject` 列表实例化。

## 5.5 音频素材（声源系统）

当前 `AudioDirector` 会运行时合成占位音效；正式替换时按下面的键名/时长准备，玩法中的噪声半径不变：

| 键 | 时长 | 类型 |
| --- | --- | --- |
| gunshot | 0.15–0.3s | 枪声，带尾音 |
| melee | 0.1–0.2s | 挥击/命中 |
| footstep | 0.06–0.12s | 脚步，2–3 个变体 |
| bite | 0.2–0.4s | 撕咬 |
| reload | 0.3–0.6s | 弹匣 |
| pickup | 0.1–0.2s | 拾取 |
| ui | 0.05–0.1s | 按钮 |
| zombie | 0.3–0.6s | 低吼/环境 |

导入：Force To Mono 视情况；短音效 Decompress On Load；长环境流式。3D 音效使用 Spatial Blend 1，枪声最大距离 45m。

## 6. 手机 UI 素材

Unity 里手机 UI 是 **UGUI 程序化生成**，不依赖图片也能显示。正式替换时按下面规格：

| 资产 | 尺寸 | 说明 |
| --- | --- | --- |
| 手机壁纸 | 1170×2532 | 9:19.5，深色、低对比，避免抢文字 |
| App 图标 | 128×128 | 9 个：任务/修行/背包/地图/行迹/交易/消息/设置（+ 预留） |
| Dock 图标 | 128×128 | 可与 App 图标复用 |
| 状态栏图标 | 64×64 | 信号、WiFi、电池；单色白 |
| 手机边框 | 9-slice 390×844 | 可选；当前用 UGUI 圆角 + 颜色绘制 |
| 字体 | TTF/OTF | 必须含常用汉字；建议思源黑体/Noto Sans SC 子集 |

设计尺寸：**390 × 844**（逻辑像素），安全区顶部 44、底部 34；手机边框 10；屏幕圆角 45；App 图标 58，圆角 16；Dock 高 76；状态栏高 44。

替换方式：

- 图标：`PhoneUI.BuildHome()` 中 `Icon` 面板改为 `Image` + Sprite；`AppEntry` 增加 `Sprite` 字段。
- 枪械/弹药/衣物/晶核图标：128×128，沿用 `icons/items/<id>.png` 命名（手枪/猎枪/霰弹枪/三种弹药/皮夹克/防弹背心/警帽/晶核）。
- 壁纸：`PhoneUI.BuildWallpaper` 的渐变 Sprite → 正式壁纸。
- 字体：`UiFactory.Font` 改为 `Resources.Load<Font>("Fonts/YourCJKFont")`。
- 保持 390×844 的布局不变；手机框在不同分辨率下由 CanvasScaler 缩放。

## 7. 导入设置

模型（FBX/GLB）：

- Scale Factor：1（Blender 导出单位为米）
- Convert Units：开
- Generate Colliders：仅对静态墙/地板；角色/道具用代码加 Collider
- Read/Write：关
- Mesh Compression：Off（小项目不必）
- Material Creation Mode：Standard / Import via MaterialDescription
- Animation Type：Humanoid；根运动 Bake Into Pose 关

贴图：

- Texture Type：Default（不是 Sprite）
- sRGB 按用途
- Max Size 按上表
- Compression：Desktop ASTC/BC7；移动 ASTC
- Mip Maps：开（3D）

UI 贴图：

- Texture Type：Sprite (2D and UI)
- Mesh Type：Full Rect
- Pixels Per Unit：100
- Filter：Bilinear
- Compression：高质量/无损

## 8. 性能预算

| 项 | 目标 |
| --- | --- |
| 可见三角面 | < 250k（中端机） |
| Draw Call | < 180（用 trim sheet + 图集） |
| 角色数 | 20–30（原型 8） |
| 实时阴影 | 1 个方向光 + 手电 Spot；室内点光不投影 |
| 手机 UI | < 150 个 UGUI Graphic；列表滚动复用 |
| 纹理内存 | < 512MB（桌面）/ < 256MB（移动） |

## 9. AI 生成工作流

### 9.1 建筑/道具（3D）

推荐 Meshy / Tripo / Rodin：

```
low-poly modular post-apocalyptic building wall section, 1m wide x 3m tall x 0.2m thick,
weathered plaster and wood trim, game-ready, PBR, 1m grid, no baked lighting,
front view, orthographic reference, isolated on transparent background
```

负面：`high poly, baked shadows, perspective, scene, multiple objects, text, logo`

生成后：

1. 在 Blender 里把 pivot 移到墙底中心、尺寸归一到 1×3×0.2；
2. 检查 UV 与 trim sheet 对齐；
3. 导出 FBX，Scale 1；
4. 用 **同一套 trim sheet** 给所有模块，控制材质数量。

### 9.2 角色（3D）

```
post-apocalyptic survivor, low-poly game character, 1.8m tall, jade-green worn jacket,
dark pants, backpack, hand-painted PBR textures, T-pose, humanoid rig, game-ready,
front view, neutral lighting, no background
```

生成后用 Mixamo 自动绑定/动画；或使用 Unity 商店的 Humanoid 角色。检查脚底 y=0、正面 +Z。

### 9.3 贴图

```
seamless PBR texture, weathered painted plaster wall, 2048x2048, albedo, muted gray-green,
post-apocalyptic grime, tileable, no lighting, no shadow, top-down flat
```

### 9.4 手机壁纸/图标

```
dark moody smartphone wallpaper, ruined jade mountains and faint spirit glow,
9:19.5, 1170x2532, low contrast center, no text, no icons
```

图标：统一 128×128，透明背景，扁平/微拟物，中心构图；同一套色板（jade / gold / qi / danger）。

## 10. 替换检查清单

- [ ] 1m 网格对齐，墙高 3m，门洞 1.7×2.3
- [ ] 墙/屋顶 pivot 在底部中心
- [ ] 屋顶、南墙支持透明淡出
- [ ] 角色脚底 y=0、正面 +Z、Humanoid
- [ ] 贴图 sRGB/法线正确，无烘焙大阴影
- [ ] 建筑不遮挡相机时不会穿帮
- [ ] 手机 UI 在 390×844 下无溢出，CJK 字体不缺字
- [ ] 场景 Draw Call / 三角面在预算内
- [ ] 替换后 `./tools/unity-check/check.sh` 仍通过（若改了 C#）

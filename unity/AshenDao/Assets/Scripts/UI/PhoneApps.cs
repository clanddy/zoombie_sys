using UnityEngine;
using UnityEngine.UI;
using AshenDao.Game;
using AshenDao.Sim;

namespace AshenDao.UI
{
    public partial class PhoneUI
    {
        private Texture2D mapTexture;

        private string TitleFor(PhoneApp app)
        {
            switch (app)
            {
                case PhoneApp.Quests: return "天机碑";
                case PhoneApp.Cultivation: return "修行";
                case PhoneApp.Inventory: return "行囊";
                case PhoneApp.Map: return "地图";
                case PhoneApp.Stats: return "行迹";
                case PhoneApp.Trade: return "交易";
                case PhoneApp.Messages: return "消息";
                case PhoneApp.Settings: return "设置";
                default: return "手机";
            }
        }

        private void BuildApp(PhoneApp app)
        {
            UiFactory.Clear(appRoot);
            BuildHeader(app);

            switch (app)
            {
                case PhoneApp.Quests: BuildQuestsApp(); break;
                case PhoneApp.Cultivation: BuildCultivationApp(); break;
                case PhoneApp.Inventory: BuildInventoryApp(); break;
                case PhoneApp.Map: BuildMapApp(); break;
                case PhoneApp.Stats: BuildStatsApp(); break;
                case PhoneApp.Trade: BuildTradeApp(); break;
                case PhoneApp.Messages: BuildMessagesApp(); break;
                case PhoneApp.Settings: BuildSettingsApp(); break;
            }
        }

        private void BuildHeader(PhoneApp app)
        {
            var back = UiFactory.Button(appRoot, "Back", "<", 24, new Vector2(38f, 38f),
                new Vector2(4f, 6f), GoHome, new Color(1f, 1f, 1f, 0.06f), PhoneTheme.Text);
            back.GetComponent<Image>().raycastTarget = true;

            var title = UiFactory.Label(appRoot, "AppTitle", TitleFor(app), 21, PhoneTheme.Text,
                TextAnchor.MiddleLeft, false);
            UiFactory.Place(title, 50f, 10f, 220f, 30f);

            var refresh = UiFactory.Button(appRoot, "Refresh", "刷", 14, new Vector2(34f, 34f),
                new Vector2(310f, 8f), () => BuildApp(current), new Color(1f, 1f, 1f, 0.06f), PhoneTheme.Jade);
            refresh.GetComponent<Image>().raycastTarget = true;

            var line = UiFactory.Panel(appRoot, "HeaderLine", new Vector2(342f, 1f), new Vector2(4f, 50f),
                new Color(1f, 1f, 1f, 0.09f), 0f);
            line.GetComponent<Image>().raycastTarget = false;
        }

        private void BuildScroll(out RectTransform content)
        {
            UiFactory.Scroll(appRoot, 4f, 58f, 342f, 678f, out _, out content);
        }

        private void BuildQuestsApp()
        {
            BuildScroll(out var content);
            var active = sim.Content.Quests.FindAll(q => sim.Quests.Status(q) == QuestStatus.Active);
            var available = sim.Content.Quests.FindAll(q => sim.Quests.Status(q) == QuestStatus.Available);
            var completed = sim.Content.Quests.FindAll(q => sim.Quests.Status(q) == QuestStatus.Completed);

            AddSectionLabel(content, "进行中 · " + active.Count, active.Count == 0 ? 24f : 22f);
            if (active.Count == 0)
            {
                AddNoteCard(content, "没有进行中的委托。去「可接取」看看。", 56f);
            }
            foreach (var def in active)
            {
                var runtime = sim.Quests.Runtime(def.Id);
                var card = UiFactory.Card(content, 126f);
                AddCardTitle(card, def.Title, def.Category, 14f);
                var summary = UiFactory.Label(card, "Summary", def.Summary, 10, PhoneTheme.Muted,
                    TextAnchor.UpperLeft, true);
                UiFactory.Place(summary, 14f, 38f, 304f, 30f);
                var lines = "";
                for (var i = 0; i < def.Objectives.Count; i++)
                {
                    var obj = def.Objectives[i];
                    var currentValue = runtime != null && runtime.Progress != null && i < runtime.Progress.Length
                        ? runtime.Progress[i]
                        : 0f;
                    lines += (i > 0 ? "\n" : "") + sim.Quests.ObjectiveText(obj) + "  " +
                             Mathf.FloorToInt(currentValue) + "/" + Mathf.RoundToInt(sim.Quests.Target(obj));
                }
                var objectives = UiFactory.Label(card, "Objectives", lines, 10, PhoneTheme.Text,
                    TextAnchor.UpperLeft, true);
                UiFactory.Place(objectives, 14f, 68f, 300f, 38f);

                var total = 0f;
                var currentTotal = 0f;
                for (var i = 0; i < def.Objectives.Count; i++)
                {
                    total += sim.Quests.Target(def.Objectives[i]);
                    currentTotal += runtime != null && runtime.Progress != null && i < runtime.Progress.Length
                        ? runtime.Progress[i]
                        : 0f;
                }
                UiFactory.Bar(card, "Progress", 14f, 112f, 300f, 5f, PhoneTheme.Jade,
                    total <= 0f ? 0f : currentTotal / total);
            }

            AddSectionLabel(content, "可接取 · " + available.Count, 24f);
            if (available.Count == 0) AddNoteCard(content, "暂时没有新的委托。", 52f);
            foreach (var def in available)
            {
                var card = UiFactory.Card(content, 128f);
                AddCardTitle(card, def.Title, def.Category, 14f);
                var summary = UiFactory.Label(card, "Summary", def.Summary, 10, PhoneTheme.Muted,
                    TextAnchor.UpperLeft, true);
                UiFactory.Place(summary, 14f, 38f, 300f, 34f);
                var rewards = "";
                for (var i = 0; i < def.Rewards.Count; i++)
                {
                    rewards += (i > 0 ? " · " : "") + sim.Quests.RewardText(def.Rewards[i]);
                }
                var rewardLabel = UiFactory.Label(card, "Rewards", rewards, 10, PhoneTheme.Gold,
                    TextAnchor.UpperLeft, true);
                UiFactory.Place(rewardLabel, 14f, 76f, 210f, 36f);
                var accept = UiFactory.Button(card, "Accept", "接取", 11, new Vector2(72f, 32f),
                    new Vector2(232f, 86f), () =>
                    {
                        if (sim.AcceptQuest(def.Id)) BuildApp(PhoneApp.Quests);
                    });
                accept.GetComponent<Image>().raycastTarget = true;
            }

            var locked = sim.Content.Quests.FindAll(q => sim.Quests.Status(q) == QuestStatus.Locked);
            if (locked.Count > 0)
            {
                AddNoteCard(content, "尚未解锁：" + string.Join("、", locked.ConvertAll(q => q.Title)), 34f);
            }

            AddSectionLabel(content, "已完成 · " + completed.Count, 24f);
            foreach (var def in completed)
            {
                var card = UiFactory.Card(content, 52f);
                var label = UiFactory.Label(card, "Done", def.Title + "  ·  已完成", 11, PhoneTheme.Muted,
                    TextAnchor.MiddleLeft, false);
                UiFactory.Place(label, 14f, 14f, 300f, 24f);
            }

            Canvas.ForceUpdateCanvases();
        }

        private void BuildCultivationApp()
        {
            BuildScroll(out var content);
            var player = sim.Player;
            var realm = sim.Realm;

            var card = UiFactory.Card(content, 174f);
            var realmLabel = UiFactory.Label(card, "Realm", realm.Name, 26, PhoneTheme.Jade,
                TextAnchor.UpperLeft, false);
            UiFactory.Place(realmLabel, 16f, 12f, 180f, 36f);
            var stage = UiFactory.Label(card, "Stage", "第 " + player.Stage + " / " + realm.Stages + " 层", 11,
                PhoneTheme.Muted, TextAnchor.UpperRight, false);
            UiFactory.Place(stage, 190f, 20f, 128f, 20f);

            var xpNeed = sim.XpNeeded;
            var xpLabel = UiFactory.Label(card, "XpLabel", "修为  " + Mathf.FloorToInt(player.Xp) + " / " +
                                                               Mathf.RoundToInt(xpNeed), 10, PhoneTheme.Muted,
                TextAnchor.UpperLeft, false);
            UiFactory.Place(xpLabel, 16f, 58f, 280f, 18f);
            UiFactory.Bar(card, "Xp", 16f, 78f, 286f, 6f, PhoneTheme.Gold, xpNeed <= 0f ? 0f : player.Xp / xpNeed);

            var qiLabel = UiFactory.Label(card, "QiLabel", "灵气  " + Mathf.FloorToInt(player.Qi) + " / " +
                                                               Mathf.FloorToInt(sim.QiMax), 10, PhoneTheme.Muted,
                TextAnchor.UpperLeft, false);
            UiFactory.Place(qiLabel, 16f, 94f, 280f, 18f);
            UiFactory.Bar(card, "Qi", 16f, 114f, 286f, 6f, PhoneTheme.Qi, player.Qi / Mathf.Max(1f, sim.QiMax));

            var dao = UiFactory.Label(card, "Dao", "道心 " + Mathf.FloorToInt(player.DaoHeart) +
                                                      "    悟性 " + player.Insight + "    灵脉 " +
                                                      (string.IsNullOrEmpty(sim.VeinName) ? "无" : sim.VeinName),
                10, PhoneTheme.Muted, TextAnchor.UpperLeft, false);
            UiFactory.Place(dao, 16f, 136f, 300f, 20f);

            var breakCard = UiFactory.Card(content, 158f);
            var breakTitle = UiFactory.Label(breakCard, "BreakTitle", "破境", 18, PhoneTheme.Text,
                TextAnchor.UpperLeft, false);
            UiFactory.Place(breakTitle, 16f, 12f, 120f, 26f);
            var chance = UiFactory.Label(breakCard, "Chance", "成功率 " + Mathf.RoundToInt(sim.BreakthroughChance() * 100f) + "%",
                11, PhoneTheme.Gold, TextAnchor.UpperRight, false);
            UiFactory.Place(chance, 190f, 16f, 130f, 20f);

            var blockers = sim.BreakthroughBlockers();
            var blockerText = blockers.Count == 0 ? "条件已满足，可以尝试破境。" : string.Join("\n", blockers);
            var blockerLabel = UiFactory.Label(breakCard, "Blockers", blockerText, 10,
                blockers.Count == 0 ? PhoneTheme.Jade : PhoneTheme.Danger, TextAnchor.UpperLeft, true);
            UiFactory.Place(blockerLabel, 16f, 44f, 300f, 58f);

            var breakButton = UiFactory.Button(breakCard, "Break", "尝试破境", 12, new Vector2(300f, 34f),
                new Vector2(16f, 112f), () =>
                {
                    sim.AttemptBreakthrough();
                    BuildApp(PhoneApp.Cultivation);
                }, PhoneTheme.Jade, new Color(0.03f, 0.07f, 0.06f));
            breakButton.interactable = blockers.Count == 0;
            breakButton.GetComponent<Image>().raycastTarget = true;

            AddSectionLabel(content, "功法与术法", 26f);
            foreach (var id in sim.Player.Techniques)
            {
                var tech = sim.Content.Technique(id);
                if (tech == null) continue;
                var level = sim.TechniqueLevel(id);
                var techCard = UiFactory.Card(content, 118f);
                var icon = UiFactory.Panel(techCard, "Icon", new Vector2(48f, 48f), new Vector2(12f, 14f),
                    tech.Kind == "active" ? new Color(0.15f, 0.35f, 0.38f) : new Color(0.15f, 0.3f, 0.24f), 12f);
                var glyph = UiFactory.Label(icon, "Glyph", tech.Kind == "active" ? "术" : "功", 20,
                    tech.Kind == "active" ? PhoneTheme.Qi : PhoneTheme.Jade, TextAnchor.MiddleCenter, false);
                glyph.rectTransform.anchorMin = Vector2.zero;
                glyph.rectTransform.anchorMax = Vector2.one;
                glyph.rectTransform.offsetMin = Vector2.zero;
                glyph.rectTransform.offsetMax = Vector2.zero;

                var name = UiFactory.Label(techCard, "Name", tech.Name + "  Lv." + level + " / " + tech.MaxLevel,
                    14, PhoneTheme.Text, TextAnchor.UpperLeft, false);
                UiFactory.Place(name, 72f, 12f, 220f, 22f);
                var meta = tech.Kind == "active"
                    ? "灵气 " + tech.QiCost + " · 冷却 " + tech.Cooldown + "s" +
                      (tech.Active != null && tech.Active.Kind == "movement" ? " · 身法" : "")
                    : "被动 · 常驻生效";
                var metaLabel = UiFactory.Label(techCard, "Meta", meta, 9, PhoneTheme.Muted,
                    TextAnchor.UpperLeft, false);
                UiFactory.Place(metaLabel, 72f, 34f, 240f, 16f);
                var desc = UiFactory.Label(techCard, "Desc", tech.Description, 9, PhoneTheme.Muted,
                    TextAnchor.UpperLeft, true);
                UiFactory.Place(desc, 72f, 50f, 250f, 30f);
                if (tech.Kind == "active")
                {
                    var cast = UiFactory.Button(techCard, "Cast", "施展", 9, new Vector2(70f, 28f),
                        new Vector2(128f, 82f), () =>
                        {
                            sim.CastTechnique(id);
                            BuildApp(PhoneApp.Cultivation);
                        }, PhoneTheme.CardLight, PhoneTheme.Qi);
                    cast.interactable = sim.Player.Qi >= tech.QiCost;
                    cast.GetComponent<Image>().raycastTarget = true;
                }
                var cost = sim.TechniqueUpgradeCost(id);
                var upgrade = UiFactory.Button(techCard, "Upgrade",
                    level >= tech.MaxLevel ? "圆满" : "升级 · " + cost + " 悟性", 9, new Vector2(116f, 28f),
                    new Vector2(206f, 82f), () =>
                    {
                        if (sim.UpgradeTechnique(id)) BuildApp(PhoneApp.Cultivation);
                    }, PhoneTheme.CardLight, PhoneTheme.Jade);
                upgrade.interactable = level < tech.MaxLevel && player.Insight >= cost;
                upgrade.GetComponent<Image>().raycastTarget = true;
            }

            var meditate = UiFactory.Button(content, "Meditate",
                sim.Meditating ? "收功" : "盘膝入定", 12, new Vector2(320f, 36f), new Vector2(6f, 6f),
                () =>
                {
                    sim.ToggleMeditate();
                    BuildApp(PhoneApp.Cultivation);
                }, sim.Meditating ? PhoneTheme.CardLight : PhoneTheme.Jade,
                sim.Meditating ? PhoneTheme.Text : new Color(0.03f, 0.07f, 0.06f));
            meditate.GetComponent<Image>().raycastTarget = true;
            SetLayoutHeight(meditate.gameObject, 36f);
            var spacer = UiFactory.Card(content, 44f);
            spacer.GetComponent<LayoutElement>().preferredHeight = 44f;

            Canvas.ForceUpdateCanvases();
        }

        private void BuildInventoryApp()
        {
            BuildScroll(out var content);
            AddSectionLabel(content, "负重 " + sim.InventoryWeight().ToString("0.0") + " / " + sim.Player.MaxWeight,
                24f);

            if (sim.Player.Inventory.Count == 0)
            {
                AddNoteCard(content, "行囊空空。", 60f);
            }

            foreach (var stack in sim.Player.Inventory.ToArray())
            {
                var def = sim.Content.Item(stack.ItemId);
                if (def == null) continue;
                var card = UiFactory.Card(content, 66f);
                var color = CategoryColor(def.Category);
                var icon = UiFactory.Panel(card, "Icon", new Vector2(44f, 44f), new Vector2(12f, 11f), color, 10f);
                var glyph = UiFactory.Label(icon, "Glyph", def.Name.Substring(0, 1), 18,
                    new Color(0.04f, 0.07f, 0.06f), TextAnchor.MiddleCenter, false);
                glyph.rectTransform.anchorMin = Vector2.zero;
                glyph.rectTransform.anchorMax = Vector2.one;
                glyph.rectTransform.offsetMin = Vector2.zero;
                glyph.rectTransform.offsetMax = Vector2.zero;

                var name = UiFactory.Label(card, "Name", def.Name + (sim.Player.Equipped == def.Id ? " · 已装备" : ""),
                    12, PhoneTheme.Text, TextAnchor.UpperLeft, false);
                UiFactory.Place(name, 66f, 10f, 180f, 22f);
                var meta = UiFactory.Label(card, "Meta", "×" + stack.Count + " · " + (def.Weight * stack.Count).ToString("0.0") + "kg",
                    9, PhoneTheme.Muted, TextAnchor.UpperLeft, false);
                UiFactory.Place(meta, 66f, 34f, 170f, 16f);

                var action = def.Weapon != null ? "装备" :
                    !string.IsNullOrEmpty(def.Slot) ? "穿戴" :
                    def.Use.Count > 0 ? "使用" : "";
                if (!string.IsNullOrEmpty(action))
                {
                    var id = def.Id;
                    var button = UiFactory.Button(card, "Use", action, 10, new Vector2(64f, 30f),
                        new Vector2(264f, 18f), () =>
                        {
                            if (def.Weapon != null || !string.IsNullOrEmpty(def.Slot))
                            {
                                sim.EquipItem(id);
                            }
                            else
                            {
                                sim.UseItem(id);
                            }
                            BuildApp(PhoneApp.Inventory);
                        });
                    button.GetComponent<Image>().raycastTarget = true;
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        private void BuildMapApp()
        {
            var mapRect = UiFactory.NewRect(appRoot, "MapImage");
            UiFactory.Place(mapRect, 18f, 64f, 314f, 314f);
            var raw = mapRect.gameObject.AddComponent<RawImage>();
            raw.texture = BuildMapTexture();
            raw.color = Color.white;
            raw.raycastTarget = false;

            var legend = UiFactory.Card(appRoot, 80f);
            UiFactory.Place((RectTransform)legend.transform, 4f, 390f, 342f, 80f);
            var text = UiFactory.Label(legend, "Legend",
                "白点：你 · 红点：附近尸群 · 青圈：灵脉\n矩形：建筑 · 灰线：道路\n地图仅显示 20 米内活动目标。",
                10, PhoneTheme.Muted, TextAnchor.UpperLeft, true);
            UiFactory.Place(text, 12f, 10f, 316f, 62f);
        }

        private Texture2D BuildMapTexture()
        {
            const int size = 256;
            if (mapTexture == null)
            {
                mapTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                mapTexture.wrapMode = TextureWrapMode.Clamp;
                mapTexture.filterMode = FilterMode.Point;
            }
            var pixels = new Color32[size * size];
            var background = new Color32(8, 17, 15, 255);
            for (var i = 0; i < pixels.Length; i++) pixels[i] = background;

            var worldSize = 64f;
            var center = new Vector2(0f, 0f);
            void Pixel(int x, int y, Color32 color)
            {
                if (x < 0 || y < 0 || x >= size || y >= size) return;
                pixels[y * size + x] = color;
            }

            void Rect(Vector2 worldCenter, Vector2 worldSize2, Color32 color)
            {
                var x0 = Mathf.RoundToInt((worldCenter.x - worldSize2.x * 0.5f - center.x) / worldSize * size + size * 0.5f);
                var x1 = Mathf.RoundToInt((worldCenter.x + worldSize2.x * 0.5f - center.x) / worldSize * size + size * 0.5f);
                var y0 = Mathf.RoundToInt((worldCenter.y - worldSize2.y * 0.5f - center.y) / worldSize * size + size * 0.5f);
                var y1 = Mathf.RoundToInt((worldCenter.y + worldSize2.y * 0.5f - center.y) / worldSize * size + size * 0.5f);
                for (var y = y0; y <= y1; y++)
                for (var x = x0; x <= x1; x++)
                    Pixel(x, y, color);
            }

            var road = new Color32(52, 56, 55, 255);
            Rect(new Vector2(0f, 0f), new Vector2(64f, 7f), road);
            Rect(new Vector2(0f, 0f), new Vector2(7f, 64f), road);

            foreach (var building in root.World.MapBuildings)
            {
                Rect(building.Center, building.Size, new Color32(42, 58, 50, 255));
                Rect(building.Center, building.Size - new Vector2(1.2f, 1.2f), new Color32(68, 88, 74, 255));
            }

            if (root.World.Vein != null)
            {
                var vein = root.World.Vein.transform.position;
                var vx = Mathf.RoundToInt(vein.x / worldSize * size + size * 0.5f);
                var vy = Mathf.RoundToInt(vein.z / worldSize * size + size * 0.5f);
                for (var y = -5; y <= 5; y++)
                for (var x = -5; x <= 5; x++)
                    if (x * x + y * y <= 25) Pixel(vx + x, vy + y, new Color32(70, 200, 170, 255));
            }

            foreach (var zombie in root.World.Zombies)
            {
                if (zombie == null) continue;
                var delta = zombie.transform.position - root.Player.transform.position;
                if (delta.sqrMagnitude > 20f * 20f) continue;
                var zx = Mathf.RoundToInt(zombie.transform.position.x / worldSize * size + size * 0.5f);
                var zy = Mathf.RoundToInt(zombie.transform.position.z / worldSize * size + size * 0.5f);
                for (var y = -2; y <= 2; y++)
                for (var x = -2; x <= 2; x++)
                    if (x * x + y * y <= 4) Pixel(zx + x, zy + y, new Color32(210, 80, 70, 255));
            }

            var px = Mathf.RoundToInt(root.Player.transform.position.x / worldSize * size + size * 0.5f);
            var py = Mathf.RoundToInt(root.Player.transform.position.z / worldSize * size + size * 0.5f);
            for (var y = -3; y <= 3; y++)
            for (var x = -3; x <= 3; x++)
                if (x * x + y * y <= 9) Pixel(px + x, py + y, new Color32(240, 236, 215, 255));

            mapTexture.SetPixels32(pixels);
            mapTexture.Apply();
            return mapTexture;
        }

        private void BuildStatsApp()
        {
            BuildScroll(out var content);
            var stats = sim.Stats;

            var summary = UiFactory.Card(content, 142f);
            var title = UiFactory.Label(summary, "Title", "本次行迹", 18, PhoneTheme.Jade,
                TextAnchor.UpperLeft, false);
            UiFactory.Place(title, 16f, 12f, 260f, 28f);
            var time = UiFactory.Label(summary, "Time",
                "存活  " + stats.SurvivalText + "\n游戏内  " + stats.GameTimeText, 12, PhoneTheme.Text,
                TextAnchor.UpperLeft, true);
            UiFactory.Place(time, 16f, 44f, 300f, 48f);
            var kills = UiFactory.Label(summary, "Kills",
                "击杀 " + stats.Kills + "   爆头 " + stats.Headshots + "   任务 " + stats.QuestsCompleted,
                11, PhoneTheme.Gold, TextAnchor.UpperLeft, false);
            UiFactory.Place(kills, 16f, 100f, 310f, 20f);

            var combat = UiFactory.Card(content, 118f);
            AddCardTitle(combat, "战斗与声音", "stats", 14f);
            var combatText = UiFactory.Label(combat, "Combat",
                "射击 " + stats.Shots + "   换弹 " + stats.Reloads + "   术法 " + stats.TechniqueCasts +
                "\n造成伤害 " + stats.DamageDealt + "   承受伤害 " + stats.DamageTaken +
                "\n制造声响 " + stats.Noises + "   对话 " + stats.Dialogues + "   交易 " + stats.Trades,
                10, PhoneTheme.Text, TextAnchor.UpperLeft, true);
            UiFactory.Place(combatText, 14f, 40f, 310f, 66f);

            var loot = UiFactory.Card(content, 92f);
            AddCardTitle(loot, "搜刮与移动", "stats", 14f);
            var lootText = UiFactory.Label(loot, "Loot",
                "拾取 " + stats.ItemsLooted + "   晶核 " + sim.CountItem("crystal_core") +
                "\n步行 " + (int)stats.DistanceWalked + "m   疾行 " + (int)stats.DistanceSprinted + "m",
                10, PhoneTheme.Text, TextAnchor.UpperLeft, true);
            UiFactory.Place(lootText, 14f, 40f, 310f, 44f);

            AddSectionLabel(content, "击杀分类", 24f);
            var kinds = new[] { "walker", "runner", "police", "soldier", "hunter", "brute" };
            var names = new[] { "行尸", "疾行尸", "警尸", "军尸", "猎尸", "巨尸" };
            for (var i = 0; i < kinds.Length; i++)
            {
                var count = stats.Get("kill." + kinds[i]);
                if (count <= 0) continue;
                var row = UiFactory.Card(content, 38f);
                var label = UiFactory.Label(row, "Row", names[i], 11, PhoneTheme.Text,
                    TextAnchor.MiddleLeft, false);
                UiFactory.Place(label, 14f, 9f, 180f, 20f);
                var value = UiFactory.Label(row, "Count", "× " + count, 11, PhoneTheme.Gold,
                    TextAnchor.MiddleRight, false);
                UiFactory.Place(value, 240f, 9f, 80f, 20f);
            }

            AddSectionLabel(content, "事件记录", 24f);
            var start = Mathf.Max(0, stats.Events.Count - 14);
            if (stats.Events.Count == 0)
            {
                AddNoteCard(content, "还没有值得记录的事。", 48f);
            }
            for (var i = stats.Events.Count - 1; i >= start; i--)
            {
                var evt = stats.Events[i];
                var row = UiFactory.Card(content, 44f);
                var label = UiFactory.Label(row, "Event",
                    "第 " + evt.Day + " 天 " + stats.HhMm(evt.Minute) + "  ·  " + evt.Text,
                    9, PhoneTheme.Muted, TextAnchor.UpperLeft, true);
                UiFactory.Place(label, 12f, 8f, 312f, 30f);
            }
            Canvas.ForceUpdateCanvases();
        }

        private void BuildTradeApp()
        {
            BuildScroll(out var content);
            var trader = sim.Trade.Trader("trader_system");
            var card = UiFactory.Card(content, 120f);
            var title = UiFactory.Label(card, "Title", trader != null ? trader.Name : "天机碑 · 石臼",
                20, PhoneTheme.Jade, TextAnchor.UpperLeft, false);
            UiFactory.Place(title, 16f, 12f, 300f, 28f);
            var greeting = UiFactory.Label(card, "Greeting",
                trader != null ? trader.Greeting : "晶核入臼，等价交换。", 10, PhoneTheme.Muted,
                TextAnchor.UpperLeft, true);
            UiFactory.Place(greeting, 16f, 44f, 300f, 38f);
            var currency = UiFactory.Label(card, "Currency",
                "持有晶核  " + sim.CountItem("crystal_core") + "    ·    旧币 " + sim.Wallet.Coin,
                11, PhoneTheme.Gold, TextAnchor.UpperLeft, false);
            UiFactory.Place(currency, 16f, 88f, 310f, 20f);

            var open = UiFactory.Button(content, "Open", "打开天机碑商栈", 13, new Vector2(318f, 44f),
                new Vector2(0f, 0f), () => root.OpenTrade("trader_system"), PhoneTheme.CardLight, PhoneTheme.Jade);
            open.GetComponent<Image>().raycastTarget = true;

            AddSectionLabel(content, "今日货单", 24f);
            if (trader != null)
            {
                foreach (var offer in trader.Offers)
                {
                    var item = sim.Content.Item(offer.ItemId);
                    if (item == null) continue;
                    var row = UiFactory.Card(content, 56f);
                    var name = UiFactory.Label(row, "Name", item.Name, 12, PhoneTheme.Text,
                        TextAnchor.MiddleLeft, false);
                    UiFactory.Place(name, 14f, 8f, 190f, 20f);
                    var price = UiFactory.Label(row, "Price",
                        sim.Trade.BuyPrice(trader, offer) + " " + sim.Trade.CurrencyName(trader.Currency),
                        10, PhoneTheme.Gold, TextAnchor.MiddleRight, false);
                    UiFactory.Place(price, 210f, 8f, 110f, 20f);
                    var stock = UiFactory.Label(row, "Stock", "库存 " + sim.Trade.StockOf(trader, offer),
                        9, PhoneTheme.Muted, TextAnchor.UpperLeft, false);
                    UiFactory.Place(stock, 14f, 30f, 200f, 18f);
                }
            }
            AddNoteCard(content, "与 NPC 交易：靠近商人按 E 交谈，在对话中选择「看看货」。", 54f);
            Canvas.ForceUpdateCanvases();
        }

        private void BuildMessagesApp()
        {
            BuildScroll(out var content);
            AddSectionLabel(content, "从废土收到的消息", 24f);
            var notices = sim.Notices;
            for (var i = notices.Count - 1; i >= 0; i--)
            {
                var notice = notices[i];
                var card = UiFactory.Card(content, 62f);
                var toneColor = notice.Tone == (int)NoticeTone.Good ? PhoneTheme.Jade :
                    notice.Tone == (int)NoticeTone.Bad ? PhoneTheme.Danger :
                    notice.Tone == (int)NoticeTone.Warn ? PhoneTheme.Gold : PhoneTheme.Muted;
                var time = UiFactory.Label(card, "Time", "第" + notice.Day + "天 " + notice.Clock, 9,
                    toneColor, TextAnchor.UpperLeft, false);
                UiFactory.Place(time, 12f, 8f, 180f, 16f);
                var text = UiFactory.Label(card, "Text", notice.Text, 10, PhoneTheme.Text,
                    TextAnchor.UpperLeft, true);
                UiFactory.Place(text, 12f, 26f, 310f, 32f);
            }
            if (notices.Count == 0) AddNoteCard(content, "还没有消息。", 56f);

            Canvas.ForceUpdateCanvases();
        }

        private void BuildSettingsApp()
        {
            BuildScroll(out var content);
            AddSectionLabel(content, "存档", 24f);

            var save = UiFactory.Button(content, "Save", "保存进度", 12, new Vector2(320f, 40f),
                new Vector2(6f, 6f), () =>
                {
                    root.Save();
                    BuildApp(PhoneApp.Settings);
                }, PhoneTheme.Jade, new Color(0.03f, 0.07f, 0.06f));
            save.GetComponent<Image>().raycastTarget = true;
            SetLayoutHeight(save.gameObject, 40f);

            var load = UiFactory.Button(content, "Load", "读取进度", 12, new Vector2(320f, 40f),
                new Vector2(6f, 6f), () =>
                {
                    root.Load();
                    BuildApp(PhoneApp.Settings);
                });
            load.GetComponent<Image>().raycastTarget = true;
            SetLayoutHeight(load.gameObject, 40f);

            AddSectionLabel(content, "操作", 26f);
            AddNoteCard(content,
                "WASD 移动 · Shift 疾行\n左键 攻击 · E 交互 · R 入定 · Q 术法\nTab 手机 · Esc 关闭 · F5/F9 存档/读档",
                82f);

            AddSectionLabel(content, "状态", 26f);
            AddNoteCard(content,
                "种子 1337 · 第 " + sim.Clock.Day + " 天 " + sim.Clock.ClockText + "\n" +
                "版本 0.2 unity · 联机：未开放\n" +
                "存档路径：Application.persistentDataPath",
                72f);

            AddSectionLabel(content, "关于", 26f);
            AddNoteCard(content,
                "劫余问道 Unity 原型。2.5D 俯视、可进入建筑、手机式 UI。",
                58f);

            Canvas.ForceUpdateCanvases();
        }

        private void AddSectionLabel(Transform parent, string text, float height)
        {
            var label = UiFactory.Label(parent, "Section", text, 10, PhoneTheme.Muted, TextAnchor.UpperLeft, false);
            var element = label.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            var rt = (RectTransform)label.transform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(6f, 0f);
            rt.offsetMax = new Vector2(-6f, 0f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = new Vector2(0f, 0f);
        }

        private void AddNoteCard(Transform parent, string text, float height)
        {
            var card = UiFactory.Card(parent, height);
            var label = UiFactory.Label(card, "Note", text, 10, PhoneTheme.Muted, TextAnchor.UpperLeft, true);
            UiFactory.Place(label, 14f, 12f, 306f, height - 18f);
        }

        private static void SetLayoutHeight(GameObject go, float height)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
        }

        private void AddCardTitle(RectTransform card, string title, string category, float y)
        {
            var titleLabel = UiFactory.Label(card, "Title", title, 15, PhoneTheme.Text, TextAnchor.UpperLeft, false);
            UiFactory.Place(titleLabel, 14f, y, 210f, 24f);
            var tag = UiFactory.Label(card, "Tag", CategoryName(category), 9, CategoryColor(category),
                TextAnchor.MiddleRight, false);
            UiFactory.Place(tag, 226f, y + 3f, 96f, 16f);
        }

        private static string CategoryName(string category)
        {
            switch (category)
            {
                case "main": return "主线";
                case "side": return "支线";
                case "cultivation": return "修行";
                case "survival": return "生存";
                default: return category;
            }
        }

        private static Color CategoryColor(string category)
        {
            switch (category)
            {
                case "main": return PhoneTheme.Gold;
                case "cultivation": return PhoneTheme.Qi;
                case "survival": return new Color(0.85f, 0.56f, 0.44f);
                case "side": return new Color(0.62f, 0.7f, 0.66f);
                case "food": return new Color(0.7f, 0.52f, 0.34f);
                case "drink": return new Color(0.4f, 0.62f, 0.75f);
                case "medical": return new Color(0.45f, 0.75f, 0.6f);
                case "weapon": return new Color(0.75f, 0.48f, 0.4f);
                case "material": return new Color(0.55f, 0.55f, 0.5f);
                case "pill": return new Color(0.65f, 0.55f, 0.8f);
                case "relic": return new Color(0.4f, 0.78f, 0.7f);
                default: return PhoneTheme.Muted;
            }
        }
    }
}

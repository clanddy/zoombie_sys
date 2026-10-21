using System;
using System.Collections.Generic;
using System.Linq;

namespace AshenDao.Sim
{
    [Serializable]
    public class ItemStack
    {
        public string ItemId;
        public int Count;

        public ItemStack() { }

        public ItemStack(string itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }

    public class StatMods
    {
        public float MaxHp;
        public float Armor;
        public float DamageMult = 1f;
        public float QiRegen = 1f;
        public float MeditationXp = 1f;
        public float Breakthrough;
        public float HungerRate = 1f;
        public float InfectionResist;
    }

    public class PlayerState
    {
        public float Hp = 115f;
        public float MaxHp = 115f;
        public float Stamina = 100f;
        public float MaxStamina = 100f;
        public float MaxWeight = 28f;

        public float Hunger = 14f;
        public float Thirst = 18f;
        public float Fatigue = 10f;
        public float Infection;
        public bool Bleeding;

        public int RealmIndex = 1;
        public int Stage = 1;
        public float Xp;
        public float Qi = 30f;
        public float DaoHeart = 30f;
        public int Insight;

        public bool Dead;
        public bool Meditating;
        public float MeditationMinutes;

        public float Shield;
        public float ShieldUntil;

        public float AttackReadyAt;
        public float StaminaRegenDelay;

        public readonly List<ItemStack> Inventory = new List<ItemStack>();
        public readonly HashSet<string> Techniques = new HashSet<string>();
        public readonly Dictionary<string, float> Cooldowns = new Dictionary<string, float>();
        public readonly Dictionary<string, int> TechniqueLevels = new Dictionary<string, int>();
        public readonly Dictionary<string, int> Magazines = new Dictionary<string, int>();
        public readonly Dictionary<string, string> Clothing = new Dictionary<string, string>();

        public string Equipped = "bat";

        public int Kills;
        public int Nights;
        public int ItemsCollected;
        public int TechniquesUsed;
        public int BreakthroughAttempts;
        public int BreakthroughFailures;
    }

    public class GameClock
    {
        public int Day = 1;
        public float MinuteOfDay = 480f;
        public float TotalMinutes = 480f;
        public bool DawnCrossed;

        public string Phase
        {
            get
            {
                if (MinuteOfDay < 300f) return "night";
                if (MinuteOfDay < 540f) return "dawn";
                if (MinuteOfDay < 1080f) return "day";
                if (MinuteOfDay < 1260f) return "dusk";
                return "night";
            }
        }

        public string ClockText
        {
            get
            {
                var total = (int)MinuteOfDay;
                return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
            }
        }

        public float Darkness
        {
            get
            {
                var m = MinuteOfDay;
                if (m < 300f) return 0.72f;
                if (m < 420f) return 0.72f - ((m - 300f) / 120f) * 0.62f;
                if (m < 1020f) return 0.1f;
                if (m < 1140f) return 0.1f + ((m - 1020f) / 120f) * 0.4f;
                if (m < 1260f) return 0.5f + ((m - 1140f) / 120f) * 0.22f;
                return 0.72f;
            }
        }

        public void Advance(float minutes)
        {
            DawnCrossed = false;
            var before = TotalMinutes;
            TotalMinutes += minutes;
            MinuteOfDay = TotalMinutes % 1440f;
            Day = (int)(TotalMinutes / 1440f) + 1;

            var prevDawn = (int)Math.Floor((before - 360f) / 1440f);
            var nextDawn = (int)Math.Floor((TotalMinutes - 360f) / 1440f);
            if (nextDawn > prevDawn) DawnCrossed = true;
        }
    }

    [Serializable]
    public class Notice
    {
        public int Id;
        public string Text;
        public int Tone;
        public string Clock;
        public int Day;
    }

    public partial class WorldSim
    {
        public readonly ContentDatabase Content;
        public readonly PlayerState Player = new PlayerState();
        public readonly GameClock Clock = new GameClock();
        public readonly QuestSystem Quests;
        public readonly List<Notice> Notices = new List<Notice>();
        public readonly List<string> MessageLog = new List<string>();
        public readonly RunStats Stats = new RunStats();
        public readonly Wallet Wallet = new Wallet();
        public readonly NoiseSystem Noise = new NoiseSystem();

        public readonly TradeSystem Trade;
        public readonly LootSystem Loot;
        public readonly StorySystem Story;

        public float MinutesPerSecond = 2f;
        public bool Meditating;
        public float SpeedBuffUntil;
        public float SpeedBuffMult = 1f;
        public string SpeedBuffName = "";
        public float VeinBonus = 1f;
        public string VeinName = "";

        private int noticeId = 1;
        private readonly Random random = new Random(1337);

        public WorldSim(ContentDatabase content)
        {
            Content = content;
            Player.Techniques.Add("tuna");
            RecalculateMaxHp();
            Player.Hp = Player.MaxHp;
            Player.Inventory.Add(new ItemStack("bat", 1));
            Player.Inventory.Add(new ItemStack("bandage", 2));
            Player.Inventory.Add(new ItemStack("canned_food", 1));
            Quests = new QuestSystem(this);
            Trade = new TradeSystem(this);
            Loot = new LootSystem(this);
            Story = new StorySystem(this);
            Quests.AutoAccept();
            Notify("第一日，08:00。活下去，再谈长生。", NoticeTone.Info);
        }

        public RealmDef Realm => Content.Realm(Player.RealmIndex);
        public bool IsAtPeak => Player.RealmIndex >= Content.MaxRealm().Index && Player.Stage >= Realm.Stages;

        public StatMods Mods
        {
            get
            {
                var mods = new StatMods();
                foreach (var id in Player.Techniques)
                {
                    var tech = Content.Technique(id);
                    if (tech == null || tech.Passive == null) continue;
                    var level = TechniqueLevel(id);
                    foreach (var entry in tech.Passive)
                    {
                        switch (entry.Stat)
                        {
                            case "maxHp": mods.MaxHp += entry.Add * level; break;
                            case "armor": mods.Armor += entry.Add * level; break;
                            case "damage": mods.DamageMult *= 1f + (entry.Mult - 1f) * level; break;
                            case "qiRegen": mods.QiRegen *= 1f + (entry.Mult - 1f) * level; break;
                            case "meditationXp": mods.MeditationXp *= 1f + (entry.Mult - 1f) * level; break;
                            case "breakthrough": mods.Breakthrough += entry.Add * level; break;
                            case "hungerRate": mods.HungerRate *= 1f + (entry.Mult - 1f) * level; break;
                            case "infectionResist": mods.InfectionResist += entry.Add * level; break;
                        }
                    }
                }
                return mods;
            }
        }

        public void RecalculateMaxHp()
        {
            Player.MaxHp = Math.Max(1f, 100f + Realm.HpBonus + Mods.MaxHp);
            if (Player.Hp > Player.MaxHp) Player.Hp = Player.MaxHp;
        }

        public float QiMax => Realm.QiMax;
        public float XpNeeded => Math.Max(1f, (float)Math.Round(Realm.XpBase * Math.Pow(Realm.XpGrowth, Player.Stage - 1)));
        public float DamageMultiplier => Realm.DamageMult * Mods.DamageMult;

        public void Notify(string text, NoticeTone tone = NoticeTone.Info)
        {
            var notice = new Notice
            {
                Id = noticeId++,
                Text = text,
                Tone = (int)tone,
                Clock = Clock.ClockText,
                Day = Clock.Day
            };
            Notices.Add(notice);
            MessageLog.Add("第" + notice.Day + "天 " + notice.Clock + " · " + text);
            if (Notices.Count > 60) Notices.RemoveAt(0);
            if (MessageLog.Count > 80) MessageLog.RemoveAt(0);
        }

        public void Update(float dt)
        {
            if (Player.Dead) return;

            var minutes = dt * MinutesPerSecond;
            var mods = Mods;
            Stats.Tick(dt, minutes);
            if (SpeedBuffUntil > 0f && Clock.TotalMinutes > SpeedBuffUntil)
            {
                SpeedBuffUntil = 0f;
                SpeedBuffMult = 1f;
                SpeedBuffName = "";
            }
            RecordSurvivalTick(dt);
            Clock.Advance(minutes);
            if (Clock.DawnCrossed)
            {
                Player.Nights++;
                Quests.NotifyDawn();
                Notify("天亮了。第 " + Clock.Day + " 天。", NoticeTone.Info);
            }

            Player.Hunger = Clamp(Player.Hunger + 0.06f * minutes * mods.HungerRate, 0f, 100f);
            Player.Thirst = Clamp(Player.Thirst + 0.085f * minutes * mods.HungerRate, 0f, 100f);
            Player.Fatigue = Clamp(Player.Fatigue + 0.05f * minutes, 0f, 100f);

            var qiRegen = Realm.QiRegen * mods.QiRegen * (Meditating ? 4f : 1f);
            Player.Qi = Clamp(Player.Qi + qiRegen * dt, 0f, QiMax);

            if (Meditating)
            {
                Player.Qi = Clamp(Player.Qi + 6f * VeinBonus * dt, 0f, QiMax);
                Player.MeditationMinutes += minutes;
                Quests.NotifyMeditate(minutes);
                var xp = 1.1f * minutes * mods.MeditationXp * VeinBonus;
                GrantXp(xp);
                Player.DaoHeart = Clamp(Player.DaoHeart + 0.1f * minutes, 0f, 100f);
            }
            else if (Player.Qi >= QiMax - 0.01f && Player.Stage >= Realm.Stages)
            {
                GrantXp(0.15f * minutes);
            }

            foreach (var key in Player.Cooldowns.Keys.ToList())
            {
                Player.Cooldowns[key] = Math.Max(0f, Player.Cooldowns[key] - dt);
            }

            if (Player.Shield > 0f && Clock.TotalMinutes > Player.ShieldUntil) Player.Shield = 0f;

            if (Player.Hunger >= 100f) Player.Hp -= 1.2f * minutes;
            if (Player.Thirst >= 100f) Player.Hp -= 2.2f * minutes;
            if (Player.Bleeding) Player.Hp -= 0.7f * dt;

            if (Player.Infection > 0f)
            {
                Player.Infection = Clamp(Player.Infection + 0.12f * minutes, 0f, 100f);
                if (Player.Infection >= 100f)
                {
                    Player.Hp = 0f;
                }
            }

            if (Player.Hp < Player.MaxHp && Player.Hunger < 60f && Player.Thirst < 60f &&
                !Player.Bleeding && Player.Infection < 30f && !Meditating)
            {
                Player.Hp = Math.Min(Player.MaxHp, Player.Hp + 0.05f * minutes);
            }

            if (!Player.Meditating)
            {
                Player.Stamina = Clamp(Player.Stamina + 11f * dt, 0f, Player.MaxStamina);
            }

            if (Player.Hp <= 0f && !Player.Dead)
            {
                Player.Hp = 0f;
                Player.Dead = true;
                Meditating = false;
                Notify("你倒下了。荒野会记住你的名字，也可能不会。", NoticeTone.Bad);
            }
        }

        public void ToggleMeditate()
        {
            if (Player.Dead) return;
            if (Meditating)
            {
                Meditating = false;
                Player.Meditating = false;
                Notify("收功。", NoticeTone.Info);
                return;
            }
            if (!Player.Techniques.Contains("tuna"))
            {
                Notify("尚未习得吐纳诀，无法入定。", NoticeTone.Warn);
                return;
            }
            Meditating = true;
            Player.Meditating = true;
            Notify("盘膝入定，灵气缓缓汇入丹田。", NoticeTone.Info);
        }

        public void StopMeditate(string reason)
        {
            if (!Meditating) return;
            Meditating = false;
            Player.Meditating = false;
            if (!string.IsNullOrEmpty(reason)) Notify(reason, NoticeTone.Warn);
        }

        public void GrantXp(float amount)
        {
            if (amount <= 0f || Player.Dead) return;
            if (IsAtPeak)
            {
                Player.Xp = XpNeeded;
                return;
            }

            Player.Xp += amount;
            var guard = 0;
            while (Player.Xp >= XpNeeded && guard++ < 16)
            {
                if (Player.Stage < Realm.Stages)
                {
                    Player.Xp -= XpNeeded;
                    Player.Stage++;
                    Player.Hp = Math.Min(Player.MaxHp, Player.Hp + 20f);
                    Notify(Realm.Name + " · " + Player.Stage + " 层", NoticeTone.Good);
                    Quests.NotifyRealm(Player.RealmIndex);
                }
                else
                {
                    Player.Xp = XpNeeded;
                    break;
                }
            }
            Player.Xp = Math.Min(Player.Xp, XpNeeded);
        }

        public void GrantQi(float amount)
        {
            Player.Qi = Clamp(Player.Qi + amount, 0f, QiMax);
        }

        public float InventoryWeight()
        {
            var total = 0f;
            foreach (var stack in Player.Inventory)
            {
                var def = Content.Item(stack.ItemId);
                if (def != null) total += def.Weight * stack.Count;
            }
            return (float)Math.Round(total, 2);
        }

        public int CountItem(string itemId)
        {
            var count = 0;
            foreach (var stack in Player.Inventory)
            {
                if (stack.ItemId == itemId) count += stack.Count;
            }
            return count;
        }

        public int AddItem(string itemId, int count, bool countForQuest = true)
        {
            var def = Content.Item(itemId);
            if (def == null || count <= 0) return count;
            var remaining = count;
            var weight = InventoryWeight();
            var added = 0;

            foreach (var stack in Player.Inventory)
            {
                if (remaining <= 0) break;
                if (stack.ItemId != itemId || stack.Count >= def.Stack) continue;
                var space = def.Stack - stack.Count;
                var weightRoom = (int)Math.Floor((Player.MaxWeight - weight) / Math.Max(0.001f, def.Weight));
                var take = Math.Min(Math.Min(space, remaining), weightRoom);
                if (take <= 0) break;
                stack.Count += take;
                remaining -= take;
                added += take;
                weight += take * def.Weight;
            }

            while (remaining > 0)
            {
                var weightRoom = (int)Math.Floor((Player.MaxWeight - weight) / Math.Max(0.001f, def.Weight));
                if (weightRoom <= 0) break;
                var take = Math.Min(Math.Min(def.Stack, remaining), weightRoom);
                Player.Inventory.Add(new ItemStack(itemId, take));
                remaining -= take;
                added += take;
                weight += take * def.Weight;
            }

            if (added > 0)
            {
                Player.ItemsCollected += added;
                if (countForQuest) Quests.NotifyCollect(itemId, added);
            }
            return remaining;
        }

        public bool RemoveItem(string itemId, int count)
        {
            var remaining = count;
            for (var i = Player.Inventory.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var stack = Player.Inventory[i];
                if (stack.ItemId != itemId) continue;
                var take = Math.Min(stack.Count, remaining);
                stack.Count -= take;
                remaining -= take;
                if (stack.Count <= 0) Player.Inventory.RemoveAt(i);
            }
            if (Player.Equipped == itemId && CountItem(itemId) == 0) Player.Equipped = null;
            return remaining == 0;
        }

        public bool UseItem(string itemId)
        {
            if (Player.Dead) return false;
            var def = Content.Item(itemId);
            if (def == null || CountItem(itemId) == 0) return false;
            if (def.Weapon != null || !string.IsNullOrEmpty(def.Slot))
            {
                return EquipItem(itemId);
            }

            var applied = false;
            foreach (var use in def.Use)
            {
                switch (use.Kind)
                {
                    case "teach":
                        applied |= LearnTechnique(use.TechniqueId);
                        break;
                    case "satisfy":
                        if (use.Need == "hunger") Player.Hunger = Clamp(Player.Hunger - use.Amount, 0f, 100f);
                        if (use.Need == "thirst") Player.Thirst = Clamp(Player.Thirst - use.Amount, 0f, 100f);
                        if (use.Need == "fatigue") Player.Fatigue = Clamp(Player.Fatigue - use.Amount, 0f, 100f);
                        applied = true;
                        break;
                    case "heal":
                        Player.Hp = Math.Min(Player.MaxHp, Player.Hp + use.Amount);
                        applied = true;
                        break;
                    case "stop_bleeding":
                        if (Player.Bleeding) Notify("血止住了。", NoticeTone.Good);
                        Player.Bleeding = false;
                        applied = true;
                        break;
                    case "qi":
                        GrantQi(use.Amount);
                        applied = true;
                        break;
                    case "cultivation_xp":
                        GrantXp(use.Amount);
                        applied = true;
                        break;
                    case "shield":
                        Player.Shield = use.Amount;
                        Player.ShieldUntil = Clock.TotalMinutes + use.Duration;
                        applied = true;
                        break;
                }
            }

            if (!applied) return false;
            RemoveItem(itemId, 1);
            return true;
        }

        public bool LearnTechnique(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            var def = Content.Technique(id);
            if (def == null) return false;
            if (Player.Techniques.Contains(id))
            {
                Notify("已经习得《" + def.Name + "》", NoticeTone.Info);
                return false;
            }
            if (Player.RealmIndex < def.RequiresRealm)
            {
                Notify("境界不足，需 " + Content.Realm(def.RequiresRealm).Name + " 方可修习《" + def.Name + "》", NoticeTone.Warn);
                return false;
            }
            Player.Techniques.Add(id);
            if (!Player.TechniqueLevels.ContainsKey(id)) Player.TechniqueLevels[id] = 1;
            RecalculateMaxHp();
            Notify("习得" + (def.Kind == "active" ? "术法" : "功法") + "《" + def.Name + "》", NoticeTone.Good);
            return true;
        }

        public float TechniqueCooldown(string id)
        {
            return Player.Cooldowns.TryGetValue(id, out var value) ? value : 0f;
        }

        public bool CastTechnique(string id)
        {
            var def = Content.Technique(id);
            if (def == null || def.Kind != "active" || !Player.Techniques.Contains(id)) return false;
            if (Player.Dead || Meditating) return false;
            if (TechniqueCooldown(id) > 0f) return false;
            if (Player.Qi < def.QiCost)
            {
                Notify("灵气不足。", NoticeTone.Warn);
                return false;
            }

            Player.Qi -= def.QiCost;
            Player.Cooldowns[id] = def.Cooldown;
            Player.TechniquesUsed++;
            Stats.TechniqueCasts++;
            Quests.NotifyCast(id);
            if (def.Active != null && def.Active.Kind == "movement")
            {
                var level = TechniqueLevel(id);
                SpeedBuffUntil = Clock.TotalMinutes + def.Active.Duration * (1f + (level - 1) * def.LevelScale);
                SpeedBuffMult = def.Active.SpeedMult;
                SpeedBuffName = def.Name;
                RecordEvent(GameEventType.Technique, "cast." + id, "施展《" + def.Name + "》", level);
                Notify("《" + def.Name + "》· 身法展开。", NoticeTone.Good);
            }
            return true;
        }

        public List<string> BreakthroughBlockers()
        {
            var blockers = new List<string>();
            if (IsAtPeak)
            {
                blockers.Add("已至此界巅峰，再无可破之境");
                return blockers;
            }
            if (Player.Stage < Realm.Stages) blockers.Add("需修至" + Realm.Name + "第 " + Realm.Stages + " 层");
            if (Player.Xp < XpNeeded) blockers.Add("修为尚未圆满");
            if (Player.Qi < QiMax * 0.5f) blockers.Add("灵气不足（需 " + (int)(QiMax * 0.5f) + "）");
            if (!string.IsNullOrEmpty(Realm.ItemId) && CountItem(Realm.ItemId) == 0) blockers.Add("缺少筑基丹");
            if (Player.DaoHeart < Realm.MinDaoHeart) blockers.Add("道心不足（需 " + (int)Realm.MinDaoHeart + "）");
            return blockers;
        }

        public float BreakthroughChance()
        {
            var chance = Realm.BaseChance + Player.DaoHeart / 260f + (Player.Qi / Math.Max(1f, QiMax)) * 0.1f + Mods.Breakthrough;
            return Clamp(chance, 0.05f, 0.95f);
        }

        public bool AttemptBreakthrough()
        {
            var blockers = BreakthroughBlockers();
            if (blockers.Count > 0)
            {
                Notify(blockers[0], NoticeTone.Warn);
                return false;
            }

            if (!string.IsNullOrEmpty(Realm.ItemId)) RemoveItem(Realm.ItemId, 1);
            Player.BreakthroughAttempts++;
            var chance = BreakthroughChance();
            var success = (float)random.NextDouble() < chance;
            if (success)
            {
                Player.RealmIndex++;
                Player.Stage = 1;
                Player.Xp = 0f;
                RecalculateMaxHp();
                Player.Qi = QiMax * 0.35f;
                Player.DaoHeart = Math.Min(100f, Player.DaoHeart + 2f);
                Player.Hp = Player.MaxHp;
                Quests.NotifyRealm(Player.RealmIndex);
                Notify("破境成功，踏入" + Realm.Name + "！", NoticeTone.Good);
            }
            else
            {
                Player.BreakthroughFailures++;
                Player.Xp *= 0.35f;
                Player.Qi = 0f;
                Player.DaoHeart = Math.Max(0f, Player.DaoHeart - 4f);
                Player.Hp = Math.Max(1f, Player.Hp - 12f);
                Notify("破境失败，经脉受损，修为倒退。", NoticeTone.Bad);
            }
            RecalculateMaxHp();
            return success;
        }

        public bool AcceptQuest(string id)
        {
            return Quests.Accept(id);
        }

        public void GrantReward(RewardDef reward)
        {
            switch (reward.Kind)
            {
                case "item":
                    var left = AddItem(reward.ItemId, reward.Count, false);
                    if (left > 0) Notify("背包已满，部分奖励掉落。", NoticeTone.Warn);
                    break;
                case "cultivation_xp":
                    GrantXp(reward.Amount);
                    break;
                case "qi":
                    GrantQi(reward.Amount);
                    break;
                case "technique":
                    LearnTechnique(reward.TechniqueId);
                    break;
                case "dao_heart":
                    Player.DaoHeart = Clamp(Player.DaoHeart + reward.Amount, 0f, 100f);
                    break;
                case "insight":
                    Player.Insight += reward.Amount;
                    Notify("悟性 +" + reward.Amount, NoticeTone.Good);
                    break;
                case "currency":
                    if (reward.Currency == "crystal_core") AddItem("crystal_core", reward.Amount, false);
                    else Wallet.Add(reward.Currency, reward.Amount);
                    break;
            }
        }

        public void AddInfection(float amount)
        {
            Player.Infection = Clamp(Player.Infection + amount * (1f - Mods.InfectionResist), 0f, 100f);
        }

        public void DamagePlayer(float amount, bool canBleed = false)
        {
            if (Player.Dead) return;
            if (Player.Shield > 0f)
            {
                var absorbed = Math.Min(Player.Shield, amount);
                Player.Shield -= absorbed;
                amount -= absorbed;
            }
            if (amount <= 0f) return;
            var armor = Armor;
            if (armor > 0f) amount *= 1f - armor / (armor + 25f);
            amount = Math.Max(1f, amount);
            Player.Hp -= amount;
            Stats.DamageTaken += (int)Math.Round(amount);
            RecordEvent(GameEventType.Damage, "player.hit", "受到伤害 " + (int)Math.Round(amount), (int)Math.Round(amount));
            if (canBleed && random.NextDouble() < 0.35) Player.Bleeding = true;
            if (Player.Hp <= 0f) Player.Hp = 0f;
            StopMeditate("入定被打断！");
        }

        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}

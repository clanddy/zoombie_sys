using System;
using System.Collections.Generic;

namespace AshenDao.Sim
{
    public enum NoticeTone
    {
        Info,
        Good,
        Warn,
        Bad
    }

    [Serializable]
    public class WeaponDef
    {
        public string Kind = "melee"; // melee | firearm
        public float Damage = 12f;
        public float Range = 1.6f;
        public float Cooldown = 0.7f;
        public float StaminaCost = 8f;
        public string AmmoItem;
        public int Magazine;
        public float ReloadTime = 1.6f;
        public float NoiseRadius;
        public int Pellets = 1;
        public float Spread;
        public bool Automatic;
        public float ArmorPenetration;
    }

    [Serializable]
    public class ItemUseDef
    {
        public string Kind;
        public string Need;
        public float Amount;
        public string TechniqueId;
        public float Duration;
    }

    [Serializable]
    public class ItemDef
    {
        public string Id;
        public string Name;
        public string Description;
        public string Category;
        public string Icon;
        public float Weight = 0.1f;
        public int Stack = 1;
        public WeaponDef Weapon;
        public List<ItemUseDef> Use = new List<ItemUseDef>();
        public int Price;
        public int SellPrice;
        public string Currency = "coin";
        public string Slot;              // head/body/weapon/ammo/...
        public float Armor;
        public bool Tradeable = true;
    }

    [Serializable]
    public class PassiveModDef
    {
        public string Stat;
        public float Add;
        public float Mult = 1f;
    }

    [Serializable]
    public class ActiveEffectDef
    {
        public string Kind;
        public float Range = 2f;
        public float Arc = 1f;
        public float Damage;
        public float Distance;
        public float Radius;
        public float Amount;
        public float Duration;
        public float SpeedMult = 1f;
        public float SprintMult = 1f;
    }

    [Serializable]
    public class TechniqueDef
    {
        public string Id;
        public string Name;
        public string Description;
        public string Kind = "passive";
        public string Icon;
        public string Vfx;
        public int Tier;
        public int RequiresRealm = 1;
        public float QiCost;
        public float Cooldown;
        public List<PassiveModDef> Passive = new List<PassiveModDef>();
        public ActiveEffectDef Active;
        public int MaxLevel = 5;
        public int InsightCost = 1;
        public float LevelScale = 0.2f;
        public string UnlockHint;
    }

    [Serializable]
    public class RealmDef
    {
        public int Index;
        public string Id;
        public string Name;
        public int Stages = 9;
        public float QiMax = 60f;
        public float XpBase = 30f;
        public float XpGrowth = 1.18f;
        public float HpBonus = 15f;
        public float DamageMult = 1.05f;
        public float QiRegen = 0.5f;
        public float BaseChance = 0.6f;
        public float MinDaoHeart = 20f;
        public string ItemId;
    }

    [Serializable]
    public class ObjectiveDef
    {
        public string Kind;
        public string ItemId;
        public string EnemyId;
        public string TechniqueId;
        public string Label;
        public int Count = 1;
        public float Minutes;
        public int RealmIndex;
        public string NpcId;
        public string Flag;
        public string TraderId;
    }

    [Serializable]
    public class RewardDef
    {
        public string Kind;
        public string ItemId;
        public string TechniqueId;
        public int Count = 1;
        public int Amount = 1;
        public string Currency = "coin";
    }

    [Serializable]
    public class QuestDef
    {
        public string Id;
        public string Title;
        public string Category;
        public string Giver;
        public string Summary;
        public string Description;
        public int RequiresRealm;
        public List<string> RequiresQuests = new List<string>();
        public List<ObjectiveDef> Objectives = new List<ObjectiveDef>();
        public List<RewardDef> Rewards = new List<RewardDef>();
        public bool AutoAccept;
        public bool Repeatable;
        public string GiverNpc;
        public List<string> RequiresFlags = new List<string>();
    }

    [Serializable]
    public class LootEntry
    {
        public string ItemId;
        public string Currency;
        public int Min = 1;
        public int Max = 1;
        public int Amount;
        public float Chance = 1f;
    }

    [Serializable]
    public class LootTableDef
    {
        public string Id;
        public int MinRolls = 1;
        public int MaxRolls = 2;
        public List<LootEntry> Entries = new List<LootEntry>();
    }

    [Serializable]
    public class ZombieDef
    {
        public string Id;
        public string Name = "行尸";
        public string Kind = "walker";
        public float Hp = 55f;
        public float Speed = 1.45f;
        public float Damage = 7f;
        public float Sight = 16f;
        public string LootTable;
        public string Tint = "#6b7a55";
    }

    [Serializable]
    public class TradeOfferDef
    {
        public string ItemId;
        public int Price;
        public int SellPrice;
        public int Stock = -1;
    }

    [Serializable]
    public class TraderDef
    {
        public string Id;
        public string Name;
        public string Kind = "system"; // system | npc
        public string Currency = "coin";
        public string Greeting;
        public List<TradeOfferDef> Offers = new List<TradeOfferDef>();
    }

    [Serializable]
    public class DialogueChoiceDef
    {
        public string Text;
        public string Next;
        public string Action;
        public string Arg;
        public int Amount;
        public string RequiresFlag;
        public string RequiresItem;
    }

    [Serializable]
    public class DialogueNodeDef
    {
        public string Id;
        public string Speaker;
        public string Text;
        public string Action;
        public string Arg;
        public List<DialogueChoiceDef> Choices = new List<DialogueChoiceDef>();
    }

    [Serializable]
    public class DialogueDef
    {
        public string Id;
        public string NpcId;
        public string StartNode;
        public List<DialogueNodeDef> Nodes = new List<DialogueNodeDef>();
    }

    [Serializable]
    public class NpcDef
    {
        public string Id;
        public string Name;
        public string Role;
        public float HomeX;
        public float HomeZ;
        public float WanderRadius = 3f;
        public string DialogueId;
        public string TraderId;
        public string Tint = "#4f8f78";
        public bool AtSafehouse;
    }

    public class ContentDatabase
    {
        public readonly List<ItemDef> Items = new List<ItemDef>();
        public readonly List<TechniqueDef> Techniques = new List<TechniqueDef>();
        public readonly List<RealmDef> Realms = new List<RealmDef>();
        public readonly List<QuestDef> Quests = new List<QuestDef>();
        public readonly List<LootTableDef> LootTables = new List<LootTableDef>();
        public readonly List<ZombieDef> Zombies = new List<ZombieDef>();
        public readonly List<TraderDef> Traders = new List<TraderDef>();
        public readonly List<NpcDef> Npcs = new List<NpcDef>();
        public readonly List<DialogueDef> Dialogues = new List<DialogueDef>();

        private readonly Dictionary<string, ItemDef> items = new Dictionary<string, ItemDef>();
        private readonly Dictionary<string, TechniqueDef> techniques = new Dictionary<string, TechniqueDef>();
        private readonly Dictionary<int, RealmDef> realms = new Dictionary<int, RealmDef>();
        private readonly Dictionary<string, QuestDef> quests = new Dictionary<string, QuestDef>();
        private readonly Dictionary<string, LootTableDef> lootTables = new Dictionary<string, LootTableDef>();
        private readonly Dictionary<string, ZombieDef> zombies = new Dictionary<string, ZombieDef>();
        private readonly Dictionary<string, TraderDef> traders = new Dictionary<string, TraderDef>();
        private readonly Dictionary<string, NpcDef> npcs = new Dictionary<string, NpcDef>();
        private readonly Dictionary<string, DialogueDef> dialogues = new Dictionary<string, DialogueDef>();

        public void Rebuild()
        {
            items.Clear();
            techniques.Clear();
            realms.Clear();
            quests.Clear();
            lootTables.Clear();
            zombies.Clear();
            traders.Clear();
            npcs.Clear();
            dialogues.Clear();
            foreach (var item in Items) items[item.Id] = item;
            foreach (var tech in Techniques) techniques[tech.Id] = tech;
            foreach (var realm in Realms) realms[realm.Index] = realm;
            foreach (var quest in Quests) quests[quest.Id] = quest;
            foreach (var table in LootTables) lootTables[table.Id] = table;
            foreach (var zombie in Zombies) zombies[zombie.Id] = zombie;
            foreach (var trader in Traders) traders[trader.Id] = trader;
            foreach (var npc in Npcs) npcs[npc.Id] = npc;
            foreach (var dialogue in Dialogues) dialogues[dialogue.Id] = dialogue;
        }

        public ItemDef Item(string id)
        {
            return items.TryGetValue(id, out var def) ? def : null;
        }

        public TechniqueDef Technique(string id)
        {
            return techniques.TryGetValue(id, out var def) ? def : null;
        }

        public RealmDef Realm(int index)
        {
            return realms.TryGetValue(index, out var def) ? def : realms[1];
        }

        public QuestDef Quest(string id)
        {
            return quests.TryGetValue(id, out var def) ? def : null;
        }

        public LootTableDef LootTable(string id)
        {
            return lootTables.TryGetValue(id, out var def) ? def : null;
        }

        public ZombieDef Zombie(string id)
        {
            return zombies.TryGetValue(id, out var def) ? def : null;
        }

        public TraderDef Trader(string id)
        {
            return traders.TryGetValue(id, out var def) ? def : null;
        }

        public NpcDef Npc(string id)
        {
            return npcs.TryGetValue(id, out var def) ? def : null;
        }

        public DialogueDef Dialogue(string id)
        {
            return dialogues.TryGetValue(id, out var def) ? def : null;
        }

        public RealmDef MaxRealm()
        {
            var best = realms[1];
            foreach (var realm in Realms)
            {
                if (realm.Index > best.Index) best = realm;
            }
            return best;
        }

        public static ContentDatabase CreateDemo()
        {
            var db = new ContentDatabase();

            db.Items.Add(Item("water_bottle", "瓶装水", "干净的饮用水。", "drink", 0.5f, 4,
                Use("satisfy", "thirst", 38f)));
            db.Items.Add(Item("canned_food", "罐头", "过期但密封完好的午餐肉。", "food", 0.4f, 8,
                Use("satisfy", "hunger", 32f)));
            db.Items.Add(Item("energy_bar", "能量棒", "高糖高热量。", "food", 0.15f, 10,
                Use("satisfy", "hunger", 14f), Use("satisfy", "fatigue", 12f)));
            db.Items.Add(Item("bandage", "绷带", "止血用的纱布。", "medical", 0.1f, 10,
                Use("heal", null, 4f), Use("stop_bleeding", null, 0f)));
            db.Items.Add(Item("scrap", "废铁", "从车壳上撬下来的金属。", "material", 0.6f, 20));
            db.Items.Add(Item("spirit_herb", "灵草", "叶片在夜里泛着微光。", "relic", 0.05f, 15,
                Use("qi", null, 18f), Use("cultivation_xp", null, 4f)));
            db.Items.Add(Item("qi_pill", "聚气丹", "入口即化的暖流。", "pill", 0.02f, 10,
                Use("qi", null, 45f), Use("cultivation_xp", null, 12f)));
            db.Items.Add(Item("foundation_pill", "筑基丹", "冲击筑基期所需的丹药。", "pill", 0.02f, 3,
                Use("qi", null, 60f)));
            db.Items.Add(Item("spirit_stone", "灵石", "凝固的天地灵气。", "relic", 0.1f, 20,
                Use("qi", null, 25f), Use("cultivation_xp", null, 6f)));
            var crystal = Item("crystal_core", "晶核", "行尸颅腔里凝结的浊气结晶，天机碑只收这个。",
                "currency", 0.05f, 50);
            crystal.Price = 2;
            crystal.SellPrice = 1;
            crystal.Currency = "crystal_core";
            db.Items.Add(crystal);

            var knife = Item("knife", "猎刀", "轻便、安静，适合贴脸。", "weapon", 0.35f, 1);
            knife.Weapon = new WeaponDef
            {
                Kind = "melee", Damage = 13f, Range = 1.35f, Cooldown = 0.42f, StaminaCost = 4f, NoiseRadius = 2.5f
            };
            knife.Price = 12;
            knife.SellPrice = 5;
            db.Items.Add(knife);

            var pistol = Item("pistol", "旧制手枪", "警用制式，弹匣还能用。", "weapon", 0.9f, 1);
            pistol.Weapon = new WeaponDef
            {
                Kind = "firearm", Damage = 24f, Range = 18f, Cooldown = 0.38f, StaminaCost = 2f,
                AmmoItem = "ammo_9mm", Magazine = 8, ReloadTime = 1.5f, NoiseRadius = 30f,
                Pellets = 1, Spread = 0.025f
            };
            pistol.Price = 55;
            pistol.SellPrice = 22;
            db.Items.Add(pistol);

            var rifle = Item("hunting_rifle", "猎枪", "栓动，声音大，但能把尸群钉在街口。", "weapon", 1.7f, 1);
            rifle.Weapon = new WeaponDef
            {
                Kind = "firearm", Damage = 62f, Range = 34f, Cooldown = 1.15f, StaminaCost = 4f,
                AmmoItem = "ammo_762", Magazine = 5, ReloadTime = 2.1f, NoiseRadius = 42f,
                Pellets = 1, Spread = 0.008f
            };
            rifle.Price = 95;
            rifle.SellPrice = 40;
            db.Items.Add(rifle);

            var shotgun = Item("shotgun", "短管霰弹枪", "近距离清场；枪声会招来更多东西。", "weapon", 1.5f, 1);
            shotgun.Weapon = new WeaponDef
            {
                Kind = "firearm", Damage = 15f, Range = 9f, Cooldown = 0.9f, StaminaCost = 5f,
                AmmoItem = "ammo_shell", Magazine = 5, ReloadTime = 2.3f, NoiseRadius = 40f,
                Pellets = 6, Spread = 0.11f
            };
            shotgun.Price = 80;
            shotgun.SellPrice = 32;
            db.Items.Add(shotgun);

            var ammo9 = Item("ammo_9mm", "9mm 子弹", "手枪弹。", "ammo", 0.01f, 30);
            ammo9.Price = 2;
            ammo9.SellPrice = 1;
            db.Items.Add(ammo9);
            var ammo762 = Item("ammo_762", "步枪弹", "猎枪与步枪通用。", "ammo", 0.015f, 30);
            ammo762.Price = 3;
            ammo762.SellPrice = 1;
            db.Items.Add(ammo762);
            var shells = Item("ammo_shell", "霰弹", "近距离的底气。", "ammo", 0.02f, 20);
            shells.Price = 4;
            shells.SellPrice = 2;
            db.Items.Add(shells);

            var leather = Item("leather_jacket", "皮夹克", "磨损的厚皮，能挡几下抓挠。", "clothing", 1.1f, 1);
            leather.Slot = "body";
            leather.Armor = 4f;
            leather.Price = 24;
            leather.SellPrice = 10;
            db.Items.Add(leather);
            var kevlar = Item("kevlar_vest", "防弹背心", "警用剩余物资。", "clothing", 1.6f, 1);
            kevlar.Slot = "body";
            kevlar.Armor = 9f;
            kevlar.Price = 48;
            kevlar.SellPrice = 20;
            db.Items.Add(kevlar);
            var cap = Item("police_cap", "警帽", "聊胜于无。", "clothing", 0.2f, 1);
            cap.Slot = "head";
            cap.Armor = 1.5f;
            cap.Price = 8;
            cap.SellPrice = 3;
            db.Items.Add(cap);

            var windScroll = Item("wind_step_scroll", "御风残卷", "残页上只有一道向前的风痕。使用后习得《御风步》。",
                "manual", 0.1f, 1);
            windScroll.Use.Add(new ItemUseDef { Kind = "teach", TechniqueId = "wind_step" });
            windScroll.Price = 3;
            windScroll.Currency = "crystal_core";
            db.Items.Add(windScroll);

            var bat = Item("bat", "棒球棍", "缠了胶带的球棍。", "weapon", 1.2f, 1);
            bat.Weapon = new WeaponDef
            {
                Kind = "melee", Damage = 18f, Range = 1.7f, Cooldown = 0.72f, StaminaCost = 9f, NoiseRadius = 5f
            };
            bat.Price = 10;
            bat.SellPrice = 4;
            db.Items.Add(bat);

            db.Techniques.Add(new TechniqueDef
            {
                Id = "tuna",
                Name = "吐纳诀",
                Description = "最基础的引气法门，提高回气与入定效率。",
                Kind = "passive",
                Icon = "icons/techniques/tuna.png",
                Tier = 0,
                RequiresRealm = 1,
                Passive = new List<PassiveModDef>
                {
                    new PassiveModDef { Stat = "qiRegen", Mult = 1.6f },
                    new PassiveModDef { Stat = "meditationXp", Mult = 1.25f }
                }
            });
            db.Techniques.Add(new TechniqueDef
            {
                Id = "iron_shirt",
                Name = "铁布衫",
                Description = "横练肉身，护甲与气血上限提高。",
                Kind = "passive",
                Icon = "icons/techniques/iron_shirt.png",
                Tier = 1,
                RequiresRealm = 1,
                Passive = new List<PassiveModDef>
                {
                    new PassiveModDef { Stat = "armor", Add = 4f },
                    new PassiveModDef { Stat = "maxHp", Add = 12f }
                }
            });
            db.Techniques.Add(new TechniqueDef
            {
                Id = "sword_qi",
                Name = "剑气斩",
                Description = "并指成剑，斩出一道弧形剑气。",
                Kind = "active",
                Icon = "icons/techniques/sword_qi.png",
                Vfx = "vfx.slash_qi",
                Tier = 1,
                RequiresRealm = 1,
                QiCost = 12f,
                Cooldown = 2.2f,
                Active = new ActiveEffectDef
                {
                    Kind = "strike",
                    Range = 2.8f,
                    Arc = 1.35f,
                    Damage = 34f
                }
            });
            db.Techniques.Add(new TechniqueDef
            {
                Id = "golden_bell",
                Name = "护体罡气",
                Description = "真气外放，凝成金钟虚影。",
                Kind = "active",
                Icon = "icons/techniques/golden_bell.png",
                Vfx = "vfx.shield",
                Tier = 2,
                RequiresRealm = 2,
                QiCost = 20f,
                Cooldown = 18f,
                Active = new ActiveEffectDef { Kind = "shield", Amount = 45f, Duration = 10f }
            });

            db.Techniques.Add(new TechniqueDef
            {
                Id = "wind_step",
                Name = "御风步",
                Description = "灵气灌入足少阴，踏风而行。持续期间移动速度大幅提高。",
                Kind = "active",
                Icon = "icons/techniques/wind_step.png",
                Vfx = "vfx.wind",
                Tier = 1,
                RequiresRealm = 1,
                QiCost = 14f,
                Cooldown = 16f,
                MaxLevel = 5,
                InsightCost = 1,
                UnlockHint = "完成入定或从天机碑兑换残卷",
                Active = new ActiveEffectDef
                {
                    Kind = "movement",
                    Duration = 6f,
                    SpeedMult = 1.75f,
                    SprintMult = 1.25f
                }
            });
            db.Techniques.Add(new TechniqueDef
            {
                Id = "cloud_step",
                Name = "踏云诀",
                Description = "御风步的进境。真气托足，短时间内的速度接近旧时代的车辆。",
                Kind = "active",
                Icon = "icons/techniques/cloud_step.png",
                Vfx = "vfx.wind",
                Tier = 2,
                RequiresRealm = 2,
                QiCost = 26f,
                Cooldown = 28f,
                MaxLevel = 5,
                InsightCost = 2,
                UnlockHint = "筑基后自悟，或从天机碑兑换",
                Active = new ActiveEffectDef
                {
                    Kind = "movement",
                    Duration = 8f,
                    SpeedMult = 2.35f,
                    SprintMult = 1.4f
                }
            });

            db.Realms.Add(Realm(1, "qi_refining", "炼气期", 9, 60f, 30f, 1.18f, 15f, 1.05f, 0.5f, 0.6f, 20f, "foundation_pill"));
            db.Realms.Add(Realm(2, "foundation", "筑基期", 6, 160f, 90f, 1.22f, 45f, 1.15f, 1.0f, 0.5f, 40f, null));
            db.Realms.Add(Realm(3, "golden_core", "金丹期", 5, 400f, 260f, 1.25f, 90f, 1.3f, 1.8f, 0.42f, 55f, null));

            db.Quests.Add(new QuestDef
            {
                Id = "first_night",
                Title = "第一夜",
                Category = "survival",
                Giver = "褪色的守夜人笔记",
                Summary = "活过第一夜，找到一口干净的水。",
                Description = "笔记上只有两行字：「水比血重要。别在夜里点灯。」找到一瓶水，撑到天亮。",
                AutoAccept = true,
                Objectives = new List<ObjectiveDef>
                {
                    new ObjectiveDef { Kind = "collect", ItemId = "water_bottle", Count = 1 },
                    new ObjectiveDef { Kind = "survive_night", Count = 1 }
                },
                Rewards = new List<RewardDef>
                {
                    new RewardDef { Kind = "technique", TechniqueId = "iron_shirt" },
                    new RewardDef { Kind = "cultivation_xp", Amount = 30 },
                    new RewardDef { Kind = "insight", Amount = 1 },
                    new RewardDef { Kind = "item", ItemId = "canned_food", Count = 2 }
                }
            });
            db.Quests.Add(new QuestDef
            {
                Id = "first_blood",
                Title = "街口的尸群",
                Category = "main",
                Giver = "天机碑",
                Summary = "清理六个行尸，证明你还拿得动武器。",
                Description = "「心不静，则气不顺。杀人者，先杀惧。」天机碑只认结果。",
                Objectives = new List<ObjectiveDef> { new ObjectiveDef { Kind = "kill", Count = 6 } },
                Rewards = new List<RewardDef>
                {
                    new RewardDef { Kind = "item", ItemId = "qi_pill", Count = 1 },
                    new RewardDef { Kind = "cultivation_xp", Amount = 40 },
                    new RewardDef { Kind = "insight", Amount = 2 }
                }
            });
            db.Quests.Add(new QuestDef
            {
                Id = "meditate",
                Title = "入定",
                Category = "cultivation",
                Giver = "守夜人笔记",
                Summary = "吐纳三十分钟，摸到气感。",
                Description = "「坐到心口发凉，再坐到发烫。」找个没有尸爪的地方坐下。",
                Objectives = new List<ObjectiveDef>
                {
                    new ObjectiveDef { Kind = "meditate", Minutes = 30f }
                },
                Rewards = new List<RewardDef>
                {
                    new RewardDef { Kind = "cultivation_xp", Amount = 50 },
                    new RewardDef { Kind = "qi", Amount = 30 },
                    new RewardDef { Kind = "insight", Amount = 2 },
                    new RewardDef { Kind = "technique", TechniqueId = "sword_qi" },
                    new RewardDef { Kind = "technique", TechniqueId = "wind_step" }
                }
            });
            db.Quests.Add(new QuestDef
            {
                Id = "scavenge",
                Title = "废墟拾荒",
                Category = "side",
                Giver = "天机碑",
                Summary = "收集废铁，换一点活下去的本钱。",
                Description = "「器物之道，亦是道。」收拢四块废铁，投入碑下的石臼。",
                Repeatable = true,
                Objectives = new List<ObjectiveDef>
                {
                    new ObjectiveDef { Kind = "collect", ItemId = "scrap", Count = 4 }
                },
                Rewards = new List<RewardDef>
                {
                    new RewardDef { Kind = "item", ItemId = "bandage", Count = 2 },
                    new RewardDef { Kind = "cultivation_xp", Amount = 25 },
                    new RewardDef { Kind = "insight", Amount = 1 }
                }
            });
            db.Quests.Add(new QuestDef
            {
                Id = "herbs",
                Title = "灵草与丹炉",
                Category = "cultivation",
                Giver = "生锈的丹炉",
                Summary = "采三株灵草，凑一枚筑基丹。",
                Description = "丹炉早已熄火，炉底却刻着一行字：「筑基无丹，如渡河无舟。」",
                RequiresQuests = new List<string> { "first_blood" },
                Objectives = new List<ObjectiveDef>
                {
                    new ObjectiveDef { Kind = "collect", ItemId = "spirit_herb", Count = 3 }
                },
                Rewards = new List<RewardDef>
                {
                    new RewardDef { Kind = "item", ItemId = "foundation_pill", Count = 1 },
                    new RewardDef { Kind = "cultivation_xp", Amount = 35 }
                }
            });
            db.Quests.Add(new QuestDef
            {
                Id = "breakthrough",
                Title = "筑基",
                Category = "main",
                Giver = "天机碑",
                Summary = "服下筑基丹，踏入筑基期。",
                Description = "炼气九层圆满之后，丹田如满弓。石碑没有安慰，只有一句话：「怕死的人，修什么仙。」",
                RequiresRealm = 1,
                RequiresQuests = new List<string> { "meditate", "herbs" },
                Objectives = new List<ObjectiveDef>
                {
                    new ObjectiveDef { Kind = "reach_realm", RealmIndex = 2, Count = 1 }
                },
                Rewards = new List<RewardDef>
                {
                    new RewardDef { Kind = "cultivation_xp", Amount = 100 },
                    new RewardDef { Kind = "dao_heart", Amount = 10 },
                    new RewardDef { Kind = "insight", Amount = 3 },
                    new RewardDef { Kind = "technique", TechniqueId = "golden_bell" }
                }
            });

            // ---------------------------------------------------------------
            // Loot tables: zombie type determines clothing / gear / firearms.
            // ---------------------------------------------------------------
            db.LootTables.Add(new LootTableDef
            {
                Id = "loot.walker", MinRolls = 1, MaxRolls = 3,
                Entries = new List<LootEntry>
                {
                    new LootEntry { Currency = "coin", Min = 1, Max = 4, Chance = 0.65f },
                    new LootEntry { ItemId = "bandage", Min = 1, Max = 1, Chance = 0.15f },
                    new LootEntry { ItemId = "canned_food", Min = 1, Max = 1, Chance = 0.12f },
                    new LootEntry { ItemId = "scrap", Min = 1, Max = 2, Chance = 0.28f },
                    new LootEntry { ItemId = "crystal_core", Min = 1, Max = 1, Chance = 0.10f },
                    new LootEntry { ItemId = "leather_jacket", Min = 1, Max = 1, Chance = 0.03f }
                }
            });
            db.LootTables.Add(new LootTableDef
            {
                Id = "loot.police", MinRolls = 2, MaxRolls = 4,
                Entries = new List<LootEntry>
                {
                    new LootEntry { Currency = "coin", Min = 2, Max = 7, Chance = 0.8f },
                    new LootEntry { ItemId = "ammo_9mm", Min = 4, Max = 9, Chance = 0.55f },
                    new LootEntry { ItemId = "pistol", Min = 1, Max = 1, Chance = 0.25f },
                    new LootEntry { ItemId = "kevlar_vest", Min = 1, Max = 1, Chance = 0.15f },
                    new LootEntry { ItemId = "police_cap", Min = 1, Max = 1, Chance = 0.22f },
                    new LootEntry { ItemId = "crystal_core", Min = 1, Max = 2, Chance = 0.35f }
                }
            });
            db.LootTables.Add(new LootTableDef
            {
                Id = "loot.soldier", MinRolls = 2, MaxRolls = 4,
                Entries = new List<LootEntry>
                {
                    new LootEntry { Currency = "coin", Min = 3, Max = 10, Chance = 0.7f },
                    new LootEntry { ItemId = "ammo_762", Min = 5, Max = 12, Chance = 0.55f },
                    new LootEntry { ItemId = "hunting_rifle", Min = 1, Max = 1, Chance = 0.22f },
                    new LootEntry { ItemId = "kevlar_vest", Min = 1, Max = 1, Chance = 0.25f },
                    new LootEntry { ItemId = "crystal_core", Min = 1, Max = 2, Chance = 0.5f }
                }
            });
            db.LootTables.Add(new LootTableDef
            {
                Id = "loot.hunter", MinRolls = 2, MaxRolls = 4,
                Entries = new List<LootEntry>
                {
                    new LootEntry { Currency = "coin", Min = 2, Max = 6, Chance = 0.6f },
                    new LootEntry { ItemId = "ammo_shell", Min = 3, Max = 7, Chance = 0.55f },
                    new LootEntry { ItemId = "shotgun", Min = 1, Max = 1, Chance = 0.2f },
                    new LootEntry { ItemId = "leather_jacket", Min = 1, Max = 1, Chance = 0.3f },
                    new LootEntry { ItemId = "crystal_core", Min = 1, Max = 1, Chance = 0.3f }
                }
            });
            db.LootTables.Add(new LootTableDef
            {
                Id = "loot.brute", MinRolls = 2, MaxRolls = 3,
                Entries = new List<LootEntry>
                {
                    new LootEntry { ItemId = "crystal_core", Min = 1, Max = 3, Chance = 0.85f },
                    new LootEntry { ItemId = "scrap", Min = 2, Max = 5, Chance = 0.7f },
                    new LootEntry { Currency = "coin", Min = 2, Max = 8, Chance = 0.5f }
                }
            });

            db.Zombies.Add(new ZombieDef
            {
                Id = "walker", Name = "行尸", Kind = "walker", Hp = 55f, Speed = 1.45f, Damage = 7f,
                Sight = 16f, LootTable = "loot.walker", Tint = "#6b7a55"
            });
            db.Zombies.Add(new ZombieDef
            {
                Id = "runner", Name = "疾行尸", Kind = "runner", Hp = 40f, Speed = 2.6f, Damage = 6f,
                Sight = 18f, LootTable = "loot.walker", Tint = "#7f6a4e"
            });
            db.Zombies.Add(new ZombieDef
            {
                Id = "police", Name = "警尸", Kind = "police", Hp = 80f, Speed = 1.5f, Damage = 9f,
                Sight = 17f, LootTable = "loot.police", Tint = "#334b63"
            });
            db.Zombies.Add(new ZombieDef
            {
                Id = "soldier", Name = "军尸", Kind = "soldier", Hp = 110f, Speed = 1.55f, Damage = 12f,
                Sight = 19f, LootTable = "loot.soldier", Tint = "#3d4d38"
            });
            db.Zombies.Add(new ZombieDef
            {
                Id = "hunter", Name = "猎尸", Kind = "hunter", Hp = 70f, Speed = 1.9f, Damage = 10f,
                Sight = 20f, LootTable = "loot.hunter", Tint = "#5b4030"
            });
            db.Zombies.Add(new ZombieDef
            {
                Id = "brute", Name = "巨尸", Kind = "brute", Hp = 190f, Speed = 1.15f, Damage = 18f,
                Sight = 14f, LootTable = "loot.brute", Tint = "#594a4a"
            });

            // ---------------------------------------------------------------
            // Traders: the system only takes crystal cores; NPCs take old coins.
            // ---------------------------------------------------------------
            db.Traders.Add(new TraderDef
            {
                Id = "trader_system",
                Name = "天机碑 · 石臼",
                Kind = "system",
                Currency = "crystal_core",
                Greeting = "碑面浮出一行字：晶核入臼，等价交换。",
                Offers = new List<TradeOfferDef>
                {
                    new TradeOfferDef { ItemId = "qi_pill", Price = 2, SellPrice = 1 },
                    new TradeOfferDef { ItemId = "bandage", Price = 1, SellPrice = 1, Stock = 20 },
                    new TradeOfferDef { ItemId = "ammo_9mm", Price = 1, SellPrice = 1, Stock = 30 },
                    new TradeOfferDef { ItemId = "spirit_herb", Price = 1, SellPrice = 1 },
                    new TradeOfferDef { ItemId = "wind_step_scroll", Price = 3, SellPrice = 1, Stock = 1 },
                    new TradeOfferDef { ItemId = "foundation_pill", Price = 8, SellPrice = 3, Stock = 2 }
                }
            });
            db.Traders.Add(new TraderDef
            {
                Id = "trader_laochen",
                Name = "老陈 · 杂货",
                Kind = "npc",
                Currency = "coin",
                Greeting = "老陈把算盘拨得噼啪响：「活人的钱，才叫钱。」",
                Offers = new List<TradeOfferDef>
                {
                    new TradeOfferDef { ItemId = "canned_food", Price = 5, SellPrice = 2, Stock = 12 },
                    new TradeOfferDef { ItemId = "water_bottle", Price = 4, SellPrice = 2, Stock = 12 },
                    new TradeOfferDef { ItemId = "bandage", Price = 6, SellPrice = 2, Stock = 8 },
                    new TradeOfferDef { ItemId = "ammo_762", Price = 8, SellPrice = 2, Stock = 15 },
                    new TradeOfferDef { ItemId = "leather_jacket", Price = 20, SellPrice = 8, Stock = 2 },
                    new TradeOfferDef { ItemId = "pistol", Price = 45, SellPrice = 18, Stock = 1 },
                    new TradeOfferDef { ItemId = "shotgun", Price = 65, SellPrice = 25, Stock = 1 }
                }
            });

            // ---------------------------------------------------------------
            // Story NPCs and dialogue. Actions are data: flag / quest / follow /
            // wait / trade / give. Conditions are flags/items.
            // ---------------------------------------------------------------
            db.Npcs.Add(new NpcDef
            {
                Id = "ahe", Name = "阿禾", Role = "守夜人", HomeX = 12.2f, HomeZ = 8.2f,
                WanderRadius = 2.2f, DialogueId = "dialogue.ahe", Tint = "#4f8f78", AtSafehouse = true
            });
            db.Npcs.Add(new NpcDef
            {
                Id = "laochen", Name = "老陈", Role = "杂货商", HomeX = -11.2f, HomeZ = 8.2f,
                WanderRadius = 1.6f, DialogueId = "dialogue.laochen", TraderId = "trader_laochen",
                Tint = "#7a6a4c", AtSafehouse = false
            });

            db.Dialogues.Add(new DialogueDef
            {
                Id = "dialogue.ahe", NpcId = "ahe", StartNode = "start",
                Nodes = new List<DialogueNodeDef>
                {
                    new DialogueNodeDef
                    {
                        Id = "start", Speaker = "阿禾",
                        Text = "你总算醒了。水比血重要——先去弄一瓶干净的水，活过今晚，再谈别的。",
                        Choices = new List<DialogueChoiceDef>
                        {
                            new DialogueChoiceDef
                            {
                                Text = "我会撑过今晚。", Action = "flag:met_ahe|quest:first_night", Next = ""
                            },
                            new DialogueChoiceDef { Text = "外面还有活人吗？", Next = "info" },
                            new DialogueChoiceDef { Text = "你为什么留下？", Next = "why" }
                        }
                    },
                    new DialogueNodeDef
                    {
                        Id = "info", Speaker = "阿禾",
                        Text = "街口有尸群，诊所里也许还有药。别在夜里点灯，光会招来不该来的东西。",
                        Choices = new List<DialogueChoiceDef>
                        {
                            new DialogueChoiceDef { Text = "记住了。", Action = "flag:met_ahe|quest:first_night", Next = "" }
                        }
                    },
                    new DialogueNodeDef
                    {
                        Id = "why", Speaker = "阿禾",
                        Text = "我答应过一个人，要守住这间屋子。后来他没能回来，我就替他守。",
                        Choices = new List<DialogueChoiceDef>
                        {
                            new DialogueChoiceDef { Text = "我帮你守。", Action = "flag:met_ahe|quest:first_night", Next = "" }
                        }
                    },
                    new DialogueNodeDef
                    {
                        Id = "follow", Speaker = "阿禾",
                        Text = "诊所的方向我熟。你走前面，我跟着；要是尸群太密，就退回安全屋。",
                        Choices = new List<DialogueChoiceDef>
                        {
                            new DialogueChoiceDef
                            {
                                Text = "一起走。", Action = "follow|quest:ahe_escort|flag:ahe_following", Next = ""
                            },
                            new DialogueChoiceDef { Text = "你留在安全屋。", Action = "wait", Next = "" }
                        }
                    },
                    new DialogueNodeDef
                    {
                        Id = "wait", Speaker = "阿禾",
                        Text = "我就在这儿。你回来的时候，门会开着。",
                        Choices = new List<DialogueChoiceDef>
                        {
                            new DialogueChoiceDef { Text = "好。", Next = "" },
                            new DialogueChoiceDef { Text = "还是跟我走。", Action = "follow", Next = "" }
                        }
                    },
                    new DialogueNodeDef
                    {
                        Id = "done", Speaker = "阿禾",
                        Text = "药拿到了，今晚能睡得踏实一点。你身上有股……不一样的气。",
                        Choices = new List<DialogueChoiceDef>
                        {
                            new DialogueChoiceDef { Text = "以后再说。", Next = "" },
                            new DialogueChoiceDef { Text = "教我。", Next = "teach" }
                        }
                    },
                    new DialogueNodeDef
                    {
                        Id = "teach", Speaker = "阿禾",
                        Text = "我只会守夜，不会修仙。但你要找的答案，也许在城北那口古井。",
                        Choices = new List<DialogueChoiceDef>
                        {
                            new DialogueChoiceDef { Text = "我会去看看。", Action = "flag:vein_hint", Next = "" }
                        }
                    }
                }
            });
            db.Dialogues.Add(new DialogueDef
            {
                Id = "dialogue.laochen", NpcId = "laochen", StartNode = "start",
                Nodes = new List<DialogueNodeDef>
                {
                    new DialogueNodeDef
                    {
                        Id = "start", Speaker = "老陈",
                        Text = "晶核？那东西天机碑收。我这儿只认旧币——活人的钱，才叫钱。",
                        Choices = new List<DialogueChoiceDef>
                        {
                            new DialogueChoiceDef
                            {
                                Text = "看看货。", Action = "flag:met_laochen|trade:trader_laochen", Next = ""
                            },
                            new DialogueChoiceDef { Text = "随便问问。", Next = "rumor" }
                        }
                    },
                    new DialogueNodeDef
                    {
                        Id = "rumor", Speaker = "老陈",
                        Text = "北边古井夜里发光；诊所后门有警尸；再往东，军尸守着半箱子弹。",
                        Choices = new List<DialogueChoiceDef>
                        {
                            new DialogueChoiceDef { Text = "谢了。", Action = "flag:met_laochen", Next = "" }
                        }
                    }
                }
            });

            var escort = new QuestDef
            {
                Id = "ahe_escort",
                Title = "诊所的路",
                Category = "story",
                Giver = "阿禾",
                GiverNpc = "ahe",
                Summary = "带阿禾去诊所取药，再回到安全屋。",
                Description = "阿禾知道诊所里还有药，但她一个人走不到。带她穿过街口，别让她死。",
                RequiresFlags = new List<string> { "met_ahe" },
                Objectives = new List<ObjectiveDef>
                {
                    new ObjectiveDef { Kind = "visit", Flag = "clinic", Label = "抵达诊所", Count = 1 },
                    new ObjectiveDef { Kind = "return_safehouse", Flag = "safehouse", Label = "返回安全屋", Count = 1 }
                },
                Rewards = new List<RewardDef>
                {
                    new RewardDef { Kind = "insight", Amount = 2 },
                    new RewardDef { Kind = "currency", Currency = "coin", Amount = 35 },
                    new RewardDef { Kind = "cultivation_xp", Amount = 45 }
                }
            };
            db.Quests.Add(escort);

            db.Rebuild();
            return db;
        }

        private static ItemDef Item(string id, string name, string desc, string category, float weight, int stack,
            params ItemUseDef[] use)
        {
            return new ItemDef
            {
                Id = id,
                Name = name,
                Description = desc,
                Category = category,
                Weight = weight,
                Stack = stack,
                Icon = "icons/items/" + id + ".png",
                Use = new List<ItemUseDef>(use)
            };
        }

        private static ItemUseDef Use(string kind, string need, float amount)
        {
            return new ItemUseDef { Kind = kind, Need = need, Amount = amount };
        }

        private static RealmDef Realm(int index, string id, string name, int stages, float qiMax, float xpBase,
            float xpGrowth, float hpBonus, float damageMult, float qiRegen, float chance, float dao, string itemId)
        {
            return new RealmDef
            {
                Index = index,
                Id = id,
                Name = name,
                Stages = stages,
                QiMax = qiMax,
                XpBase = xpBase,
                XpGrowth = xpGrowth,
                HpBonus = hpBonus,
                DamageMult = damageMult,
                QiRegen = qiRegen,
                BaseChance = chance,
                MinDaoHeart = dao,
                ItemId = itemId
            };
        }
    }
}

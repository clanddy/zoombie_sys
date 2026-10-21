using System;
using System.Collections.Generic;

namespace AshenDao.Sim
{
    [Serializable]
    public class CooldownSave
    {
        public string Id;
        public float Time;
    }

    [Serializable]
    public class QuestSave
    {
        public string Id;
        public int Status;
        public int TimesCompleted;
        public List<float> Progress = new List<float>();
    }

    [Serializable]
    public class IntPair
    {
        public string Id;
        public int Value;
    }

    [Serializable]
    public class StringPair
    {
        public string Id;
        public string Value;
    }

    [Serializable]
    public class TraderStockSave
    {
        public string TraderId;
        public string ItemId;
        public int Count;
    }

    [Serializable]
    public class StatsSave
    {
        public float SurvivalSeconds;
        public float GameMinutes;
        public float DistanceWalked;
        public float DistanceSprinted;
        public int Kills;
        public int Headshots;
        public int ItemsLooted;
        public int CrystalsCollected;
        public int Trades;
        public int QuestsCompleted;
        public int Dialogues;
        public int Noises;
        public int Shots;
        public int Reloads;
        public int TechniqueCasts;
        public int DamageDealt;
        public int DamageTaken;
        public int Deaths;
        public List<StatEntry> Counters = new List<StatEntry>();
        public List<GameEvent> Events = new List<GameEvent>();
    }

    [Serializable]
    public class SaveData
    {
        public int Version = 2;
        public int Day;
        public float TotalMinutes;
        public float Hp;
        public float Stamina;
        public float Hunger;
        public float Thirst;
        public float Fatigue;
        public float Infection;
        public bool Bleeding;
        public int RealmIndex;
        public int Stage;
        public float Xp;
        public float Qi;
        public float DaoHeart;
        public int Insight;
        public string Equipped;
        public int Coins;
        public int Crystals;
        public int SpiritStones;
        public int Kills;
        public int Nights;
        public int ItemsCollected;
        public int TechniquesUsed;
        public int BreakthroughAttempts;
        public int BreakthroughFailures;
        public List<ItemStack> Inventory = new List<ItemStack>();
        public List<string> Techniques = new List<string>();
        public List<CooldownSave> Cooldowns = new List<CooldownSave>();
        public List<QuestSave> Quests = new List<QuestSave>();
        public List<IntPair> TechniqueLevels = new List<IntPair>();
        public List<IntPair> Magazines = new List<IntPair>();
        public List<StringPair> Clothing = new List<StringPair>();
        public List<NpcState> Npcs = new List<NpcState>();
        public List<string> Flags = new List<string>();
        public List<TraderStockSave> TraderStock = new List<TraderStockSave>();
        public StatsSave Stats = new StatsSave();
    }

    public partial class WorldSim
    {
        public SaveData ExportSave()
        {
            var data = new SaveData
            {
                Day = Clock.Day,
                TotalMinutes = Clock.TotalMinutes,
                Hp = Player.Hp,
                Stamina = Player.Stamina,
                Hunger = Player.Hunger,
                Thirst = Player.Thirst,
                Fatigue = Player.Fatigue,
                Infection = Player.Infection,
                Bleeding = Player.Bleeding,
                RealmIndex = Player.RealmIndex,
                Stage = Player.Stage,
                Xp = Player.Xp,
                Qi = Player.Qi,
                DaoHeart = Player.DaoHeart,
                Insight = Player.Insight,
                Equipped = Player.Equipped,
                Coins = Wallet.Coin,
                Crystals = Wallet.Crystal,
                SpiritStones = Wallet.SpiritStone,
                Kills = Player.Kills,
                Nights = Player.Nights,
                ItemsCollected = Player.ItemsCollected,
                TechniquesUsed = Player.TechniquesUsed,
                BreakthroughAttempts = Player.BreakthroughAttempts,
                BreakthroughFailures = Player.BreakthroughFailures
            };

            foreach (var stack in Player.Inventory)
            {
                data.Inventory.Add(new ItemStack(stack.ItemId, stack.Count));
            }
            foreach (var tech in Player.Techniques) data.Techniques.Add(tech);
            foreach (var pair in Player.Cooldowns)
            {
                if (pair.Value > 0f) data.Cooldowns.Add(new CooldownSave { Id = pair.Key, Time = pair.Value });
            }
            foreach (var pair in Player.TechniqueLevels)
            {
                data.TechniqueLevels.Add(new IntPair { Id = pair.Key, Value = pair.Value });
            }
            foreach (var pair in Player.Magazines)
            {
                data.Magazines.Add(new IntPair { Id = pair.Key, Value = pair.Value });
            }
            foreach (var pair in Player.Clothing)
            {
                data.Clothing.Add(new StringPair { Id = pair.Key, Value = pair.Value });
            }
            foreach (var npc in Story.AllStates) data.Npcs.Add(npc);
            foreach (var flag in Story.Flags) data.Flags.Add(flag);
            data.TraderStock = Trade.ExportStock();
            data.Stats = new StatsSave
            {
                SurvivalSeconds = Stats.SurvivalSeconds,
                GameMinutes = Stats.GameMinutes,
                DistanceWalked = Stats.DistanceWalked,
                DistanceSprinted = Stats.DistanceSprinted,
                Kills = Stats.Kills,
                Headshots = Stats.Headshots,
                ItemsLooted = Stats.ItemsLooted,
                CrystalsCollected = Stats.CrystalsCollected,
                Trades = Stats.Trades,
                QuestsCompleted = Stats.QuestsCompleted,
                Dialogues = Stats.Dialogues,
                Noises = Stats.Noises,
                Shots = Stats.Shots,
                Reloads = Stats.Reloads,
                TechniqueCasts = Stats.TechniqueCasts,
                DamageDealt = Stats.DamageDealt,
                DamageTaken = Stats.DamageTaken,
                Deaths = Stats.Deaths
            };
            data.Stats.Counters.AddRange(Stats.Counters);
            for (var i = Math.Max(0, Stats.Events.Count - 60); i < Stats.Events.Count; i++)
                data.Stats.Events.Add(Stats.Events[i]);
            foreach (var runtime in Quests.AllRuntimes())
            {
                var quest = new QuestSave
                {
                    Id = runtime.Id,
                    Status = (int)runtime.Status,
                    TimesCompleted = runtime.TimesCompleted
                };
                if (runtime.Progress != null) quest.Progress.AddRange(runtime.Progress);
                data.Quests.Add(quest);
            }
            return data;
        }

        public void ImportSave(SaveData data)
        {
            if (data == null) return;

            Clock.TotalMinutes = data.TotalMinutes;
            Clock.MinuteOfDay = data.TotalMinutes % 1440f;
            Clock.Day = data.Day;

            Player.Hp = data.Hp;
            Player.Stamina = data.Stamina;
            Player.Hunger = data.Hunger;
            Player.Thirst = data.Thirst;
            Player.Fatigue = data.Fatigue;
            Player.Infection = data.Infection;
            Player.Bleeding = data.Bleeding;
            Player.RealmIndex = data.RealmIndex;
            Player.Stage = data.Stage;
            Player.Xp = data.Xp;
            Player.Qi = data.Qi;
            Player.DaoHeart = data.DaoHeart;
            Player.Insight = data.Insight;
            Player.Equipped = data.Equipped;
            Wallet.Coin = data.Coins;
            Wallet.Crystal = data.Crystals;
            Wallet.SpiritStone = data.SpiritStones;
            Player.Kills = data.Kills;
            Player.Nights = data.Nights;
            Player.ItemsCollected = data.ItemsCollected;
            Player.TechniquesUsed = data.TechniquesUsed;
            Player.BreakthroughAttempts = data.BreakthroughAttempts;
            Player.BreakthroughFailures = data.BreakthroughFailures;
            Player.Meditating = false;
            Player.Dead = data.Hp <= 0f;
            Meditating = false;

            Player.Inventory.Clear();
            foreach (var stack in data.Inventory) Player.Inventory.Add(new ItemStack(stack.ItemId, stack.Count));
            Player.Techniques.Clear();
            foreach (var tech in data.Techniques) Player.Techniques.Add(tech);
            Player.Cooldowns.Clear();
            foreach (var cooldown in data.Cooldowns) Player.Cooldowns[cooldown.Id] = cooldown.Time;
            Player.TechniqueLevels.Clear();
            foreach (var pair in data.TechniqueLevels) Player.TechniqueLevels[pair.Id] = pair.Value;
            Player.Magazines.Clear();
            foreach (var pair in data.Magazines) Player.Magazines[pair.Id] = pair.Value;
            Player.Clothing.Clear();
            foreach (var pair in data.Clothing) Player.Clothing[pair.Id] = pair.Value;
            foreach (var npc in data.Npcs) Story.Restore(npc);
            foreach (var flag in data.Flags) Story.RestoreFlag(flag);
            Trade.ImportStock(data.TraderStock);
            if (data.Stats != null)
            {
                Stats.SurvivalSeconds = data.Stats.SurvivalSeconds;
                Stats.GameMinutes = data.Stats.GameMinutes;
                Stats.DistanceWalked = data.Stats.DistanceWalked;
                Stats.DistanceSprinted = data.Stats.DistanceSprinted;
                Stats.Kills = data.Stats.Kills;
                Stats.Headshots = data.Stats.Headshots;
                Stats.ItemsLooted = data.Stats.ItemsLooted;
                Stats.CrystalsCollected = data.Stats.CrystalsCollected;
                Stats.Trades = data.Stats.Trades;
                Stats.QuestsCompleted = data.Stats.QuestsCompleted;
                Stats.Dialogues = data.Stats.Dialogues;
                Stats.Noises = data.Stats.Noises;
                Stats.Shots = data.Stats.Shots;
                Stats.Reloads = data.Stats.Reloads;
                Stats.TechniqueCasts = data.Stats.TechniqueCasts;
                Stats.DamageDealt = data.Stats.DamageDealt;
                Stats.DamageTaken = data.Stats.DamageTaken;
                Stats.Deaths = data.Stats.Deaths;
                Stats.Counters.Clear();
                Stats.Counters.AddRange(data.Stats.Counters);
                Stats.Events.Clear();
                Stats.Events.AddRange(data.Stats.Events);
            }
            foreach (var quest in data.Quests)
            {
                Quests.Restore(quest.Id, (QuestStatus)quest.Status, quest.TimesCompleted, quest.Progress.ToArray());
            }

            RecalculateMaxHp();
            Player.Hp = Math.Min(Player.Hp, Player.MaxHp);
        }
    }
}

using System;
using System.Collections.Generic;

namespace AshenDao.Sim
{
    public partial class WorldSim
    {
        public int RandomInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return random.Next(minInclusive, maxExclusive);
        }

        public float RandomFloat()
        {
            return (float)random.NextDouble();
        }

        public int TechniqueLevel(string id)
        {
            if (string.IsNullOrEmpty(id)) return 1;
            return Player.TechniqueLevels.TryGetValue(id, out var level) ? Math.Max(1, level) : 1;
        }

        public int TechniqueUpgradeCost(string id)
        {
            var def = Content.Technique(id);
            if (def == null) return 0;
            return Math.Max(1, def.InsightCost * TechniqueLevel(id));
        }

        public bool UpgradeTechnique(string id)
        {
            var def = Content.Technique(id);
            if (def == null || !Player.Techniques.Contains(id))
            {
                Notify("尚未习得此功法。", NoticeTone.Warn);
                return false;
            }
            var level = TechniqueLevel(id);
            if (level >= Math.Max(1, def.MaxLevel))
            {
                Notify("《" + def.Name + "》已至圆满。", NoticeTone.Info);
                return false;
            }
            if (Player.RealmIndex < def.RequiresRealm)
            {
                Notify("境界不足。", NoticeTone.Warn);
                return false;
            }
            var cost = TechniqueUpgradeCost(id);
            if (Player.Insight < cost)
            {
                Notify("悟性不足（需 " + cost + "）。", NoticeTone.Warn);
                return false;
            }

            Player.Insight -= cost;
            Player.TechniqueLevels[id] = level + 1;
            RecalculateMaxHp();
            RecordEvent(GameEventType.Technique, "upgrade." + id,
                "《" + def.Name + "》提升至 " + (level + 1) + " 层", level + 1);
            Notify("《" + def.Name + "》提升至 " + (level + 1) + " 层。", NoticeTone.Good);
            return true;
        }

        public float MoveSpeed => 4.1f * (SpeedBuffUntil > 0f ? SpeedBuffMult : 1f);
        public float SprintSpeed => 6.1f * (SpeedBuffUntil > 0f ? SpeedBuffMult : 1f);

        public string SpeedBuffText => SpeedBuffUntil > Clock.TotalMinutes
            ? SpeedBuffName + " " + Math.Max(0f, SpeedBuffUntil - Clock.TotalMinutes).ToString("0.0") + " 分"
            : "";

        public void RecordSurvivalTick(float dt)
        {
            // Survival time is accumulated in Stats.Tick. Kept as a named seam for
            // future session/telemetry systems that need per-tick hooks.
        }

        public void RecordMovement(float distance, bool sprinting)
        {
            if (distance <= 0f) return;
            if (sprinting) Stats.DistanceSprinted += distance;
            else Stats.DistanceWalked += distance;
        }

        public GameEvent RecordEvent(GameEventType type, string key, string text, int amount = 1)
        {
            var evt = Stats.Record(type, key, text, amount, Clock);
            if (type == GameEventType.Kill || type == GameEventType.Story || type == GameEventType.Quest)
            {
                MessageLog.Add("第" + evt.Day + "天 " + evt.Minute.ToString("000") + " · " + text);
                if (MessageLog.Count > 80) MessageLog.RemoveAt(0);
            }
            return evt;
        }

        public NoiseEvent EmitNoise(string source, float x, float z, float radius, float loudness = 1f)
        {
            var evt = Noise.Emit(source, x, z, radius, loudness, Clock.TotalMinutes);
            Stats.Noises++;
            if (radius >= 8f)
            {
                RecordEvent(GameEventType.Noise, "noise." + source, source + "（半径 " + (int)radius + "m）", 1);
            }
            return evt;
        }

        public void OnZombieKilled(ZombieDef def)
        {
            var kind = def != null ? def.Kind : "walker";
            Player.Kills++;
            Stats.Kills++;
            Stats.Add("kill." + kind);
            Quests.NotifyKill(kind);
            RecordEvent(GameEventType.Kill, "kill." + kind,
                "击杀" + (def != null ? def.Name : "行尸"), 1);
        }

        public void RecordDamageToZombie(float damage, bool headshot)
        {
            Stats.DamageDealt += Math.Max(0, (int)Math.Round(damage));
            if (headshot) Stats.Headshots++;
        }

        public NpcState Npc(string id)
        {
            return Story.State(id);
        }

        public DialogueSession StartDialogue(string npcId)
        {
            return Story.Start(npcId);
        }

        public DialogueSession ChooseDialogue(DialogueSession session, int index)
        {
            return Story.Choose(session, index);
        }

        public string ConsumePendingTrade()
        {
            return Story.ConsumePendingTrade();
        }

        public List<LootResult> RollLoot(string tableId)
        {
            return Loot.Roll(tableId);
        }

        public bool Buy(string traderId, string itemId, int count = 1)
        {
            return Trade.Buy(traderId, itemId, count);
        }

        public bool Sell(string traderId, string itemId, int count = 1)
        {
            return Trade.Sell(traderId, itemId, count);
        }

        public bool SetFlag(string flag)
        {
            Story.SetFlag(flag);
            return true;
        }

        public string CurrencyText
        {
            get { return "旧币 " + Wallet.Coin + " · 晶核 " + CountItem("crystal_core"); }
        }
    }
}

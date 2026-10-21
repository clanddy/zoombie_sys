using System;
using System.Collections.Generic;
using System.Linq;

namespace AshenDao.Sim
{
    public enum QuestStatus
    {
        Available,
        Locked,
        Active,
        Completed
    }

    public class QuestRuntime
    {
        public string Id;
        public QuestStatus Status;
        public float[] Progress;
        public int TimesCompleted;
        public bool AutoAccepted;
    }

    public class QuestSystem
    {
        private readonly WorldSim sim;
        private readonly Dictionary<string, QuestRuntime> runtimes = new Dictionary<string, QuestRuntime>();

        public QuestSystem(WorldSim world)
        {
            sim = world;
        }

        public void AutoAccept()
        {
            foreach (var quest in sim.Content.Quests)
            {
                if (quest.AutoAccept) Accept(quest.Id);
            }
        }

        public QuestRuntime Runtime(string id)
        {
            return runtimes.TryGetValue(id, out var runtime) ? runtime : null;
        }

        public bool IsCompleted(string id)
        {
            var runtime = Runtime(id);
            return runtime != null && runtime.Status == QuestStatus.Completed;
        }

        public bool RequirementsMet(QuestDef def)
        {
            if (def.RequiresRealm > 0 && sim.Player.RealmIndex < def.RequiresRealm) return false;
            foreach (var required in def.RequiresQuests)
            {
                if (!IsCompleted(required)) return false;
            }
            foreach (var flag in def.RequiresFlags)
            {
                if (!sim.Story.HasFlag(flag)) return false;
            }
            return true;
        }

        public bool CanAccept(QuestDef def)
        {
            if (def == null) return false;
            var runtime = Runtime(def.Id);
            if (runtime != null)
            {
                if (runtime.Status == QuestStatus.Active) return false;
                if (runtime.Status == QuestStatus.Completed && !def.Repeatable) return false;
            }
            return RequirementsMet(def);
        }

        public QuestStatus Status(QuestDef def)
        {
            var runtime = Runtime(def.Id);
            if (runtime != null && runtime.Status == QuestStatus.Active) return QuestStatus.Active;
            if (runtime != null && runtime.Status == QuestStatus.Completed)
            {
                if (def.Repeatable && RequirementsMet(def)) return QuestStatus.Available;
                return QuestStatus.Completed;
            }
            return CanAccept(def) ? QuestStatus.Available : QuestStatus.Locked;
        }

        public bool Accept(string id)
        {
            var def = sim.Content.Quest(id);
            if (def == null || !CanAccept(def)) return false;

            var runtime = Runtime(id);
            if (runtime == null)
            {
                runtime = new QuestRuntime { Id = id };
                runtimes[id] = runtime;
            }

            runtime.Status = QuestStatus.Active;
            runtime.Progress = new float[def.Objectives.Count];
            for (var i = 0; i < def.Objectives.Count; i++)
            {
                var obj = def.Objectives[i];
                if (obj.Kind == "collect")
                {
                    runtime.Progress[i] = Math.Min(obj.Count, sim.CountItem(obj.ItemId));
                }
                else if (obj.Kind == "reach_realm")
                {
                    runtime.Progress[i] = sim.Player.RealmIndex >= obj.RealmIndex ? 1f : 0f;
                }
                else if ((obj.Kind == "visit" || obj.Kind == "return_safehouse" || obj.Kind == "talk") &&
                         !string.IsNullOrEmpty(obj.Flag) && sim.Story.HasFlag(obj.Flag))
                {
                    runtime.Progress[i] = 1f;
                }
            }

            sim.Notify("接受任务：" + def.Title, NoticeTone.Info);
            CheckCompletion(def, runtime);
            return true;
        }

        public List<QuestDef> QuestsWithStatus(QuestStatus status)
        {
            return sim.Content.Quests.Where(q => Status(q) == status).ToList();
        }

        public void NotifyKill(string enemyId)
        {
            foreach (var def in ActiveDefs()) ProgressBy(def, obj => obj.Kind == "kill" && (string.IsNullOrEmpty(obj.EnemyId) || obj.EnemyId == enemyId) ? 1f : 0f);
        }

        public void NotifyCollect(string itemId, int count)
        {
            foreach (var def in ActiveDefs()) ProgressBy(def, obj => obj.Kind == "collect" && obj.ItemId == itemId ? count : 0f);
        }

        public void NotifyMeditate(float minutes)
        {
            foreach (var def in ActiveDefs()) ProgressBy(def, obj => obj.Kind == "meditate" ? minutes : 0f);
        }

        public void NotifyCast(string techniqueId)
        {
            foreach (var def in ActiveDefs()) ProgressBy(def, obj => obj.Kind == "cast_technique" && obj.TechniqueId == techniqueId ? 1f : 0f);
        }

        public void NotifyRealm(int realmIndex)
        {
            foreach (var def in ActiveDefs())
            {
                ProgressBy(def, obj => obj.Kind == "reach_realm" && realmIndex >= obj.RealmIndex ? 1f : 0f);
            }
        }

        public void NotifyDawn()
        {
            foreach (var def in ActiveDefs()) ProgressBy(def, obj => obj.Kind == "survive_night" ? 1f : 0f);
        }

        public void NotifyFlag(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return;
            foreach (var def in ActiveDefs())
            {
                ProgressBy(def, obj =>
                    (obj.Kind == "visit" || obj.Kind == "return_safehouse" || obj.Kind == "talk") &&
                    obj.Flag == flag ? 1f : 0f);
            }
        }

        private List<QuestDef> ActiveDefs()
        {
            return sim.Content.Quests
                .Where(q => Runtime(q.Id) != null && Runtime(q.Id).Status == QuestStatus.Active)
                .ToList();
        }

        private void ProgressBy(QuestDef def, Func<ObjectiveDef, float> amount)
        {
            var runtime = Runtime(def.Id);
            if (runtime == null || runtime.Status != QuestStatus.Active) return;
            for (var i = 0; i < def.Objectives.Count; i++)
            {
                var obj = def.Objectives[i];
                var add = amount(obj);
                if (add <= 0f) continue;
                var target = Target(obj);
                runtime.Progress[i] = Math.Min(target, runtime.Progress[i] + add);
            }
            CheckCompletion(def, runtime);
        }

        public float Target(ObjectiveDef obj)
        {
            if (obj.Kind == "meditate") return obj.Minutes;
            if (obj.Kind == "reach_realm") return 1f;
            return obj.Count;
        }

        private void CheckCompletion(QuestDef def, QuestRuntime runtime)
        {
            if (runtime.Status != QuestStatus.Active) return;
            for (var i = 0; i < def.Objectives.Count; i++)
            {
                if (runtime.Progress[i] < Target(def.Objectives[i])) return;
            }

            runtime.Status = QuestStatus.Completed;
            runtime.TimesCompleted++;
            sim.Stats.QuestsCompleted++;
            sim.RecordEvent(GameEventType.Quest, "complete." + def.Id, "任务完成：" + def.Title, 1);
            sim.Notify("任务完成：" + def.Title, NoticeTone.Good);
            foreach (var reward in def.Rewards) sim.GrantReward(reward);

            foreach (var next in sim.Content.Quests)
            {
                if (next.RequiresQuests.Contains(def.Id) && CanAccept(next))
                {
                    sim.Notify("新任务可接取：" + next.Title, NoticeTone.Info);
                }
            }
        }

        public string ObjectiveText(ObjectiveDef obj)
        {
            switch (obj.Kind)
            {
                case "kill": return "击杀" + (string.IsNullOrEmpty(obj.EnemyId) ? "行尸" : obj.EnemyId);
                case "collect":
                    var item = sim.Content.Item(obj.ItemId);
                    return "收集" + (item != null ? item.Name : obj.ItemId);
                case "survive_night": return "熬过夜晚";
                case "meditate": return "打坐吐纳 " + (int)obj.Minutes + " 分钟";
                case "cast_technique":
                    var tech = sim.Content.Technique(obj.TechniqueId);
                    return "施展" + (tech != null ? tech.Name : obj.TechniqueId);
                case "reach_realm":
                    var realm = sim.Content.Realm(obj.RealmIndex);
                    return "达到" + (realm != null ? realm.Name : "境界 " + obj.RealmIndex);
                case "visit":
                    return string.IsNullOrEmpty(obj.Label) ? "抵达目标地点" : obj.Label;
                case "return_safehouse":
                    return string.IsNullOrEmpty(obj.Label) ? "返回安全屋" : obj.Label;
                case "talk":
                    return string.IsNullOrEmpty(obj.Label) ? "与目标交谈" : obj.Label;
                case "escort":
                    return string.IsNullOrEmpty(obj.Label) ? "护送目标" : obj.Label;
                default: return obj.Label ?? "未知目标";
            }
        }

        public string RewardText(RewardDef reward)
        {
            switch (reward.Kind)
            {
                case "item":
                    var item = sim.Content.Item(reward.ItemId);
                    return (item != null ? item.Name : reward.ItemId) + " ×" + reward.Count;
                case "cultivation_xp": return "修为 +" + reward.Amount;
                case "qi": return "灵气 +" + reward.Amount;
                case "technique":
                    var tech = sim.Content.Technique(reward.TechniqueId);
                    return "功法《" + (tech != null ? tech.Name : reward.TechniqueId) + "》";
                case "dao_heart": return "道心 +" + reward.Amount;
                case "insight": return "悟性 +" + reward.Amount;
                case "currency": return sim.Trade.CurrencyName(reward.Currency) + " +" + reward.Amount;
                default: return "未知奖励";
            }
        }

        public List<QuestRuntime> AllRuntimes()
        {
            return runtimes.Values.ToList();
        }

        public void Restore(string id, QuestStatus status, int timesCompleted, float[] progress)
        {
            var def = sim.Content.Quest(id);
            if (def == null) return;
            runtimes[id] = new QuestRuntime
            {
                Id = id,
                Status = status,
                TimesCompleted = timesCompleted,
                Progress = progress ?? new float[def.Objectives.Count]
            };
        }

        public bool TryGetProgress(string questId, int index, out float current)
        {
            current = 0f;
            var runtime = Runtime(questId);
            if (runtime == null || runtime.Progress == null || index >= runtime.Progress.Length) return false;
            current = runtime.Progress[index];
            return true;
        }
    }
}

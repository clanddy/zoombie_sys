using System;
using System.Collections.Generic;

namespace AshenDao.Sim
{
    public enum GameEventType
    {
        Kill = 1,
        Loot = 2,
        Trade = 3,
        Quest = 4,
        Dialogue = 5,
        Noise = 6,
        Weapon = 7,
        Technique = 8,
        Story = 9,
        Escort = 10,
        Damage = 11,
        Craft = 12
    }

    [Serializable]
    public class GameEvent
    {
        public int Id;
        public int Type;
        public string Key;
        public string Text;
        public int Amount;
        public int Day;
        public float Minute;

        public GameEventType EventType => (GameEventType)Type;
    }

    [Serializable]
    public class StatEntry
    {
        public string Key;
        public int Count;

        public StatEntry() { }

        public StatEntry(string key, int count)
        {
            Key = key;
            Count = count;
        }
    }

    /// <summary>
    /// Session statistics and a bounded event timeline. The timeline is deliberately
    /// separate from notices: it is the analytical record used by the phone's 行迹 app,
    /// death screen, achievements and future telemetry/export.
    /// </summary>
    public class RunStats
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

        public readonly List<StatEntry> Counters = new List<StatEntry>();
        public readonly List<GameEvent> Events = new List<GameEvent>();

        private int nextEventId = 1;

        public void Tick(float dt, float gameMinutes)
        {
            SurvivalSeconds += Math.Max(0f, dt);
            GameMinutes += Math.Max(0f, gameMinutes);
        }

        public void Add(string key, int amount = 1)
        {
            if (string.IsNullOrEmpty(key) || amount == 0) return;
            for (var i = 0; i < Counters.Count; i++)
            {
                if (Counters[i].Key != key) continue;
                Counters[i].Count += amount;
                return;
            }
            Counters.Add(new StatEntry(key, amount));
        }

        public int Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            for (var i = 0; i < Counters.Count; i++)
            {
                if (Counters[i].Key == key) return Counters[i].Count;
            }
            return 0;
        }

        public GameEvent Record(GameEventType type, string key, string text, int amount, GameClock clock)
        {
            var evt = new GameEvent
            {
                Id = nextEventId++,
                Type = (int)type,
                Key = key ?? "",
                Text = text ?? "",
                Amount = amount,
                Day = clock != null ? clock.Day : 0,
                Minute = clock != null ? clock.MinuteOfDay : 0f
            };
            Events.Add(evt);
            if (Events.Count > 120) Events.RemoveAt(0);
            return evt;
        }

        public string SurvivalText
        {
            get
            {
                var total = (int)SurvivalSeconds;
                return (total / 3600).ToString("00") + ":" + ((total / 60) % 60).ToString("00") + ":" +
                       (total % 60).ToString("00");
            }
        }

        public string GameTimeText
        {
            get
            {
                var total = (int)GameMinutes;
                return (total / 60) + " 小时 " + (total % 60) + " 分";
            }
        }

        public string HhMm(float minuteOfDay)
        {
            var total = (int)minuteOfDay;
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
    }
}

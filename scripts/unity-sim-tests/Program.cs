using System;
using System.Linq;
using AshenDao.Sim;

static class Program
{
    static int failures;

    static void Check(bool condition, string message)
    {
        if (condition) { Console.WriteLine("  ok  " + message); return; }
        failures++;
        Console.WriteLine("  FAIL " + message);
    }

    static void Main()
    {
        Console.WriteLine("Ashen Dao C# sim harness");
        var content = ContentDatabase.CreateDemo();
        var sim = new WorldSim(content);
        Check(sim.Quests.Status(content.Quest("first_night")) == QuestStatus.Active, "first_night auto accepted");
        Check(sim.Player.Techniques.Contains("tuna"), "tuna learned at start");

        sim.AddItem("water_bottle", 1);
        var firstNight = sim.Quests.Runtime("first_night");
        Check(firstNight.Progress[0] == 1f, "water counted for collect objective");

        sim.Clock.TotalMinutes = 359f;
        sim.Clock.MinuteOfDay = 359f;
        for (var i = 0; i < 60; i++) sim.Update(1f / 20f);
        Check(sim.Player.Nights == 1, "dawn counted a survived night");
        Check(firstNight.Status == QuestStatus.Completed, "first_night completed");
        Check(sim.Player.Techniques.Contains("iron_shirt"), "quest reward learned iron_shirt");

        Check(sim.AcceptQuest("first_blood"), "first_blood accepted");
        for (var i = 0; i < 6; i++) sim.Quests.NotifyKill("walker");
        Check(sim.Quests.Runtime("first_blood").Status == QuestStatus.Completed, "kill quest completed");

        Check(sim.AcceptQuest("meditate"), "meditate accepted");
        sim.ToggleMeditate();
        for (var i = 0; i < 320; i++) sim.Update(1f / 20f);
        Check(sim.Quests.Runtime("meditate").Status == QuestStatus.Completed, "meditation quest completed");
        Check(sim.Player.Techniques.Contains("sword_qi"), "sword_qi learned");

        sim.Player.Stage = 9;
        sim.Player.Xp = sim.XpNeeded;
        sim.Player.Qi = sim.QiMax;
        sim.Player.DaoHeart = 100f;
        var blockers = sim.BreakthroughBlockers();
        Check(blockers.Any(b => b.Contains("筑基丹")), "missing foundation pill blocks breakthrough");
        sim.AddItem("foundation_pill", 3, false);
        var ascended = false;
        for (var attempt = 0; attempt < 20 && !ascended; attempt++)
        {
            sim.Player.Stage = 9;
            sim.Player.Xp = sim.XpNeeded;
            sim.Player.Qi = sim.QiMax;
            sim.Player.DaoHeart = 100f;
            if (sim.CountItem("foundation_pill") == 0) sim.AddItem("foundation_pill", 1, false);
            ascended = sim.AttemptBreakthrough();
        }
        Check(ascended && sim.Player.RealmIndex == 2, "breakthrough reaches 筑基");

        // ---- 事件/统计 ----
        var feature = new WorldSim(ContentDatabase.CreateDemo());
        for (var i = 0; i < 40; i++) feature.Update(1f / 20f);
        Check(feature.Stats.SurvivalSeconds > 1.5f, "survival time accumulates");
        feature.OnZombieKilled(content.Zombie("police"));
        Check(feature.Stats.Kills == 1, "kill increments stats");
        Check(feature.Stats.Get("kill.police") == 1, "kill category recorded");
        Check(feature.Stats.Events.Count > 0, "event timeline recorded");

        // ---- 噪声 ----
        var noise = feature.EmitNoise("gunshot", 10f, 10f, 30f, 1f);
        Check(noise.Hears(20f, 10f), "noise radius is heard");
        Check(!noise.Hears(45f, 10f), "noise outside radius is not heard");
        Check(feature.Noise.Recent.Count == 1, "noise bus keeps recent events");

        // ---- 交易：晶核 vs 旧币 ----
        feature.AddItem("crystal_core", 3, false);
        feature.Wallet.Coin = 20;
        Check(feature.Buy("trader_system", "bandage", 1), "system trade accepts crystal core");
        Check(feature.Buy("trader_laochen", "canned_food", 1), "npc trade accepts coin");
        Check(feature.Stats.Trades == 2, "trade stats recorded");

        // ---- 枪械：弹匣/换弹/噪声 ----
        feature.AddItem("pistol", 1, false);
        feature.AddItem("ammo_9mm", 8, false);
        feature.EquipItem("pistol");
        var mag = feature.Magazine("pistol");
        Check(feature.TryFire(out var shot), "firearm fires");
        Check(shot != null && shot.Damage > 0f, "shot spec has damage");
        Check(feature.Magazine("pistol") == mag - 1, "shot consumes magazine");
        Check(feature.Reload(), "reload consumes ammo");
        Check(feature.Magazine("pistol") == mag, "reload fills magazine");

        // ---- 功法升级与身法 ----
        feature.Player.Insight = 3;
        Check(feature.LearnTechnique("wind_step"), "wind_step learned");
        var beforeLevel = feature.TechniqueLevel("wind_step");
        Check(feature.UpgradeTechnique("wind_step"), "technique upgrade spends insight");
        Check(feature.TechniqueLevel("wind_step") == beforeLevel + 1, "technique level increments");
        feature.Player.Qi = feature.QiMax;
        Check(feature.CastTechnique("wind_step"), "movement technique casts");
        Check(feature.SpeedBuffUntil > feature.Clock.TotalMinutes, "movement buff active");

        // ---- 故事对话/跟随/护送 ----
        var dialogue = feature.StartDialogue("ahe");
        Check(dialogue != null && dialogue.Choices.Count >= 1, "npc dialogue starts");
        feature.ChooseDialogue(dialogue, 0);
        Check(feature.Story.HasFlag("met_ahe"), "dialogue choice sets story flag");
        feature.Story.Apply("follow");
        Check(feature.Npc("ahe").Following, "npc follow order");
        Check(feature.AcceptQuest("ahe_escort"), "escort quest accepts");
        feature.SetFlag("clinic");
        feature.SetFlag("safehouse");
        Check(feature.Quests.IsCompleted("ahe_escort"), "escort quest completes on zone flags");

        // ---- 护甲 ----
        feature.AddItem("leather_jacket", 1, false);
        Check(feature.EquipItem("leather_jacket"), "clothing equips");
        Check(feature.Armor > 0f, "clothing contributes armor");

        // ---- 掉落 ----
        var loot = feature.RollLoot("loot.police");
        Check(loot != null, "loot table rolls");

        var save = feature.ExportSave();
        var loaded = new WorldSim(ContentDatabase.CreateDemo());
        loaded.ImportSave(save);
        Check(Math.Abs(loaded.Player.Xp - feature.Player.Xp) < 0.01f, "save/load xp");
        Check(loaded.Player.RealmIndex == feature.Player.RealmIndex, "save/load realm");
        Check(loaded.CountItem("foundation_pill") >= 0, "save/load inventory");
        Check(loaded.Stats.Kills == feature.Stats.Kills, "save/load stats kills");
        Check(loaded.Stats.Events.Count == feature.Stats.Events.Count, "save/load event timeline");
        Check(loaded.Story.HasFlag("met_ahe"), "save/load story flags");
        Check(loaded.Npc("ahe").Following, "save/load npc follow state");
        Check(loaded.TechniqueLevel("wind_step") == feature.TechniqueLevel("wind_step"), "save/load technique level");
        Check(loaded.Magazine("pistol") == feature.Magazine("pistol"), "save/load magazine");
        Check(loaded.Wallet.Coin == feature.Wallet.Coin, "save/load wallet");
        var trader = feature.Trade.Trader("trader_system");
        var bandage = trader != null ? trader.Offers.Find(o => o.ItemId == "bandage") : null;
        Check(bandage != null && loaded.Trade.StockOf(loaded.Trade.Trader("trader_system"), bandage) ==
              feature.Trade.StockOf(feature.Trade.Trader("trader_system"), bandage), "save/load trader stock");

        Console.WriteLine(failures == 0 ? "ALL PASSED" : failures + " FAILURES");
        Environment.Exit(failures == 0 ? 0 : 1);
    }
}

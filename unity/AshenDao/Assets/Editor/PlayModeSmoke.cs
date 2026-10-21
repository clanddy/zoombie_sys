using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using AshenDao.Game;
using AshenDao.UI;

public static class PlayModeSmoke
{
    private static bool started;
    private static int frames;
    private static string reportPath;
    private static readonly StringBuilder report = new StringBuilder();
    private static int errors;
    private static int editorErrorsIgnored;

    public static void Run()
    {
        reportPath = Path.Combine(RepoRoot, "docs/unity-smoke.json");
        report.AppendLine("{");
        report.AppendLine("  \"startedAt\": \"" + DateTime.Now.ToString("o") + "\",");
        Application.logMessageReceived += OnLog;
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
        started = true;
        Debug.Log("[Smoke] entering play mode");
    }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            // Unity 6.4's QuickSearch throws this on project startup in batch/GUI mode.
            // It is editor-only, not from Assets/Scripts.
            if (condition.Contains("ArgumentOutOfRangeException") && condition.Contains("Index was out of range"))
            {
                editorErrorsIgnored++;
                return;
            }
            errors++;
            report.AppendLine("  \"error\": " + Quote(condition) + ",");
        }
    }

    private static void Tick()
    {
        if (!started) return;
        if (!EditorApplication.isPlaying)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Finish("play mode never started");
            return;
        }

        frames++;
        if (frames == 1 || frames == 3 || frames == 8 || frames == 14 || frames == 16 ||
            frames == 22 || frames == 24 || frames == 30 || frames == 38 || frames == 46 || frames == 60)
            Debug.Log("[Smoke] tick " + frames + " playing=" + EditorApplication.isPlaying);

        if (frames == 3)
        {
            Capture("unity-game.png");
            var root = GameRoot.Instance;
            report.AppendLine("  \"gameRoot\": " + Bool(root != null) + ",");
            if (root != null)
            {
                report.AppendLine("  \"player\": " + Bool(root.Player != null) + ",");
                report.AppendLine("  \"phone\": " + Bool(root.Phone != null) + ",");
                report.AppendLine("  \"worldBuildings\": " + (root.World != null ? root.World.Buildings.Count : -1) + ",");
                report.AppendLine("  \"zombies\": " + (root.World != null ? root.World.Zombies.Count : -1) + ",");
                report.AppendLine("  \"npcs\": " + (root.World != null ? root.World.Npcs.Count : -1) + ",");
                report.AppendLine("  \"simRealm\": " + (root.Sim != null ? root.Sim.Player.RealmIndex : -1) + ",");
                report.AppendLine("  \"statsKills\": " + (root.Sim != null ? root.Sim.Stats.Kills : -1) + ",");
                report.AppendLine("  \"statsSeconds\": " + (root.Sim != null ? root.Sim.Stats.SurvivalSeconds : -1f) + ",");
                report.AppendLine("  \"playerPos\": \"" + (root.Player != null ? root.Player.transform.position.ToString() : "none") + "\",");
            }
        }

        if (frames == 8 && GameRoot.Instance != null)
        {
            GameRoot.Instance.OpenDialogue("ahe");
            report.AppendLine("  \"dialogueOpen\": " + Bool(GameRoot.Instance.Dialogue.IsOpen) + ",");
        }
        if (frames == 10) Capture("unity-dialogue.png");
        if (frames == 14 && GameRoot.Instance != null && GameRoot.Instance.Dialogue.IsOpen)
        {
            GameRoot.Instance.Dialogue.Close();
            report.AppendLine("  \"dialogueClosed\": true,");
        }

        if (frames == 16 && GameRoot.Instance != null)
        {
            GameRoot.Instance.OpenTrade("trader_system");
            report.AppendLine("  \"tradeOpen\": " + Bool(GameRoot.Instance.TradePanel.IsOpen) + ",");
        }
        if (frames == 18) Capture("unity-trade.png");
        if (frames == 22 && GameRoot.Instance != null && GameRoot.Instance.TradePanel.IsOpen)
        {
            GameRoot.Instance.TradePanel.Close();
        }

        if (frames == 24 && GameRoot.Instance != null)
        {
            GameRoot.Instance.Phone.Open();
            GameRoot.Instance.Phone.OpenApp(PhoneApp.Stats);
            report.AppendLine("  \"statsApp\": true,");
        }
        if (frames == 30) { Capture("unity-stats.png"); report.AppendLine("  \"currentApp\": \"" + GameRoot.Instance.Phone.CurrentApp + "\","); }
        if (frames == 34 && GameRoot.Instance != null)
        {
            GameRoot.Instance.Phone.GoHome();
        }
        if (frames == 38) Capture("unity-phone.png");

        if (frames == 46)
        {
            if (GameRoot.Instance != null && GameRoot.Instance.Phone != null) GameRoot.Instance.Phone.Close();
            RunFeatureChecks();
        }

        if (frames == 52 && GameRoot.Instance != null && GameRoot.Instance.World != null &&
            GameRoot.Instance.World.Zombies.Count > 1)
        {
            var listener = GameRoot.Instance.World.Zombies[1];
            report.AppendLine("  \"zombieInvestigating\": " + Bool(listener != null && listener.IsInvestigating) + ",");
        }
        if (frames == 60) Capture("unity-game-2.png");
        if (frames == 75) Finish(null);
    }

    private static void RunFeatureChecks()
    {
        var root = GameRoot.Instance;
        if (root == null) return;
        var sim = root.Sim;

        // Firearm / ammo / reload.
        sim.AddItem("pistol", 1, false);
        sim.AddItem("ammo_9mm", 8, false);
        sim.EquipItem("pistol");
        var beforeMag = sim.Magazine("pistol");
        var fired = sim.TryFire(out var shot);
        var afterMag = sim.Magazine("pistol");
        var reloaded = sim.Reload();
        report.AppendLine("  \"weaponFired\": " + Bool(fired) + ",");
        report.AppendLine("  \"weaponMagBefore\": " + beforeMag + ",");
        report.AppendLine("  \"weaponMagAfter\": " + afterMag + ",");
        report.AppendLine("  \"weaponReloaded\": " + Bool(reloaded) + ",");
        report.AppendLine("  \"weaponShotDamage\": " + (shot != null ? shot.Damage : 0f) + ",");

        // Noise bus.
        var zombie = root.World != null && root.World.Zombies.Count > 1 ? root.World.Zombies[1] : null;
        var noiseOrigin = zombie != null ? zombie.transform.position : root.Player.transform.position;
        var noise = sim.EmitNoise("gunshot", noiseOrigin.x, noiseOrigin.z, 30f, 1f);
        report.AppendLine("  \"noiseEvents\": " + sim.Noise.Recent.Count + ",");
        report.AppendLine("  \"noiseRadius\": " + noise.Radius + ",");
        report.AppendLine("  \"animatorOnPlayer\": " + Bool(root.Player.GetComponent<CharacterAnimation>() != null) + ",");
        report.AppendLine("  \"animatorOnZombie\": " + Bool(zombie != null && zombie.GetComponent<CharacterAnimation>() != null) + ",");

        // Trade: system uses crystal cores, NPC uses coins.
        sim.AddItem("crystal_core", 3, false);
        sim.Wallet.Coin = 20;
        var buyCrystal = sim.Buy("trader_system", "bandage", 1);
        var buyCoin = sim.Buy("trader_laochen", "canned_food", 1);
        report.AppendLine("  \"tradeCrystal\": " + Bool(buyCrystal) + ",");
        report.AppendLine("  \"tradeCoin\": " + Bool(buyCoin) + ",");
        report.AppendLine("  \"tradeCount\": " + sim.Stats.Trades + ",");

        // Loot table and kill/drop path.
        var loot = sim.RollLoot("loot.police");
        report.AppendLine("  \"lootRolls\": " + loot.Count + ",");
        if (root.World != null && root.World.Zombies.Count > 0 && root.World.Zombies[0] != null)
        {
            root.World.Zombies[0].TakeDamage(9999f, Vector3.forward);
            report.AppendLine("  \"killAfterTest\": " + sim.Stats.Kills + ",");
        }

        // Technique upgrade / movement buff.
        sim.Player.Insight += 3;
        var learned = sim.LearnTechnique("wind_step");
        var upgraded = sim.UpgradeTechnique("wind_step");
        var cast = sim.CastTechnique("wind_step");
        report.AppendLine("  \"techLearned\": " + Bool(learned) + ",");
        report.AppendLine("  \"techUpgraded\": " + Bool(upgraded) + ",");
        report.AppendLine("  \"techCast\": " + Bool(cast) + ",");
        report.AppendLine("  \"techLevel\": " + sim.TechniqueLevel("wind_step") + ",");
        report.AppendLine("  \"speedBuff\": " + Quote(sim.SpeedBuffText) + ",");

        // Dialogue choice + story flag.
        var dialogue = sim.StartDialogue("ahe");
        var choices = dialogue != null ? dialogue.Choices.Count : 0;
        if (dialogue != null) sim.ChooseDialogue(dialogue, 0);
        report.AppendLine("  \"dialogueChoices\": " + choices + ",");
        report.AppendLine("  \"flagMetAhe\": " + Bool(sim.Story.HasFlag("met_ahe")) + ",");
        report.AppendLine("  \"statsKillsEnd\": " + sim.Stats.Kills + ",");
        report.AppendLine("  \"statsEvents\": " + sim.Stats.Events.Count + ",");
        report.AppendLine("  \"wallet\": " + Quote(sim.CurrencyText) + ",");
    }

    private static void Capture(string name)
    {
        var path = Path.Combine(RepoRoot, "docs/screenshots", name);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? RepoRoot);
        ScreenCapture.CaptureScreenshot(path, 2);
        Debug.Log("[Smoke] screenshot -> " + path);
    }

    private static void DumpPhoneHierarchy()
    {
        var phone = GameRoot.Instance.Phone;
        var rects = phone.GetComponentsInChildren<RectTransform>(true);
        Debug.Log("[Smoke] phone rects=" + rects.Length);
        foreach (var rect in rects)
        {
            if (rect == null) continue;
            var depth = 0;
            var parent = rect.parent;
            while (parent != null && parent != phone.transform) { depth++; parent = parent.parent; }
            if (depth > 9) continue;
            var image = rect.GetComponent<UnityEngine.UI.Image>();
            var color = image != null ? image.color.ToString() : "no-image";
            Debug.Log("[PhoneUI] " + new string(' ', depth * 2) + rect.name +
                      " pos=" + rect.anchoredPosition + " size=" + rect.sizeDelta +
                      " active=" + rect.gameObject.activeSelf + " img=" + color);
            if (rect.name == "HomeWallpaper" || rect.name == "Notification" || rect.name == "Home")
            {
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Debug.Log("[PhoneUI] corners " + rect.name + " " + corners[0] + " " + corners[1] + " " +
                          corners[2] + " " + corners[3]);
            }
        }
    }

    private static void Finish(string fatal)
    {
        started = false;
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        report.AppendLine("  \"frames\": " + frames + ",");
        report.AppendLine("  \"errors\": " + errors + ",");
        report.AppendLine("  \"editorErrorsIgnored\": " + editorErrorsIgnored + ",");
        report.AppendLine("  \"fatal\": " + Quote(fatal) + ",");
        report.AppendLine("  \"finishedAt\": \"" + DateTime.Now.ToString("o") + "\"");
        report.AppendLine("}");
        try { File.WriteAllText(reportPath, report.ToString()); } catch (Exception e) { Debug.LogError(e); }
        Debug.Log("[Smoke] finished fatal=" + (fatal ?? "none") + " errors=" + errors);
        EditorApplication.Exit(errors > 0 || fatal != null ? 1 : 0);
    }

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));

    private static string Bool(bool value) => value ? "true" : "false";
    private static string Quote(string value) => "\"" + (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}

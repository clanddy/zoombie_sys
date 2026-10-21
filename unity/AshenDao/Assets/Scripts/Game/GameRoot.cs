using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using AshenDao.Sim;
using AshenDao.UI;

namespace AshenDao.Game
{
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }
        public static bool SkipAutoLoad;

        public ContentDatabase Content { get; private set; }
        public WorldSim Sim { get; private set; }
        public WorldBuilder World { get; private set; }
        public PlayerController Player { get; private set; }
        public TopDownCamera CameraRig { get; private set; }
        public VisionController Vision { get; private set; }
        public PhoneUI Phone { get; private set; }
        public HudUI Hud { get; private set; }
        public DialogueUI Dialogue { get; private set; }
        public TradeUI TradePanel { get; private set; }
        public AudioDirector Audio { get; private set; }

        public bool InputBlocked => Sim != null && Sim.Player.Dead ||
                                    Phone != null && Phone.IsOpen ||
                                    Dialogue != null && Dialogue.IsOpen ||
                                    TradePanel != null && TradePanel.IsOpen;

        private DayNightController dayNight;
        private Light sun;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;

            Content = ContentDatabase.CreateDemo();
            Sim = new WorldSim(Content);

            SetupCamera();
            SetupSun();
            SetupEventSystem();

            var playerGo = new GameObject("Player");
            Player = playerGo.AddComponent<PlayerController>();
            playerGo.transform.position = new Vector3(10f, 0.2f, 8f);
            Player.Setup(this);

            World = new WorldBuilder();
            World.PlayerSpawn = new Vector3(10f, 0.2f, 8f);
            World.Build(Player);

            Vision = Player.gameObject.AddComponent<VisionController>();
            Vision.Setup(Player);

            CameraRig.Setup(Player.transform);
            CameraRig.Snap();

            dayNight = gameObject.AddComponent<DayNightController>();
            dayNight.Setup(Sim, sun, Vision, World);

            Phone = new GameObject("PhoneUI").AddComponent<PhoneUI>();
            Phone.Setup(this);
            Hud = new GameObject("HudUI").AddComponent<HudUI>();
            Hud.Setup(this);
            Dialogue = new GameObject("DialogueUI").AddComponent<DialogueUI>();
            Dialogue.Setup(this);
            TradePanel = new GameObject("TradeUI").AddComponent<TradeUI>();
            TradePanel.Setup(this);
            Audio = gameObject.AddComponent<AudioDirector>();
            Audio.Setup(Sim);

            SaveSystem.TryAutoLoad(this);
            Sim.Notify("TAB 手机 · E 交谈/拾取 · F 换弹 · R 入定 · Q 术法", NoticeTone.Info);
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            CameraRig = cam.GetComponent<TopDownCamera>();
            if (CameraRig == null) CameraRig = cam.gameObject.AddComponent<TopDownCamera>();
            cam.transform.rotation = Quaternion.Euler(53f, 0f, 0f);
        }

        private void SetupSun()
        {
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    sun = light;
                    break;
                }
            }
            if (sun == null)
            {
                var go = new GameObject("Sun");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.shadows = LightShadows.Soft;
        }

        private void SetupEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private void Update()
        {
            if (Sim == null) return;

            Sim.Update(Time.deltaTime);

            if (Phone != null && Input.GetKeyDown(KeyCode.Tab)) Phone.Toggle();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Dialogue != null && Dialogue.IsOpen) Dialogue.Close();
                else if (TradePanel != null && TradePanel.IsOpen) TradePanel.Close();
                else if (Phone != null && Phone.IsOpen) Phone.Close();
            }

            UpdateStoryZones();

            if (Input.GetKeyDown(KeyCode.F5)) Save();
            if (Input.GetKeyDown(KeyCode.F8))
            {
                SaveSystem.Delete();
                SkipAutoLoad = true;
                Restart();
            }
            if (Input.GetKeyDown(KeyCode.F9)) Load();
            if (Sim.Player.Dead && Input.GetKeyDown(KeyCode.R)) Restart();

            if (World != null && World.Vein != null)
            {
                if (World.Vein.Contains(Player.transform.position))
                {
                    Sim.VeinBonus = World.Vein.Strength;
                    Sim.VeinName = World.Vein.VeinName;
                }
                else
                {
                    Sim.VeinBonus = 1f;
                    Sim.VeinName = "";
                }
            }
        }

        public bool CastTechnique(string id)
        {
            return Sim != null && Player != null && Player.CastTechnique(Sim, id);
        }

        public bool CastFirstActive()
        {
            foreach (var tech in Content.Techniques)
            {
                if (tech.Kind != "active") continue;
                if (!Sim.Player.Techniques.Contains(tech.Id)) continue;
                return Player.CastTechnique(Sim, tech.Id);
            }
            return false;
        }

        public void OnZombieKilled(ZombieAI zombie)
        {
            if (Sim == null) return;
            Sim.OnZombieKilled(zombie != null ? zombie.Def : null);
            if (zombie != null && Sim.Stats.SurvivalSeconds < float.MaxValue)
            {
                var loot = Sim.RollLoot(zombie.Def != null ? zombie.Def.LootTable : "loot.walker");
                SpawnLoot(loot, zombie.transform.position);
            }
        }

        public void SpawnLoot(List<LootResult> loot, Vector3 position)
        {
            if (loot == null) return;
            for (var i = 0; i < loot.Count; i++)
            {
                var offset = new Vector3((i % 3 - 1) * 0.42f, 0f, (i / 3) * 0.42f);
                ItemPickup.Create(World != null && World.Root != null ? World.Root : transform, position + offset,
                    loot[i], Content);
            }
        }

        public void OpenDialogue(string npcId)
        {
            if (Dialogue == null)
            {
                Dialogue = new GameObject("DialogueUI").AddComponent<DialogueUI>();
                Dialogue.Setup(this);
            }
            Dialogue.Open(npcId);
        }

        public void OpenTrade(string traderId)
        {
            if (TradePanel == null)
            {
                TradePanel = new GameObject("TradeUI").AddComponent<TradeUI>();
                TradePanel.Setup(this);
            }
            TradePanel.Open(traderId);
        }

        private void UpdateStoryZones()
        {
            if (Sim == null || Player == null) return;
            var pos = Player.transform.position;
            if (!Sim.Story.HasFlag("clinic") && (pos - new Vector3(12f, 0f, -12f)).sqrMagnitude < 9f)
            {
                Sim.SetFlag("clinic");
            }
            if (Sim.Story.HasFlag("clinic") && !Sim.Story.HasFlag("safehouse") &&
                (pos - new Vector3(10f, 0f, 8f)).sqrMagnitude < 12f)
            {
                Sim.SetFlag("safehouse");
            }
        }

        public void OnPlayerDead()
        {
            Sim.Stats.Deaths++;
            Sim.RecordEvent(GameEventType.Damage, "player.death", "你倒下了。", 1);
            if (Phone != null) Phone.Close();
            if (Dialogue != null) Dialogue.Close();
            if (TradePanel != null) TradePanel.Close();
            if (Hud != null) Hud.ShowDeath();
        }

        public void Save()
        {
            SaveSystem.Save(this);
            Sim.Notify("进度已保存。", NoticeTone.Info);
        }

        public void Load()
        {
            if (SaveSystem.Load(this)) Sim.Notify("已读取存档。", NoticeTone.Info);
            else Sim.Notify("没有找到可用存档。", NoticeTone.Warn);
        }

        public void Restart()
        {
            SkipAutoLoad = true;
            Time.timeScale = 1f;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}

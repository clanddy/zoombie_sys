using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AshenDao.Game;
using AshenDao.Sim;

namespace AshenDao.UI
{
    public enum PhoneApp
    {
        Home,
        Quests,
        Cultivation,
        Inventory,
        Map,
        Stats,
        Trade,
        Messages,
        Settings
    }

    public partial class PhoneUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public PhoneApp CurrentApp => current;

        private GameRoot root;
        private WorldSim sim;
        private Canvas canvas;
        private CanvasGroup group;
        private RectTransform phoneRoot;
        private RectTransform contentRoot;
        private GameObject dim;
        private Image dimImage;
        private Text statusTime;
        private Text statusDate;
        private Text batteryText;
        private Image batteryFill;
        private Text homeTime;
        private Text homeDate;
        private Text homeLocation;
        private RectTransform homeRoot;
        private RectTransform appRoot;
        private PhoneApp current = PhoneApp.Home;
        private Coroutine animRoutine;
        private float battery = 86f;
        private int unread = 1;

        public void Setup(GameRoot owner)
        {
            root = owner;
            sim = owner.Sim;
            canvas = UiFactory.CreateCanvas("PhoneCanvas", 100);
            canvas.transform.SetParent(transform, false);

            var dimRt = UiFactory.FullScreen(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
            dim = dimRt.gameObject;
            dimImage = dimRt.GetComponent<Image>();
            dimImage.raycastTarget = true;
            var dimButton = dim.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.targetGraphic = dimImage;
            dimButton.onClick.AddListener(Close);

            phoneRoot = UiFactory.CenterPanel(canvas.transform, "Phone", new Vector2(390f, 844f),
                Color.clear, 0f);
            group = phoneRoot.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            var shadow = UiFactory.CenterPanel(phoneRoot, "Shadow", new Vector2(374f, 830f),
                new Color(0f, 0f, 0f, 0.45f), 50f);
            shadow.anchoredPosition = new Vector2(0f, -16f);

            var frame = UiFactory.CenterPanel(phoneRoot, "Frame", new Vector2(390f, 844f),
                PhoneTheme.Frame, 54f);
            frame.GetComponent<Image>().raycastTarget = true;

            var screen = UiFactory.Panel(frame, "Screen", new Vector2(370f, 824f), new Vector2(10f, 10f),
                PhoneTheme.Screen, 45f);
            screen.GetComponent<Image>().raycastTarget = true;
            BuildWallpaper(screen);
            BuildStatusBar(screen);
            BuildIsland(screen);

            contentRoot = UiFactory.Panel(screen, "Content", new Vector2(350f, 744f),
                new Vector2(10f, 56f), Color.clear, 0f);
            contentRoot.GetComponent<Image>().raycastTarget = false;
            homeRoot = UiFactory.NewRect(contentRoot, "Home");
            UiFactory.Place(homeRoot, 0f, 0f, 350f, 744f);
            appRoot = UiFactory.NewRect(contentRoot, "App");
            UiFactory.Place(appRoot, 0f, 0f, 350f, 744f);
            appRoot.gameObject.SetActive(false);

            BuildHomeIndicator(screen);
            BuildSideButtons(phoneRoot);
            BuildHome();

            canvas.gameObject.SetActive(false);
        }

        private void BuildWallpaper(RectTransform screen)
        {
            var wallpaper = UiFactory.NewRect(screen, "Wallpaper");
            wallpaper.anchorMin = Vector2.zero;
            wallpaper.anchorMax = Vector2.one;
            wallpaper.offsetMin = new Vector2(2f, 2f);
            wallpaper.offsetMax = new Vector2(-2f, -2f);
            var image = wallpaper.gameObject.AddComponent<Image>();
            image.sprite = UiFactory.GradientSprite(new Color(0.07f, 0.15f, 0.13f), new Color(0.02f, 0.04f, 0.045f));
            image.type = Image.Type.Simple;
            image.raycastTarget = false;

            var moonImage = UiFactory.CenterPanel(wallpaper, "Moon", new Vector2(150f, 150f),
                new Color(0.85f, 0.92f, 0.86f, 0.12f), 0f).GetComponent<Image>();
            moonImage.sprite = UiFactory.RadialSprite();
            moonImage.type = Image.Type.Simple;
            moonImage.rectTransform.anchoredPosition = new Vector2(105f, 210f);

            var glowImage = UiFactory.CenterPanel(wallpaper, "Glow", new Vector2(340f, 340f),
                new Color(0.2f, 0.7f, 0.55f, 0.10f), 0f).GetComponent<Image>();
            glowImage.sprite = UiFactory.RadialSprite();
            glowImage.type = Image.Type.Simple;
            glowImage.rectTransform.anchoredPosition = new Vector2(-60f, -180f);
        }

        private void BuildStatusBar(RectTransform screen)
        {
            statusTime = UiFactory.Label(screen, "StatusTime", "08:00", 14, PhoneTheme.Text, TextAnchor.MiddleLeft, false);
            UiFactory.Place(statusTime, 24f, 14f, 60f, 22f);
            statusDate = UiFactory.Label(screen, "StatusDate", "第1天", 10, PhoneTheme.Muted, TextAnchor.MiddleLeft, false);
            UiFactory.Place(statusDate, 88f, 16f, 56f, 18f);

            for (var i = 0; i < 4; i++)
            {
                var bar = UiFactory.Panel(screen, "Signal" + i, new Vector2(3f, 4f + i * 2f),
                    new Vector2(248f + i * 5f, 26f - i * 2f), PhoneTheme.Text, 1f);
                bar.GetComponent<Image>().raycastTarget = false;
            }

            var network = UiFactory.Label(screen, "Network", "5G", 9, PhoneTheme.Muted, TextAnchor.MiddleLeft, false);
            UiFactory.Place(network, 270f, 16f, 22f, 16f);

            var batteryBack = UiFactory.Panel(screen, "Battery", new Vector2(24f, 11f), new Vector2(324f, 18f),
                Color.clear, 0f);
            var border = UiFactory.Panel(batteryBack, "Border", new Vector2(24f, 11f), Vector2.zero,
                new Color(1f, 1f, 1f, 0.25f), 3f);
            border.GetComponent<Image>().raycastTarget = false;
            var inner = UiFactory.Panel(batteryBack, "Inner", new Vector2(20f, 7f), new Vector2(2f, 2f),
                new Color(0f, 0f, 0f, 0.3f), 2f);
            inner.GetComponent<Image>().raycastTarget = false;
            var fill = UiFactory.Panel(inner, "Fill", new Vector2(17f, 7f), Vector2.zero, PhoneTheme.Jade, 2f);
            batteryFill = fill.GetComponent<Image>();
            batteryFill.type = Image.Type.Filled;
            batteryFill.fillMethod = Image.FillMethod.Horizontal;
            batteryFill.fillAmount = 0.86f;
            batteryText = UiFactory.Label(screen, "BatteryText", "86", 9, PhoneTheme.Muted, TextAnchor.MiddleRight, false);
            UiFactory.Place(batteryText, 292f, 16f, 28f, 16f);
        }

        private void BuildIsland(RectTransform screen)
        {
            var island = UiFactory.CenterPanel(screen, "Island", new Vector2(118f, 28f), Color.black, 14f);
            island.anchoredPosition = new Vector2(0f, 388f);
            var cameraDot = UiFactory.CenterPanel(island, "Camera", new Vector2(7f, 7f),
                new Color(0.18f, 0.23f, 0.25f), 4f);
            cameraDot.anchoredPosition = new Vector2(38f, 0f);
        }

        private void BuildHomeIndicator(RectTransform screen)
        {
            var indicator = UiFactory.CenterPanel(screen, "HomeIndicator", new Vector2(92f, 5f),
                new Color(1f, 1f, 1f, 0.45f), 3f);
            indicator.anchoredPosition = new Vector2(0f, -404f);
        }

        private void BuildSideButtons(RectTransform phone)
        {
            UiFactory.Panel(phone, "PowerButton", new Vector2(3f, 72f), new Vector2(390f, 190f),
                new Color(0.18f, 0.2f, 0.19f), 2f);
            UiFactory.Panel(phone, "VolumeUp", new Vector2(3f, 48f), new Vector2(390f, 280f),
                new Color(0.18f, 0.2f, 0.19f), 2f);
            UiFactory.Panel(phone, "VolumeDown", new Vector2(3f, 48f), new Vector2(390f, 338f),
                new Color(0.18f, 0.2f, 0.19f), 2f);
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (IsOpen) return;
            if (sim != null && sim.Player.Dead) return;
            IsOpen = true;
            canvas.gameObject.SetActive(true);
            dim.SetActive(true);
            group.alpha = 0f;
            phoneRoot.anchoredPosition = new Vector2(0f, -34f);
            phoneRoot.localScale = Vector3.one * 0.965f;
            current = PhoneApp.Home;
            unread = 0;
            appRoot.gameObject.SetActive(false);
            homeRoot.gameObject.SetActive(true);
            BuildHome();
            RefreshStatus();
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(AnimateOpen(true));
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (animRoutine != null) StopCoroutine(animRoutine);
            animRoutine = StartCoroutine(AnimateOpen(false));
        }

        private IEnumerator AnimateOpen(bool opening)
        {
            var duration = 0.22f;
            var time = 0f;
            var startAlpha = group.alpha;
            var startY = phoneRoot.anchoredPosition.y;
            var startScale = phoneRoot.localScale.x;
            var targetY = opening ? 0f : -34f;
            var targetScale = opening ? 1f : 0.965f;
            var dimTarget = opening ? 0.55f : 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(time / duration);
                var ease = 1f - Mathf.Pow(1f - t, 3f);
                group.alpha = Mathf.Lerp(startAlpha, opening ? 1f : 0f, ease);
                phoneRoot.anchoredPosition = new Vector2(0f, Mathf.Lerp(startY, targetY, ease));
                phoneRoot.localScale = Vector3.one * Mathf.Lerp(startScale, targetScale, ease);
                var color = dimImage.color;
                color.a = Mathf.Lerp(color.a, dimTarget, ease);
                dimImage.color = color;
                yield return null;
            }

            if (!opening)
            {
                canvas.gameObject.SetActive(false);
            }
            animRoutine = null;
        }

        public void RefreshAll()
        {
            if (canvas == null) return;
            RefreshStatus();
            if (current == PhoneApp.Home) BuildHome();
            else BuildApp(current);
        }

        private void Update()
        {
            if (!IsOpen) return;
            battery = Mathf.Max(35f, 86f - sim.Clock.TotalMinutes / 120f);
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (statusTime == null) return;
            statusTime.text = sim.Clock.ClockText;
            statusDate.text = "第" + sim.Clock.Day + "天";
            batteryText.text = Mathf.RoundToInt(battery) + "%";
            batteryFill.fillAmount = battery / 100f;
            batteryFill.color = battery < 20f ? PhoneTheme.Danger : PhoneTheme.Jade;

            if (current == PhoneApp.Home && homeTime != null)
            {
                homeTime.text = sim.Clock.ClockText;
                homeDate.text = "第 " + sim.Clock.Day + " 天 · " + PhaseName();
            }
        }

        public void GoHome()
        {
            current = PhoneApp.Home;
            appRoot.gameObject.SetActive(false);
            homeRoot.gameObject.SetActive(true);
            BuildHome();
        }

        public void OpenApp(PhoneApp app)
        {
            if (app == PhoneApp.Home)
            {
                GoHome();
                return;
            }
            current = app;
            homeRoot.gameObject.SetActive(false);
            appRoot.gameObject.SetActive(true);
            BuildApp(app);
        }

        private void BuildHome()
        {
            UiFactory.Clear(homeRoot);
            var wallpaper = UiFactory.NewRect(homeRoot, "HomeWallpaper");
            wallpaper.anchorMin = Vector2.zero;
            wallpaper.anchorMax = Vector2.one;
            wallpaper.offsetMin = Vector2.zero;
            wallpaper.offsetMax = Vector2.zero;
            var bg = wallpaper.gameObject.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.07f, 0.065f, 0.55f);
            bg.raycastTarget = false;

            homeTime = UiFactory.Label(homeRoot, "HomeTime", sim.Clock.ClockText, 58, PhoneTheme.Text,
                TextAnchor.UpperLeft, false);
            UiFactory.Place(homeTime, 10f, 18f, 250f, 74f);
            homeDate = UiFactory.Label(homeRoot, "HomeDate", "第 " + sim.Clock.Day + " 天 · " + PhaseName(),
                13, PhoneTheme.Jade, TextAnchor.UpperLeft, false);
            UiFactory.Place(homeDate, 12f, 94f, 280f, 20f);
            homeLocation = UiFactory.Label(homeRoot, "HomeLocation", "废土 · 安全屋", 11, PhoneTheme.Muted,
                TextAnchor.UpperLeft, false);
            UiFactory.Place(homeLocation, 12f, 114f, 300f, 18f);

            if (sim.Notices.Count > 0)
            {
                var latest = sim.Notices[sim.Notices.Count - 1];
                var notice = UiFactory.Panel(homeRoot, "Notification", new Vector2(314f, 54f),
                    new Vector2(18f, 146f), new Color(0.07f, 0.12f, 0.1f, 0.9f), 14f);
                var nTop = UiFactory.Label(notice, "NTop", "天机碑 · 第 " + latest.Day + " 天 " + latest.Clock,
                    8, PhoneTheme.Muted, TextAnchor.UpperLeft, false);
                UiFactory.Place(nTop, 12f, 8f, 220f, 14f);
                var nText = UiFactory.Label(notice, "NText", latest.Text, 10, PhoneTheme.Text,
                    TextAnchor.UpperLeft, true);
                UiFactory.Place(nText, 12f, 24f, 288f, 26f);
            }

            var apps = new[]
            {
                new AppEntry("任务", "卷", PhoneTheme.Jade, PhoneApp.Quests),
                new AppEntry("修行", "气", PhoneTheme.Qi, PhoneApp.Cultivation),
                new AppEntry("背包", "囊", PhoneTheme.Gold, PhoneApp.Inventory),
                new AppEntry("地图", "图", new Color(0.45f, 0.62f, 0.85f), PhoneApp.Map),
                new AppEntry("行迹", "迹", new Color(0.65f, 0.72f, 0.55f), PhoneApp.Stats),
                new AppEntry("交易", "易", new Color(0.85f, 0.65f, 0.35f), PhoneApp.Trade),
                new AppEntry("消息", "信", new Color(0.72f, 0.55f, 0.85f), PhoneApp.Messages),
                new AppEntry("设置", "设", new Color(0.5f, 0.55f, 0.53f), PhoneApp.Settings)
            };

            for (var i = 0; i < apps.Length; i++)
            {
                var col = i % 4;
                var row = i / 4;
                var entry = apps[i];
                var x = 18f + col * 82f;
                var y = 216f + row * 102f;
                var icon = UiFactory.Panel(homeRoot, "App_" + entry.Label, new Vector2(58f, 58f),
                    new Vector2(x, y), entry.Color, 15f);
                var iconImage = icon.GetComponent<Image>();
                iconImage.raycastTarget = true;
                var button = icon.gameObject.AddComponent<Button>();
                button.targetGraphic = iconImage;
                var target = entry.App;
                var disabled = entry.Disabled;
                button.onClick.AddListener(() =>
                {
                    if (!disabled) OpenApp(target);
                });
                var glyph = UiFactory.Label(icon, "Glyph", entry.Glyph, 25, new Color(0.03f, 0.06f, 0.055f),
                    TextAnchor.MiddleCenter, false);
                glyph.rectTransform.anchorMin = Vector2.zero;
                glyph.rectTransform.anchorMax = Vector2.one;
                glyph.rectTransform.offsetMin = Vector2.zero;
                glyph.rectTransform.offsetMax = Vector2.zero;

                if (entry.App == PhoneApp.Messages && unread > 0)
                {
                    var badge = UiFactory.CenterPanel(icon, "Badge", new Vector2(20f, 20f), PhoneTheme.Danger, 10f);
                    badge.anchoredPosition = new Vector2(24f, 24f);
                    var badgeText = UiFactory.Label(badge, "Count", unread.ToString(), 10, Color.white,
                        TextAnchor.MiddleCenter, false);
                    badgeText.rectTransform.anchorMin = Vector2.zero;
                    badgeText.rectTransform.anchorMax = Vector2.one;
                    badgeText.rectTransform.offsetMin = Vector2.zero;
                    badgeText.rectTransform.offsetMax = Vector2.zero;
                }

                var label = UiFactory.Label(homeRoot, "AppLabel_" + i, entry.Label, 10, PhoneTheme.Text,
                    TextAnchor.MiddleCenter, false);
                UiFactory.Place(label, x - 6f, y + 64f, 70f, 16f);
            }

            var divider = UiFactory.Panel(homeRoot, "DockDivider", new Vector2(310f, 1f), new Vector2(20f, 556f),
                new Color(1f, 1f, 1f, 0.12f), 0f);
            divider.GetComponent<Image>().raycastTarget = false;

            var dockApps = new[]
            {
                new AppEntry("任务", "卷", PhoneTheme.Jade, PhoneApp.Quests),
                new AppEntry("修行", "气", PhoneTheme.Qi, PhoneApp.Cultivation),
                new AppEntry("背包", "囊", PhoneTheme.Gold, PhoneApp.Inventory),
                new AppEntry("地图", "图", new Color(0.45f, 0.62f, 0.85f), PhoneApp.Map)
            };
            for (var i = 0; i < dockApps.Length; i++)
            {
                var entry = dockApps[i];
                var x = 30f + i * 80f;
                var icon = UiFactory.Panel(homeRoot, "Dock_" + entry.Label, new Vector2(52f, 52f),
                    new Vector2(x, 590f), new Color(1f, 1f, 1f, 0.08f), 14f);
                var image = icon.GetComponent<Image>();
                image.raycastTarget = true;
                var button = icon.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                var target = entry.App;
                button.onClick.AddListener(() => OpenApp(target));
                var glyph = UiFactory.Label(icon, "Glyph", entry.Glyph, 22, entry.Color, TextAnchor.MiddleCenter,
                    false);
                glyph.rectTransform.anchorMin = Vector2.zero;
                glyph.rectTransform.anchorMax = Vector2.one;
                glyph.rectTransform.offsetMin = Vector2.zero;
                glyph.rectTransform.offsetMax = Vector2.zero;
            }

            for (var i = 0; i < 2; i++)
            {
                var dot = UiFactory.CenterPanel(homeRoot, "PageDot", new Vector2(6f, 6f),
                    i == 0 ? PhoneTheme.Text : new Color(1f, 1f, 1f, 0.2f), 3f);
                dot.anchorMin = new Vector2(0.5f, 0f);
                dot.anchorMax = new Vector2(0.5f, 0f);
                dot.anchoredPosition = new Vector2(i == 0 ? -10f : 10f, 24f);
            }

            var hint = UiFactory.Label(homeRoot, "HomeHint", "Tab 收起手机", 9, new Color(1f, 1f, 1f, 0.3f),
                TextAnchor.MiddleCenter, false);
            UiFactory.Place(hint, 100f, 706f, 150f, 16f);
        }

        private string PhaseName()
        {
            switch (sim.Clock.Phase)
            {
                case "dawn": return "拂晓";
                case "day": return "白昼";
                case "dusk": return "黄昏";
                default: return "长夜";
            }
        }

        private struct AppEntry
        {
            public readonly string Label;
            public readonly string Glyph;
            public readonly Color Color;
            public readonly PhoneApp App;
            public readonly bool Disabled;

            public AppEntry(string label, string glyph, Color color, PhoneApp app, bool disabled = false)
            {
                Label = label;
                Glyph = glyph;
                Color = color;
                App = app;
                Disabled = disabled;
            }
        }
    }
}

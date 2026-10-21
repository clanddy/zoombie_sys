using UnityEngine;
using UnityEngine.UI;
using AshenDao.Game;
using AshenDao.Sim;

namespace AshenDao.UI
{
    public class HudUI : MonoBehaviour
    {
        private GameRoot root;
        private WorldSim sim;
        private Canvas canvas;
        private Text clockText;
        private Text realmText;
        private Image hpFill;
        private Image staminaFill;
        private Image qiFill;
        private Text hpText;
        private Text staminaText;
        private Text qiText;
        private Text ammoText;
        private Text speedText;
        private Text promptText;
        private Transform promptRoot;
        private Text questText;
        private RectTransform noticeRoot;
        private CanvasGroup noticeGroup;
        private Text noticeText;
        private float noticeTimer;
        private float lastHp;
        private Image damageFlash;
        private float flashAlpha;
        private GameObject deathRoot;
        private Text deathStats;
        private int lastNoticeId;

        public void Setup(GameRoot owner)
        {
            root = owner;
            sim = owner.Sim;
            canvas = UiFactory.CreateCanvas("HudCanvas", 50);
            canvas.transform.SetParent(transform, false);

            var panel = UiFactory.Panel(canvas.transform, "Status", new Vector2(310f, 136f),
                new Vector2(18f, 18f), new Color(0.03f, 0.07f, 0.065f, 0.82f), 10f);
            var title = UiFactory.Label(panel, "Title", "劫余问道", 11, PhoneTheme.Jade, TextAnchor.UpperLeft, false);
            UiFactory.Place(title, 14f, 10f, 180f, 18f);
            clockText = UiFactory.Label(panel, "Clock", "第 1 天 · 08:00", 16, PhoneTheme.Text,
                TextAnchor.UpperLeft, false);
            UiFactory.Place(clockText, 14f, 28f, 200f, 24f);
            realmText = UiFactory.Label(panel, "Realm", "炼气期 · 1 层", 10, PhoneTheme.Gold,
                TextAnchor.UpperRight, false);
            UiFactory.Place(realmText, 190f, 14f, 106f, 18f);

            hpFill = UiFactory.Bar(panel, "Hp", 14f, 66f, 282f, 6f, PhoneTheme.Danger, 1f);
            staminaFill = UiFactory.Bar(panel, "Stamina", 14f, 84f, 282f, 5f, PhoneTheme.Gold, 1f);
            qiFill = UiFactory.Bar(panel, "Qi", 14f, 100f, 282f, 5f, PhoneTheme.Qi, 0.5f);
            hpText = UiFactory.Label(panel, "HpText", "", 9, PhoneTheme.Muted, TextAnchor.UpperLeft, false);
            UiFactory.Place(hpText, 14f, 57f, 120f, 12f);
            staminaText = UiFactory.Label(panel, "StaminaText", "", 9, PhoneTheme.Muted, TextAnchor.UpperLeft, false);
            UiFactory.Place(staminaText, 14f, 76f, 120f, 12f);
            qiText = UiFactory.Label(panel, "QiText", "", 9, PhoneTheme.Muted, TextAnchor.UpperLeft, false);
            UiFactory.Place(qiText, 14f, 92f, 160f, 12f);
            ammoText = UiFactory.Label(panel, "AmmoText", "", 9, PhoneTheme.Gold, TextAnchor.UpperRight, false);
            UiFactory.Place(ammoText, 176f, 92f, 120f, 12f);
            speedText = UiFactory.Label(panel, "SpeedText", "", 9, PhoneTheme.Qi, TextAnchor.UpperLeft, false);
            UiFactory.Place(speedText, 14f, 108f, 282f, 16f);

            questText = UiFactory.Label(canvas.transform, "QuestTrack", "TAB 手机查看任务", 10,
                PhoneTheme.Muted, TextAnchor.UpperRight, true);
            UiFactory.Place(questText, 1120f, 22f, 450f, 40f);
            questText.alignment = TextAnchor.UpperRight;

            var hint = UiFactory.Label(canvas.transform, "Hint", "TAB 手机   E 交互   F 换弹   R 入定   Q/1 术法   2 御风   SHIFT 疾行",
                10, PhoneTheme.Muted, TextAnchor.LowerLeft, false);
            UiFactory.Place(hint, 20f, 842f, 600f, 40f);

            var prompt = UiFactory.CenterPanel(canvas.transform, "Prompt", new Vector2(420f, 44f),
                new Color(0.03f, 0.07f, 0.06f, 0.9f), 10f);
            prompt.anchoredPosition = new Vector2(0f, -300f);
            promptRoot = prompt;
            promptText = UiFactory.Label(prompt, "PromptText", "", 13, PhoneTheme.Gold,
                TextAnchor.MiddleCenter, false);
            promptText.rectTransform.anchorMin = Vector2.zero;
            promptText.rectTransform.anchorMax = Vector2.one;
            promptText.rectTransform.offsetMin = new Vector2(10f, 0f);
            promptText.rectTransform.offsetMax = new Vector2(-10f, 0f);
            prompt.gameObject.SetActive(false);

            var notice = UiFactory.CenterPanel(canvas.transform, "Notice", new Vector2(560f, 38f),
                new Color(0.03f, 0.07f, 0.06f, 0.88f), 8f);
            notice.anchoredPosition = new Vector2(0f, 350f);
            noticeRoot = notice;
            noticeGroup = notice.gameObject.AddComponent<CanvasGroup>();
            noticeGroup.alpha = 0f;
            noticeText = UiFactory.Label(notice, "NoticeText", "", 12, PhoneTheme.Text,
                TextAnchor.MiddleCenter, false);
            noticeText.rectTransform.anchorMin = Vector2.zero;
            noticeText.rectTransform.anchorMax = Vector2.one;
            noticeText.rectTransform.offsetMin = Vector2.zero;
            noticeText.rectTransform.offsetMax = Vector2.zero;

            var flash = UiFactory.FullScreen(canvas.transform, "DamageFlash", new Color(0.6f, 0.04f, 0.03f, 0f));
            damageFlash = flash.GetComponent<Image>();
            damageFlash.raycastTarget = false;

            BuildDeathScreen();
            lastHp = sim.Player.Hp;
            Refresh();
        }

        private void BuildDeathScreen()
        {
            var death = UiFactory.FullScreen(canvas.transform, "Death", new Color(0.02f, 0.01f, 0.01f, 0.82f));
            deathRoot = death.gameObject;
            var title = UiFactory.Label(death, "Title", "道消身殒", 64, new Color(0.85f, 0.78f, 0.7f),
                TextAnchor.MiddleCenter, false);
            title.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            title.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            title.rectTransform.offsetMin = new Vector2(0f, 40f);
            title.rectTransform.offsetMax = new Vector2(0f, 160f);
            deathStats = UiFactory.Label(death, "Stats", "", 14, PhoneTheme.Muted, TextAnchor.MiddleCenter, true);
            deathStats.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            deathStats.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            deathStats.rectTransform.offsetMin = new Vector2(0f, -120f);
            deathStats.rectTransform.offsetMax = new Vector2(0f, 20f);
            var hint = UiFactory.Label(death, "Hint", "按 R 重新开始 · 按 F9 读取最近存档", 16, PhoneTheme.Gold,
                TextAnchor.MiddleCenter, false);
            hint.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            hint.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            hint.rectTransform.offsetMin = new Vector2(0f, -220f);
            hint.rectTransform.offsetMax = new Vector2(0f, -170f);
            deathRoot.SetActive(false);
        }

        private void Update()
        {
            if (sim == null) return;
            Refresh();
            FadeDamage();
            FadeNotice();

            if (root.Phone != null && root.Phone.IsOpen) return;
        }

        public void Refresh()
        {
            if (clockText == null) return;
            clockText.text = "第 " + sim.Clock.Day + " 天 · " + sim.Clock.ClockText + " · " + PhaseName();
            realmText.text = sim.Realm.Name + " · " + sim.Player.Stage + " 层";
            hpFill.fillAmount = Mathf.Clamp01(sim.Player.Hp / sim.Player.MaxHp);
            staminaFill.fillAmount = Mathf.Clamp01(sim.Player.Stamina / sim.Player.MaxStamina);
            qiFill.fillAmount = Mathf.Clamp01(sim.Player.Qi / Mathf.Max(1f, sim.QiMax));
            hpText.text = "气血 " + Mathf.CeilToInt(sim.Player.Hp) + " / " + Mathf.CeilToInt(sim.Player.MaxHp);
            staminaText.text = "体力 " + Mathf.CeilToInt(sim.Player.Stamina);
            qiText.text = "灵气 " + Mathf.FloorToInt(sim.Player.Qi) + " / " + Mathf.CeilToInt(sim.QiMax) +
                          (sim.Meditating ? "  入定中" : "");
            ammoText.text = sim.AmmoText;
            speedText.text = string.IsNullOrEmpty(sim.SpeedBuffText)
                ? "悟性 " + sim.Player.Insight + " · 护甲 " + Mathf.RoundToInt(sim.Armor)
                : "身法 · " + sim.SpeedBuffText;

            var active = sim.Quests.QuestsWithStatus(QuestStatus.Active);
            if (active.Count == 0)
            {
                questText.text = "没有进行中的任务";
            }
            else
            {
                var quest = active[0];
                questText.text = "任务 · " + quest.Title + "\n" + quest.Summary;
            }

            var prompt = root.Player != null ? root.Player.CurrentPrompt : "";
            if (promptRoot != null)
            {
                var show = !string.IsNullOrEmpty(prompt) && !sim.Player.Dead;
                if (promptRoot.gameObject.activeSelf != show) promptRoot.gameObject.SetActive(show);
                if (show) promptText.text = "E   " + prompt;
            }

            if (sim.Player.Dead && deathRoot != null && !deathRoot.activeSelf) ShowDeath();
            if (!sim.Player.Dead && deathRoot != null && deathRoot.activeSelf) deathRoot.SetActive(false);
            if (deathRoot != null && deathRoot.activeSelf)
            {
                deathStats.text = "境界 " + sim.Realm.Name + " · 第 " + sim.Player.Stage + " 层\n" +
                                  "存活 " + sim.Stats.SurvivalText + " · 击杀 " + sim.Stats.Kills +
                                  " · 任务 " + sim.Stats.QuestsCompleted + "\n" +
                                  "搜刮 " + sim.Stats.ItemsLooted + " · 旧币 " + sim.Wallet.Coin +
                                  " · 晶核 " + sim.CountItem("crystal_core");
            }

            foreach (var notice in sim.Notices)
            {
                if (notice.Id <= lastNoticeId) continue;
                lastNoticeId = notice.Id;
                ShowNotice(notice);
            }

            if (sim.Player.Hp < lastHp - 0.5f)
            {
                flashAlpha = Mathf.Max(flashAlpha, 0.22f);
            }
            lastHp = sim.Player.Hp;
        }

        private void ShowNotice(Notice notice)
        {
            noticeText.text = notice.Text;
            noticeText.color = notice.Tone == (int)NoticeTone.Bad ? new Color(1f, 0.68f, 0.62f) :
                notice.Tone == (int)NoticeTone.Warn ? PhoneTheme.Gold :
                notice.Tone == (int)NoticeTone.Good ? PhoneTheme.Jade : PhoneTheme.Text;
            noticeGroup.alpha = 1f;
            noticeTimer = 4.2f;
            noticeRoot.anchoredPosition = new Vector2(0f, 350f);
        }

        private void FadeNotice()
        {
            if (noticeGroup == null || noticeGroup.alpha <= 0f) return;
            noticeTimer -= Time.unscaledDeltaTime;
            if (noticeTimer < 0f) noticeGroup.alpha = Mathf.MoveTowards(noticeGroup.alpha, 0f, Time.unscaledDeltaTime * 2f);
        }

        private void FadeDamage()
        {
            if (flashAlpha <= 0f) return;
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 1.6f);
            damageFlash.color = new Color(0.6f, 0.04f, 0.03f, flashAlpha);
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

        public void ShowDeath()
        {
            if (deathRoot != null) deathRoot.SetActive(true);
        }

        public void HideDeath()
        {
            if (deathRoot != null) deathRoot.SetActive(false);
        }
    }
}

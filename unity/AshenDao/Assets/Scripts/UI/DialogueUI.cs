using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AshenDao.Game;
using AshenDao.Sim;

namespace AshenDao.UI
{
    public class DialogueUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public string NpcId { get; private set; }

        private GameRoot root;
        private WorldSim sim;
        private Canvas canvas;
        private DialogueSession session;
        private RectTransform panel;
        private Text speaker;
        private Text body;
        private readonly List<GameObject> buttons = new List<GameObject>();

        public void Setup(GameRoot owner)
        {
            root = owner;
            sim = owner.Sim;
            canvas = UiFactory.CreateCanvas("DialogueCanvas", 180);
            canvas.transform.SetParent(transform, false);

            var dim = UiFactory.FullScreen(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.42f));
            dim.GetComponent<Image>().raycastTarget = true;

            panel = UiFactory.CenterPanel(canvas.transform, "Dialogue", new Vector2(1040f, 380f),
                new Color(0.035f, 0.07f, 0.06f, 0.96f), 18f);
            panel.anchoredPosition = new Vector2(0f, -230f);

            speaker = UiFactory.Label(panel, "Speaker", "", 20, PhoneTheme.Jade, TextAnchor.UpperLeft, false);
            UiFactory.Place(speaker, 28f, 20f, 500f, 30f);
            var hint = UiFactory.Label(panel, "Hint", "对话 · 点击选项继续", 9, PhoneTheme.Muted,
                TextAnchor.UpperRight, false);
            UiFactory.Place(hint, 700f, 26f, 310f, 20f);

            body = UiFactory.Label(panel, "Body", "", 15, PhoneTheme.Text, TextAnchor.UpperLeft, true);
            UiFactory.Place(body, 28f, 60f, 980f, 72f);

            canvas.gameObject.SetActive(false);
        }

        public void Open(string npcId)
        {
            if (IsOpen) return;
            var next = sim.StartDialogue(npcId);
            if (next == null)
            {
                sim.Notify("对方没有回应。", NoticeTone.Warn);
                return;
            }
            session = next;
            NpcId = npcId;
            IsOpen = true;
            canvas.gameObject.SetActive(true);
            Rebuild();
        }

        private void Rebuild()
        {
            if (session == null) return;
            speaker.text = session.Speaker;
            body.text = session.Text;
            for (var i = 0; i < buttons.Count; i++) Destroy(buttons[i]);
            buttons.Clear();

            if (session.Choices.Count == 0)
            {
                var close = UiFactory.Button(panel, "Choice0", "离开", 14, new Vector2(980f, 42f),
                    new Vector2(28f, 170f), Close, PhoneTheme.CardLight, PhoneTheme.Jade);
                buttons.Add(close.gameObject);
                return;
            }

            for (var i = 0; i < session.Choices.Count && i < 5; i++)
            {
                var index = i;
                var choice = session.Choices[i];
                var button = UiFactory.Button(panel, "Choice" + i, choice.Text, 14, new Vector2(980f, 42f),
                    new Vector2(28f, 160f + i * 48f), () => Choose(index), PhoneTheme.CardLight, PhoneTheme.Text);
                buttons.Add(button.gameObject);
            }
        }

        private void Choose(int index)
        {
            sim.ChooseDialogue(session, index);
            if (session.Done) Close();
            else Rebuild();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            canvas.gameObject.SetActive(false);
            session = null;
            NpcId = null;
            var trader = sim.ConsumePendingTrade();
            if (!string.IsNullOrEmpty(trader) && root != null) root.OpenTrade(trader);
        }
    }
}

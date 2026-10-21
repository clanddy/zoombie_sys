using UnityEngine;
using UnityEngine.UI;
using AshenDao.Game;
using AshenDao.Sim;

namespace AshenDao.UI
{
    public class TradeUI : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public string TraderId { get; private set; }

        private GameRoot root;
        private WorldSim sim;
        private Canvas canvas;
        private RectTransform panel;
        private Text title;
        private Text wallet;
        private RectTransform content;

        public void Setup(GameRoot owner)
        {
            root = owner;
            sim = owner.Sim;
            canvas = UiFactory.CreateCanvas("TradeCanvas", 170);
            canvas.transform.SetParent(transform, false);

            var dim = UiFactory.FullScreen(canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.48f));
            dim.GetComponent<Image>().raycastTarget = true;

            panel = UiFactory.CenterPanel(canvas.transform, "Trade", new Vector2(980f, 700f),
                new Color(0.035f, 0.07f, 0.06f, 0.97f), 18f);
            title = UiFactory.Label(panel, "Title", "交易", 24, PhoneTheme.Jade, TextAnchor.UpperLeft, false);
            UiFactory.Place(title, 26f, 20f, 520f, 34f);
            wallet = UiFactory.Label(panel, "Wallet", "", 11, PhoneTheme.Gold, TextAnchor.MiddleRight, false);
            UiFactory.Place(wallet, 560f, 28f, 280f, 22f);
            var close = UiFactory.Button(panel, "Close", "关闭", 12, new Vector2(80f, 32f),
                new Vector2(860f, 18f), Close, PhoneTheme.CardLight, PhoneTheme.Muted);
            close.GetComponent<Image>().raycastTarget = true;

            var viewport = UiFactory.Scroll(panel, 22f, 78f, 936f, 590f, out _, out content);
            viewport.GetComponent<Image>().raycastTarget = true;
            canvas.gameObject.SetActive(false);
        }

        public void Open(string traderId)
        {
            var trader = sim.Trade.Trader(traderId);
            if (trader == null)
            {
                sim.Notify("没有这个商人。", NoticeTone.Warn);
                return;
            }
            TraderId = traderId;
            IsOpen = true;
            canvas.gameObject.SetActive(true);
            Rebuild();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            canvas.gameObject.SetActive(false);
            TraderId = null;
        }

        private void Rebuild()
        {
            var trader = sim.Trade.Trader(TraderId);
            if (trader == null) return;
            title.text = trader.Name;
            wallet.text = sim.Trade.CurrencyName(trader.Currency) + "  " +
                          sim.Trade.CurrencyCount(trader.Currency) + "   ·   旧币 " + sim.Wallet.Coin;
            UiFactory.Clear(content);

            foreach (var offer in trader.Offers)
            {
                var item = sim.Content.Item(offer.ItemId);
                if (item == null) continue;
                var card = UiFactory.Card(content, 78f);
                var name = UiFactory.Label(card, "Name", item.Name, 14, PhoneTheme.Text,
                    TextAnchor.UpperLeft, false);
                UiFactory.Place(name, 14f, 10f, 260f, 24f);
                var price = UiFactory.Label(card, "Price",
                    "买 " + sim.Trade.BuyPrice(trader, offer) + " " + sim.Trade.CurrencyName(trader.Currency) +
                    "  ·  卖 " + sim.Trade.SellPrice(trader, offer) + "  ·  库存 " +
                    sim.Trade.StockOf(trader, offer), 10, PhoneTheme.Muted, TextAnchor.UpperLeft, false);
                UiFactory.Place(price, 14f, 36f, 560f, 20f);
                var stats = UiFactory.Label(card, "Stats", item.Description, 9, PhoneTheme.Muted,
                    TextAnchor.UpperLeft, true);
                UiFactory.Place(stats, 14f, 55f, 620f, 18f);

                var buy = UiFactory.Button(card, "Buy", "买入", 11, new Vector2(64f, 30f),
                    new Vector2(760f, 12f), () =>
                    {
                        if (sim.Buy(TraderId, offer.ItemId, 1)) Rebuild();
                    }, PhoneTheme.CardLight, PhoneTheme.Jade);
                buy.GetComponent<Image>().raycastTarget = true;
                var sell = UiFactory.Button(card, "Sell", "卖出", 11, new Vector2(64f, 30f),
                    new Vector2(832f, 12f), () =>
                    {
                        if (sim.Sell(TraderId, offer.ItemId, 1)) Rebuild();
                    }, PhoneTheme.CardLight, PhoneTheme.Gold);
                sell.GetComponent<Image>().raycastTarget = true;
            }
            Canvas.ForceUpdateCanvases();
        }
    }
}

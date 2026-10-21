using System;
using System.Collections.Generic;

namespace AshenDao.Sim
{
    [Serializable]
    public class Wallet
    {
        public int Coin;
        public int Crystal;
        public int SpiritStone;

        public int Get(string currency)
        {
            switch (currency)
            {
                case "coin": return Coin;
                case "crystal": return Crystal;
                case "spirit_stone": return SpiritStone;
                default: return 0;
            }
        }

        public void Add(string currency, int amount)
        {
            if (amount == 0) return;
            switch (currency)
            {
                case "coin": Coin = Math.Max(0, Coin + amount); break;
                case "crystal": Crystal = Math.Max(0, Crystal + amount); break;
                case "spirit_stone": SpiritStone = Math.Max(0, SpiritStone + amount); break;
            }
        }

        public bool Spend(string currency, int amount)
        {
            if (amount < 0) return false;
            if (Get(currency) < amount) return false;
            Add(currency, -amount);
            return true;
        }

        public string Text
        {
            get { return "旧币 " + Coin; }
        }
    }

    public class TradeSystem
    {
        private readonly WorldSim sim;
        private readonly Dictionary<string, Dictionary<string, int>> stock =
            new Dictionary<string, Dictionary<string, int>>();

        public TradeSystem(WorldSim world)
        {
            sim = world;
        }

        public TraderDef Trader(string id)
        {
            return sim.Content.Trader(id);
        }

        public int StockOf(TraderDef trader, TradeOfferDef offer)
        {
            if (trader == null || offer == null) return 0;
            if (offer.Stock < 0) return 9999;
            if (!stock.TryGetValue(trader.Id, out var items))
            {
                items = new Dictionary<string, int>();
                stock[trader.Id] = items;
            }
            if (!items.TryGetValue(offer.ItemId, out var count))
            {
                count = offer.Stock;
                items[offer.ItemId] = count;
            }
            return count;
        }

        private void ChangeStock(TraderDef trader, TradeOfferDef offer, int delta)
        {
            if (trader == null || offer == null || offer.Stock < 0) return;
            var current = StockOf(trader, offer);
            stock[trader.Id][offer.ItemId] = Math.Max(0, current + delta);
        }

        public int CurrencyCount(string currency)
        {
            if (string.IsNullOrEmpty(currency)) return 0;
            var item = sim.Content.Item(currency);
            return item != null ? sim.CountItem(currency) : sim.Wallet.Get(currency);
        }

        public bool SpendCurrency(string currency, int amount)
        {
            var item = sim.Content.Item(currency);
            if (item != null) return sim.RemoveItem(currency, amount);
            return sim.Wallet.Spend(currency, amount);
        }

        public void AddCurrency(string currency, int amount)
        {
            var item = sim.Content.Item(currency);
            if (item != null) sim.AddItem(currency, amount, false);
            else sim.Wallet.Add(currency, amount);
        }

        public int BuyPrice(TraderDef trader, TradeOfferDef offer)
        {
            if (offer == null) return 0;
            if (offer.Price > 0) return offer.Price;
            var item = sim.Content.Item(offer.ItemId);
            return item != null ? Math.Max(1, item.Price) : 1;
        }

        public int SellPrice(TraderDef trader, TradeOfferDef offer)
        {
            if (offer == null) return 0;
            if (offer.SellPrice > 0) return offer.SellPrice;
            var item = sim.Content.Item(offer.ItemId);
            return item != null ? Math.Max(1, item.SellPrice) : 0;
        }

        public bool Buy(string traderId, string itemId, int count = 1)
        {
            var trader = Trader(traderId);
            if (trader == null || count <= 0) return false;
            var offer = trader.Offers.Find(o => o.ItemId == itemId);
            if (offer == null) return false;
            var available = StockOf(trader, offer);
            if (available < count)
            {
                sim.Notify("货不够了。", NoticeTone.Warn);
                return false;
            }
            var total = BuyPrice(trader, offer) * count;
            if (CurrencyCount(trader.Currency) < total)
            {
                sim.Notify("「" + CurrencyName(trader.Currency) + "」不够。", NoticeTone.Warn);
                return false;
            }

            var left = sim.AddItem(itemId, count, false);
            var bought = count - left;
            if (bought <= 0)
            {
                sim.Notify("背包放不下。", NoticeTone.Warn);
                return false;
            }
            SpendCurrency(trader.Currency, BuyPrice(trader, offer) * bought);
            ChangeStock(trader, offer, -bought);
            sim.Stats.Trades++;
            var item = sim.Content.Item(itemId);
            sim.RecordEvent(GameEventType.Trade, "buy." + traderId,
                "从 " + trader.Name + " 购入 " + (item != null ? item.Name : itemId) + " ×" + bought, bought);
            sim.Notify("购入 " + (item != null ? item.Name : itemId) + " ×" + bought, NoticeTone.Good);
            return true;
        }

        public bool Sell(string traderId, string itemId, int count = 1)
        {
            var trader = Trader(traderId);
            if (trader == null || count <= 0) return false;
            var item = sim.Content.Item(itemId);
            if (item == null || !item.Tradeable || sim.CountItem(itemId) < count)
            {
                sim.Notify("没有可卖的物品。", NoticeTone.Warn);
                return false;
            }
            var offer = trader.Offers.Find(o => o.ItemId == itemId);
            var unit = offer != null ? SellPrice(trader, offer) : Math.Max(1, item.SellPrice);
            if (unit <= 0)
            {
                sim.Notify("对方不收这个。", NoticeTone.Warn);
                return false;
            }
            if (!sim.RemoveItem(itemId, count)) return false;
            AddCurrency(trader.Currency, unit * count);
            if (offer != null) ChangeStock(trader, offer, count);
            sim.Stats.Trades++;
            sim.RecordEvent(GameEventType.Trade, "sell." + traderId,
                "向 " + trader.Name + " 出售 " + item.Name + " ×" + count, count);
            sim.Notify("售出 " + item.Name + " ×" + count + "，获得 " + CurrencyName(trader.Currency) + " " + (unit * count),
                NoticeTone.Good);
            return true;
        }

        public string CurrencyName(string currency)
        {
            if (currency == "crystal_core") return "晶核";
            if (currency == "coin") return "旧币";
            if (currency == "spirit_stone") return "灵石";
            var item = sim.Content.Item(currency);
            return item != null ? item.Name : currency;
        }

        public List<TradeOfferDef> Offers(string traderId)
        {
            var trader = Trader(traderId);
            return trader != null ? trader.Offers : new List<TradeOfferDef>();
        }

        public List<TraderStockSave> ExportStock()
        {
            var list = new List<TraderStockSave>();
            foreach (var pair in stock)
            {
                foreach (var item in pair.Value)
                {
                    list.Add(new TraderStockSave { TraderId = pair.Key, ItemId = item.Key, Count = item.Value });
                }
            }
            return list;
        }

        public void ImportStock(List<TraderStockSave> list)
        {
            stock.Clear();
            if (list == null) return;
            foreach (var entry in list)
            {
                if (string.IsNullOrEmpty(entry.TraderId) || string.IsNullOrEmpty(entry.ItemId)) continue;
                if (!stock.TryGetValue(entry.TraderId, out var items))
                {
                    items = new Dictionary<string, int>();
                    stock[entry.TraderId] = items;
                }
                items[entry.ItemId] = Math.Max(0, entry.Count);
            }
        }
    }

    [Serializable]
    public class LootResult
    {
        public string ItemId;
        public string Currency;
        public int Count;
        public int Amount;

        public string DisplayName(ContentDatabase content)
        {
            if (!string.IsNullOrEmpty(ItemId))
            {
                var item = content.Item(ItemId);
                return (item != null ? item.Name : ItemId) + " ×" + Count;
            }
            return Currency == "coin" ? "旧币 ×" + Amount : Currency + " ×" + Amount;
        }
    }

    public class LootSystem
    {
        private readonly WorldSim sim;

        public LootSystem(WorldSim world)
        {
            sim = world;
        }

        public List<LootResult> Roll(string tableId)
        {
            var results = new List<LootResult>();
            var table = sim.Content.LootTable(tableId);
            if (table == null) return results;
            var rolls = table.MinRolls + sim.RandomInt(0, Math.Max(0, table.MaxRolls - table.MinRolls) + 1);
            for (var r = 0; r < rolls; r++)
            {
                foreach (var entry in table.Entries)
                {
                    if (sim.RandomFloat() > entry.Chance) continue;
                    if (!string.IsNullOrEmpty(entry.ItemId))
                    {
                        var count = entry.Min + sim.RandomInt(0, Math.Max(0, entry.Max - entry.Min) + 1);
                        var existing = results.Find(x => x.ItemId == entry.ItemId);
                        if (existing != null) existing.Count += count;
                        else results.Add(new LootResult { ItemId = entry.ItemId, Count = count });
                    }
                    else if (!string.IsNullOrEmpty(entry.Currency))
                    {
                        var amount = entry.Amount > 0
                            ? entry.Amount
                            : entry.Min + sim.RandomInt(0, Math.Max(0, entry.Max - entry.Min) + 1);
                        var existing = results.Find(x => x.Currency == entry.Currency);
                        if (existing != null) existing.Amount += amount;
                        else results.Add(new LootResult { Currency = entry.Currency, Amount = amount });
                    }
                }
            }
            return results;
        }
    }
}

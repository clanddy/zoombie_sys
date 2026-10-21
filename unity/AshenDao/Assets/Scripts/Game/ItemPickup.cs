using UnityEngine;
using AshenDao.Sim;

namespace AshenDao.Game
{
    public class PickupInteractable : InteractableBase
    {
        public string ItemId;
        public int Count = 1;
        public string Currency;
        public int Amount;
        public string Label;

        public override string Prompt => "拾取 " + Label;

        public override void Interact(PlayerController player)
        {
            var sim = GameRoot.Instance.Sim;
            if (!string.IsNullOrEmpty(Currency))
            {
                sim.Wallet.Add(Currency, Amount);
                sim.RecordEvent(GameEventType.Loot, "loot.currency." + Currency,
                    "拾取" + (Currency == "coin" ? "旧币" : Currency) + " ×" + Amount, Amount);
                sim.Notify("拾取 " + (Currency == "coin" ? "旧币" : Currency) + " ×" + Amount, NoticeTone.Good);
                Destroy(gameObject);
                return;
            }

            var left = sim.AddItem(ItemId, Count, false);
            if (left > 0)
            {
                Count = left;
                sim.Notify("背包放不下。", NoticeTone.Warn);
                return;
            }

            sim.Stats.ItemsLooted += Count;
            if (ItemId == "crystal_core") sim.Stats.CrystalsCollected += Count;
            sim.RecordEvent(GameEventType.Loot, "loot." + ItemId, "拾取" + Label, Count);
            sim.Notify("拾取 " + Label, NoticeTone.Good);
            Destroy(gameObject);
        }
    }

    public class ItemPickup : MonoBehaviour
    {
        public static PickupInteractable Create(Transform parent, Vector3 position, LootResult loot, ContentDatabase content)
        {
            var go = new GameObject("Pickup_" + (string.IsNullOrEmpty(loot.ItemId) ? loot.Currency : loot.ItemId));
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var pickup = go.AddComponent<PickupInteractable>();
            pickup.ItemId = loot.ItemId;
            pickup.Count = loot.Count;
            pickup.Currency = loot.Currency;
            pickup.Amount = loot.Amount;
            var item = !string.IsNullOrEmpty(loot.ItemId) ? content.Item(loot.ItemId) : null;
            pickup.Label = item != null ? item.Name + " ×" + loot.Count : "旧币 ×" + loot.Amount;

            var color = item != null ? new Color(0.55f, 0.9f, 0.75f, 0.9f) : new Color(0.9f, 0.75f, 0.35f, 0.9f);
            var mat = MaterialFactory.Emissive(color, color);
            var cube = PrimitiveFactory.Box(go.transform, "Visual", new Vector3(0f, 0.22f, 0f),
                new Vector3(0.28f, 0.28f, 0.28f), mat, false);
            cube.transform.localRotation = Quaternion.Euler(20f, 35f, 12f);
            return pickup;
        }
    }
}

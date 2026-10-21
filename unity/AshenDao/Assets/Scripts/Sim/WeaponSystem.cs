using System;

namespace AshenDao.Sim
{
    public class ShotSpec
    {
        public string WeaponId;
        public string ItemName;
        public float Damage;
        public float Range;
        public float Spread;
        public int Pellets = 1;
        public float NoiseRadius;
        public bool Automatic;
    }

    public partial class WorldSim
    {
        public int MagazineSize(string weaponId)
        {
            var item = Content.Item(weaponId);
            return item != null && item.Weapon != null ? item.Weapon.Magazine : 0;
        }

        public int Magazine(string weaponId)
        {
            var size = MagazineSize(weaponId);
            if (size <= 0) return 0;
            if (!Player.Magazines.TryGetValue(weaponId, out var count))
            {
                count = size;
                Player.Magazines[weaponId] = count;
            }
            return count;
        }

        public void EnsureMagazine(string weaponId)
        {
            var size = MagazineSize(weaponId);
            if (size <= 0 || Player.Magazines.ContainsKey(weaponId)) return;
            Player.Magazines[weaponId] = size;
        }

        public string AmmoText
        {
            get
            {
                var item = Player.Equipped != null ? Content.Item(Player.Equipped) : null;
                if (item == null || item.Weapon == null) return "";
                if (item.Weapon.Kind != "firearm") return "近战";
                return Magazine(item.Id) + " / " + MagazineSize(item.Id) + "  " + item.Weapon.AmmoItem;
            }
        }

        public bool IsFirearmEquipped
        {
            get
            {
                var item = Player.Equipped != null ? Content.Item(Player.Equipped) : null;
                return item != null && item.Weapon != null && item.Weapon.Kind == "firearm";
            }
        }

        public bool TryFire(out ShotSpec shot)
        {
            shot = null;
            if (Player.Dead || Meditating) return false;
            var item = Player.Equipped != null ? Content.Item(Player.Equipped) : null;
            if (item == null || item.Weapon == null || item.Weapon.Kind != "firearm") return false;
            var key = "weapon:" + item.Id;
            if (Player.Cooldowns.TryGetValue(key, out var cooldown) && cooldown > 0f) return false;
            if (Player.Stamina < item.Weapon.StaminaCost) return false;
            if (Magazine(item.Id) <= 0)
            {
                Notify("弹匣空了，按 F 换弹。", NoticeTone.Warn);
                return false;
            }

            Player.Magazines[item.Id] = Magazine(item.Id) - 1;
            Player.Cooldowns[key] = item.Weapon.Cooldown;
            Player.Stamina = Math.Max(0f, Player.Stamina - item.Weapon.StaminaCost);
            Stats.Shots++;
            RecordEvent(GameEventType.Weapon, "fire." + item.Id, "开火：" + item.Name, 1);
            shot = new ShotSpec
            {
                WeaponId = item.Id,
                ItemName = item.Name,
                Damage = item.Weapon.Damage * DamageMultiplier,
                Range = item.Weapon.Range,
                Spread = item.Weapon.Spread,
                Pellets = Math.Max(1, item.Weapon.Pellets),
                NoiseRadius = item.Weapon.NoiseRadius,
                Automatic = item.Weapon.Automatic
            };
            return true;
        }

        public bool Reload()
        {
            if (Player.Dead || Meditating) return false;
            var item = Player.Equipped != null ? Content.Item(Player.Equipped) : null;
            if (item == null || item.Weapon == null || item.Weapon.Kind != "firearm") return false;
            var size = MagazineSize(item.Id);
            var current = Magazine(item.Id);
            var need = size - current;
            if (need <= 0)
            {
                Notify("弹匣是满的。", NoticeTone.Info);
                return false;
            }
            if (CountItem(item.Weapon.AmmoItem) <= 0)
            {
                Notify("没有 " + (Content.Item(item.Weapon.AmmoItem)?.Name ?? item.Weapon.AmmoItem) + "。", NoticeTone.Warn);
                return false;
            }
            var take = Math.Min(need, CountItem(item.Weapon.AmmoItem));
            RemoveItem(item.Weapon.AmmoItem, take);
            Player.Magazines[item.Id] = current + take;
            Player.Cooldowns["weapon:" + item.Id] = item.Weapon.ReloadTime;
            Stats.Reloads++;
            RecordEvent(GameEventType.Weapon, "reload." + item.Id, "换弹：" + item.Name, take);
            Notify("换弹完成（" + (current + take) + "/" + size + "）。", NoticeTone.Info);
            return true;
        }

        public bool EquipItem(string itemId)
        {
            var item = Content.Item(itemId);
            if (item == null || CountItem(itemId) <= 0) return false;
            if (item.Weapon != null)
            {
                Player.Equipped = itemId;
                if (item.Weapon.Kind == "firearm") EnsureMagazine(itemId);
                RecordEvent(GameEventType.Weapon, "equip." + itemId, "装备：" + item.Name, 1);
                Notify("装备 " + item.Name + (item.Weapon.Kind == "firearm"
                    ? "（" + Magazine(itemId) + "/" + MagazineSize(itemId) + "）"
                    : ""), NoticeTone.Info);
                return true;
            }
            if (!string.IsNullOrEmpty(item.Slot))
            {
                Player.Clothing[item.Slot] = itemId;
                RecordEvent(GameEventType.Loot, "wear." + itemId, "穿戴：" + item.Name, 1);
                Notify("穿戴 " + item.Name + "（护甲 +" + item.Armor + "）", NoticeTone.Good);
                return true;
            }
            return false;
        }

        public float ClothingArmor()
        {
            var total = 0f;
            foreach (var pair in Player.Clothing)
            {
                var item = Content.Item(pair.Value);
                if (item != null) total += item.Armor;
            }
            return total;
        }

        public float Armor => Mods.Armor + ClothingArmor();
    }
}

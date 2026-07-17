using System.Collections.Generic;
using UnityEngine;

namespace Prison.Social
{
    /// <summary>One purchasable line in an NPC's daily stock.</summary>
    public class TradeStockEntry
    {
        public ItemData item;
        public int count;
        public float unitPrice; // computed live at menu-open time, cached here for display
    }

    /// <summary>
    /// Daily trade stock per NPC (spec §8): refreshed at Morning Count from archetype stock
    /// ranges, drawing from <see cref="ItemDatabase"/>. Gang store purchases deliver under the
    /// player's bed the next morning after headcount. Plain class owned by <see cref="SocialWorld"/>.
    /// </summary>
    public class TradingService
    {
        private readonly SocialRoster _roster;
        private readonly Dictionary<int, List<TradeStockEntry>> _stockByActor = new Dictionary<int, List<TradeStockEntry>>();
        private readonly List<(ItemData item, int count)> _pendingBedDeliveries = new List<(ItemData, int)>();
        private System.Random _rng;

        public TradingService(SocialRoster roster, int seed)
        {
            _roster = roster;
            _rng = new System.Random(seed);
        }

        public IReadOnlyList<TradeStockEntry> GetStock(int actorId) =>
            _stockByActor.TryGetValue(actorId, out var list) ? list : (IReadOnlyList<TradeStockEntry>)System.Array.Empty<TradeStockEntry>();

        public bool HasStockToday(int actorId) => GetStock(actorId).Count > 0;

        /// <summary>Rolls fresh stock for every trading inmate. Call on Morning Roll Call.</summary>
        public void RefreshDailyStock()
        {
            _stockByActor.Clear();

            var candidates = AllTradableItems();
            if (candidates.Count == 0) return;

            foreach (var identity in _roster.Inmates())
            {
                var profile = ArchetypeCatalog.Get(identity.archetype);
                if (profile.stockMax <= 0) continue;

                int count = _rng.Next(profile.stockMin, profile.stockMax + 1);
                if (count <= 0) continue;

                var stock = new List<TradeStockEntry>(count);
                for (int i = 0; i < count; i++)
                {
                    var item = PickWeighted(candidates, identity.archetype == PrisonerArchetype.Hustler);
                    if (item == null) continue;
                    var existing = stock.Find(e => e.item == item);
                    if (existing != null) existing.count++;
                    else stock.Add(new TradeStockEntry { item = item, count = 1 });
                }
                if (stock.Count > 0)
                    _stockByActor[identity.actorId] = stock;
            }

            RefreshGangStores();
        }

        private readonly Dictionary<int, List<TradeStockEntry>> _gangStoreStock = new Dictionary<int, List<TradeStockEntry>>();

        /// <summary>
        /// Gang store stock (spec §5, Syndicate first): rolled daily, hustler-grade selection.
        /// Only meaningful for gangs whose profile has <c>hasStore</c>.
        /// </summary>
        public IReadOnlyList<TradeStockEntry> GetGangStoreStock(int gangId)
        {
            if (!_gangStoreStock.TryGetValue(gangId, out var list))
                return System.Array.Empty<TradeStockEntry>();
            return list;
        }

        private void RefreshGangStores()
        {
            _gangStoreStock.Clear();
            var candidates = AllTradableItems();
            if (candidates.Count == 0) return;

            foreach (var gang in GangCatalog.All())
            {
                if (!gang.hasStore) continue;
                var stock = new List<TradeStockEntry>();
                for (int i = 0; i < 4; i++)
                {
                    var item = PickWeighted(candidates, true);
                    if (item == null) continue;
                    var existing = stock.Find(e => e.item == item);
                    if (existing != null) existing.count++;
                    else stock.Add(new TradeStockEntry { item = item, count = 1 });
                }
                _gangStoreStock[gang.gangId] = stock;
            }
        }

        /// <summary>Gang store order: queued, then delivered under the bed after the next morning headcount.</summary>
        public void QueueBedDelivery(ItemData item, int count) =>
            _pendingBedDeliveries.Add((item, count));

        /// <summary>Drains pending gang-store deliveries into the player's inventory (call after morning count).</summary>
        public int DeliverPendingToBed(PlayerInventory inventory)
        {
            if (inventory == null || _pendingBedDeliveries.Count == 0) return 0;
            int delivered = 0;
            for (int i = _pendingBedDeliveries.Count - 1; i >= 0; i--)
            {
                var (item, count) = _pendingBedDeliveries[i];
                if (inventory.AddItem(item, count))
                {
                    delivered += count;
                    _pendingBedDeliveries.RemoveAt(i);
                }
            }
            return delivered;
        }

        public int PendingBedDeliveryCount => _pendingBedDeliveries.Count;

        /// <summary>Removes one unit from an NPC's stock after a successful purchase.</summary>
        public void ConsumeStock(int actorId, ItemData item)
        {
            if (!_stockByActor.TryGetValue(actorId, out var stock)) return;
            var entry = stock.Find(e => e.item == item);
            if (entry == null) return;
            entry.count--;
            if (entry.count <= 0) stock.Remove(entry);
        }

        private static List<ItemData> AllTradableItems()
        {
            var result = new List<ItemData>();
            var db = ItemDatabase.Singleton;
            if (db == null || db.allItemsInGame == null) return result;
            foreach (var item in db.allItemsInGame)
            {
                if (item == null) continue;
                result.Add(item);
            }
            return result;
        }

        /// <summary>Hustlers can roll rare/contraband; everyone else sticks to common goods.</summary>
        private ItemData PickWeighted(List<ItemData> candidates, bool isHustler)
        {
            float total = 0f;
            for (int i = 0; i < candidates.Count; i++)
                total += Weight(candidates[i], isHustler);
            if (total <= 0f) return null;

            float roll = (float)(_rng.NextDouble() * total);
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= Weight(candidates[i], isHustler);
                if (roll <= 0f) return candidates[i];
            }
            return candidates[candidates.Count - 1];
        }

        private static float Weight(ItemData item, bool isHustler)
        {
            bool restricted = item.category == ItemCategory.Contraband
                || item.category == ItemCategory.Weapon
                || item.rarity == ItemRarity.Rare
                || item.rarity == ItemRarity.Legendary;
            if (restricted && !isHustler) return 0f;

            float w;
            switch (item.rarity)
            {
                case ItemRarity.Uncommon: w = 2.5f; break;
                case ItemRarity.Rare: w = 1f; break;
                case ItemRarity.Legendary: w = 0.25f; break;
                default: w = 4f; break;
            }
            return w * Mathf.Max(0.01f, item.weightMultiplier);
        }
    }
}

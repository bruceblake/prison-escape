using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Prison;

namespace Prison.Tests
{
    /// <summary>
    /// EditMode unit tests for inventory, crafting, loot weighting, contraband classification,
    /// and the security-alert event hooks:
    /// <see cref="PlayerInventory"/>, <see cref="CraftingSystem"/>, <see cref="CraftingRecipeDescription"/>,
    /// <see cref="LootTable.GetRarityBaseWeight"/>, <see cref="MorningShakedownSweeper.ShouldConfiscate"/>,
    /// <see cref="PrisonSecurityAlerts"/>.
    /// </summary>
    public class CraftingInventoryLootTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _created)
                if (o != null) UnityEngine.Object.DestroyImmediate(o);
            _created.Clear();
        }

        private ItemData NewItem(string itemName, ItemCategory category = ItemCategory.CraftingPart, ItemRarity rarity = ItemRarity.Common)
        {
            var i = ScriptableObject.CreateInstance<ItemData>();
            i.itemName = itemName;
            i.category = category;
            i.rarity = rarity;
            _created.Add(i);
            return i;
        }

        private PlayerInventory NewInventory(int maxSlots = 6)
        {
            var go = new GameObject("Inventory");
            _created.Add(go);
            var inv = go.AddComponent<PlayerInventory>();
            inv.maxSlots = maxSlots;
            inv.EnsureSlotCapacity();
            return inv;
        }

        private CraftingRecipe NewRecipe(ItemData result, int resultAmount, params (ItemData item, int amount)[] ingredients)
        {
            var r = ScriptableObject.CreateInstance<CraftingRecipe>();
            r.recipeName = "Test Recipe";
            r.result = result;
            r.resultAmount = resultAmount;
            r.ingredients = ingredients
                .Select(t => new CraftingIngredient { item = t.item, amount = t.amount })
                .ToArray();
            _created.Add(r);
            return r;
        }

        // ============ PlayerInventory ============
        [Test]
        public void AddItem_CraftingPart_Stacks()
        {
            var inv = NewInventory();
            var part = NewItem("Soap", ItemCategory.CraftingPart);
            Assert.IsTrue(inv.AddItem(part, 2));
            Assert.IsTrue(inv.AddItem(part, 3));
            Assert.AreEqual(5, inv.CountItem(part));
        }

        [Test]
        public void AddItem_NonStackable_UsesSeparateSlots()
        {
            var inv = NewInventory();
            var tool = NewItem("Screwdriver", ItemCategory.Tool);
            Assert.IsTrue(inv.AddItem(tool));
            Assert.IsTrue(inv.AddItem(tool));
            Assert.AreEqual(2, inv.CountItem(tool));
        }

        [Test]
        public void AddItem_FullInventory_ReturnsFalse()
        {
            var inv = NewInventory(3);
            Assert.IsTrue(inv.AddItem(NewItem("A", ItemCategory.Tool)));
            Assert.IsTrue(inv.AddItem(NewItem("B", ItemCategory.Tool)));
            Assert.IsTrue(inv.AddItem(NewItem("C", ItemCategory.Tool)));
            Assert.IsFalse(inv.AddItem(NewItem("D", ItemCategory.Tool)), "Inventory should reject items when full.");
        }

        [Test]
        public void HasItem_RespectsRequiredAmount()
        {
            var inv = NewInventory();
            var part = NewItem("Wire", ItemCategory.CraftingPart);
            inv.AddItem(part, 2);
            Assert.IsTrue(inv.HasItem(part, 2));
            Assert.IsFalse(inv.HasItem(part, 3));
        }

        [Test]
        public void HasItem_Null_False()
            => Assert.IsFalse(NewInventory().HasItem(null));

        [Test]
        public void CountItem_MatchesByItemName_AcrossInstances()
        {
            var inv = NewInventory();
            var soapA = NewItem("Soap", ItemCategory.CraftingPart);
            var soapB = NewItem("Soap", ItemCategory.CraftingPart); // different asset, same name
            inv.AddItem(soapA, 2);
            Assert.AreEqual(2, inv.CountItem(soapB), "CountItem should match by itemName as well as reference.");
        }

        [Test]
        public void RemoveItem_DecrementsAndClearsSlot()
        {
            var inv = NewInventory();
            var part = NewItem("Wire", ItemCategory.CraftingPart);
            inv.AddItem(part, 5);
            Assert.IsTrue(inv.RemoveItem(part, 2));
            Assert.AreEqual(3, inv.CountItem(part));
            Assert.IsTrue(inv.RemoveItem(part, 3));
            Assert.AreEqual(0, inv.CountItem(part));
            Assert.IsFalse(inv.RemoveItem(part, 1), "Removing from empty should fail.");
        }

        [Test]
        public void AddItem_CraftingPart_StacksAcrossDuplicateAssetsByName()
        {
            var inv = NewInventory();
            var soapA = NewItem("Soap", ItemCategory.CraftingPart);
            var soapB = NewItem("Soap", ItemCategory.CraftingPart); // different asset, same name
            inv.AddItem(soapA, 2);
            inv.AddItem(soapB, 3);
            Assert.AreEqual(5, inv.CountItem(soapA));
            int occupied = 0;
            foreach (var slot in inv.inventorySlots)
                if (!slot.IsEmpty) occupied++;
            Assert.AreEqual(1, occupied, "Same-named crafting parts must merge into one stack, not split.");
        }

        [Test]
        public void RemoveItem_SpansMultipleStacks()
        {
            var inv = NewInventory();
            var wire = NewItem("Wire", ItemCategory.CraftingPart);
            var wireDupe = NewItem("Wire", ItemCategory.CraftingPart);
            // Force split stacks (legacy saves / scene-authored slots can contain them).
            inv.inventorySlots[0] = new InventorySlot(wire, 2);
            inv.inventorySlots[1] = new InventorySlot(wireDupe, 1);

            Assert.IsTrue(inv.HasItem(wire, 3), "HasItem sums across stacks");
            Assert.IsTrue(inv.RemoveItem(wire, 3), "RemoveItem must span stacks like HasItem does");
            Assert.AreEqual(0, inv.CountItem(wire));
        }

        [Test]
        public void GetEquippedItem_ReturnsSelectedSlotItem()
        {
            var inv = NewInventory();
            var part = NewItem("Wire", ItemCategory.CraftingPart);
            inv.AddItem(part, 1);
            inv.selectedSlotIndex = 0;
            Assert.AreEqual(part, inv.GetEquippedItem());
        }

        // ============ CraftingSystem ============
        [Test]
        public void CanCraft_NullArguments_False()
        {
            var inv = NewInventory();
            var recipe = NewRecipe(NewItem("Shiv", ItemCategory.Weapon), 1, (NewItem("Wire"), 1));
            Assert.IsFalse(CraftingSystem.CanCraft(null, inv));
            Assert.IsFalse(CraftingSystem.CanCraft(recipe, null));
        }

        [Test]
        public void CanCraft_True_WhenIngredientsPresent()
        {
            var inv = NewInventory();
            var wire = NewItem("Wire");
            inv.AddItem(wire, 2);
            var recipe = NewRecipe(NewItem("Shiv", ItemCategory.Weapon), 1, (wire, 2));
            Assert.IsTrue(CraftingSystem.CanCraft(recipe, inv));
        }

        [Test]
        public void CanCraft_False_WhenIngredientsInsufficient()
        {
            var inv = NewInventory();
            var wire = NewItem("Wire");
            inv.AddItem(wire, 1);
            var recipe = NewRecipe(NewItem("Shiv", ItemCategory.Weapon), 1, (wire, 2));
            Assert.IsFalse(CraftingSystem.CanCraft(recipe, inv));
        }

        [Test]
        public void TryCraft_ConsumesIngredients_ProducesResult()
        {
            var inv = NewInventory();
            var wire = NewItem("Wire");
            var handle = NewItem("Handle");
            inv.AddItem(wire, 2);
            inv.AddItem(handle, 1);
            var shiv = NewItem("Shiv", ItemCategory.Weapon);
            var recipe = NewRecipe(shiv, 1, (wire, 2), (handle, 1));

            Assert.IsTrue(CraftingSystem.TryCraft(recipe, inv));
            Assert.AreEqual(0, inv.CountItem(wire));
            Assert.AreEqual(0, inv.CountItem(handle));
            Assert.AreEqual(1, inv.CountItem(shiv));
            Assert.IsFalse(CraftingSystem.CanCraft(recipe, inv), "Should not be craftable again after consuming parts.");
        }

        [Test]
        public void TryCraft_Fails_WhenCannotCraft()
        {
            var inv = NewInventory();
            var wire = NewItem("Wire");
            var recipe = NewRecipe(NewItem("Shiv", ItemCategory.Weapon), 1, (wire, 2));
            Assert.IsFalse(CraftingSystem.TryCraft(recipe, inv));
        }

        [Test]
        public void TryCraft_NoRoomForResult_RefundsIngredients()
        {
            // 3 slots: wire×2 + two tools → consuming 1 wire leaves no empty slot for the
            // non-stackable result, so the craft must abort and give the wire back.
            var inv = NewInventory(3);
            var wire = NewItem("Wire");
            inv.AddItem(wire, 2);
            inv.AddItem(NewItem("ToolA", ItemCategory.Tool));
            inv.AddItem(NewItem("ToolB", ItemCategory.Tool));
            var shiv = NewItem("Shiv", ItemCategory.Weapon);
            var recipe = NewRecipe(shiv, 1, (wire, 1));

            Assert.IsFalse(CraftingSystem.TryCraft(recipe, inv), "Craft must fail when the result has no slot.");
            Assert.AreEqual(2, inv.CountItem(wire), "Consumed parts must be refunded.");
            Assert.AreEqual(0, inv.CountItem(shiv));
        }

        // ============ CraftingRecipeDescription ============
        [Test]
        public void IngredientRequirementLines_FormatsNameAndAmount()
        {
            var recipe = NewRecipe(NewItem("Shiv", ItemCategory.Weapon), 1,
                (NewItem("Soap"), 2), (NewItem("Wire"), 1));
            var lines = CraftingRecipeDescription.IngredientRequirementLines(recipe).ToList();
            CollectionAssert.AreEqual(new[] { "Soap \u00D72", "Wire \u00D71" }, lines);
        }

        [Test]
        public void IngredientRequirementLines_ZeroAmount_ClampsToOne()
        {
            var recipe = NewRecipe(NewItem("Shiv", ItemCategory.Weapon), 1, (NewItem("Soap"), 0));
            var lines = CraftingRecipeDescription.IngredientRequirementLines(recipe).ToList();
            CollectionAssert.AreEqual(new[] { "Soap \u00D71" }, lines);
        }

        [Test]
        public void IngredientRequirementLines_NullRecipe_Empty()
            => Assert.IsEmpty(CraftingRecipeDescription.IngredientRequirementLines(null).ToList());

        [Test]
        public void IngredientsRichParagraph_NoIngredients_Message()
        {
            var recipe = NewRecipe(NewItem("Shiv", ItemCategory.Weapon), 1);
            Assert.AreEqual("<i>No ingredients defined.</i>", CraftingRecipeDescription.IngredientsRichParagraph(recipe, NewInventory()));
        }

        [Test]
        public void IngredientsRichParagraph_HaveEnough_ShowsCheckAndCounts()
        {
            var inv = NewInventory();
            var soap = NewItem("Soap");
            inv.AddItem(soap, 2);
            var recipe = NewRecipe(NewItem("Shiv", ItemCategory.Weapon), 1, (soap, 2));
            string para = CraftingRecipeDescription.IngredientsRichParagraph(recipe, inv);
            StringAssert.Contains("2/2", para);
            StringAssert.Contains("\u2714", para); // ✔
        }

        [Test]
        public void IngredientsRichParagraph_Missing_ShowsBulletAndZero()
        {
            var inv = NewInventory();
            var soap = NewItem("Soap");
            var recipe = NewRecipe(NewItem("Shiv", ItemCategory.Weapon), 1, (soap, 2));
            string para = CraftingRecipeDescription.IngredientsRichParagraph(recipe, inv);
            StringAssert.Contains("0/2", para);
            StringAssert.Contains("\u2022", para); // •
        }

        // ============ LootTable.GetRarityBaseWeight ============
        [TestCase(ItemRarity.Common, 60f)]
        [TestCase(ItemRarity.Uncommon, 25f)]
        [TestCase(ItemRarity.Rare, 10f)]
        [TestCase(ItemRarity.Legendary, 5f)]
        public void GetRarityBaseWeight_MatchesDesign(ItemRarity rarity, float expected)
            => Assert.AreEqual(expected, LootTable.GetRarityBaseWeight(rarity), 1e-4f);

        // ============ MorningShakedownSweeper.ShouldConfiscate ============
        [Test]
        public void ShouldConfiscate_Null_False()
            => Assert.IsFalse(MorningShakedownSweeper.ShouldConfiscate(null));

        [TestCase(ItemCategory.Contraband, true)]
        [TestCase(ItemCategory.Tool, true)]
        [TestCase(ItemCategory.Weapon, true)]
        [TestCase(ItemCategory.CraftingPart, false)]
        [TestCase(ItemCategory.Consumable, false)]
        public void ShouldConfiscate_ByCategory(ItemCategory category, bool expected)
            => Assert.AreEqual(expected, MorningShakedownSweeper.ShouldConfiscate(NewItem("X", category)));

        // ============ PrisonSecurityAlerts ============
        [Test]
        public void RaiseLockdown_InvokesSubscriberWithReason()
        {
            string captured = null;
            System.Action<string> handler = r => captured = r;
            PrisonSecurityAlerts.OnLockdown += handler;
            try
            {
                PrisonSecurityAlerts.RaiseLockdown("escape attempt");
                Assert.AreEqual("escape attempt", captured);
            }
            finally
            {
                PrisonSecurityAlerts.OnLockdown -= handler;
            }
        }

        [Test]
        public void RaiseSuspicion_InvokesSubscriberWithReason()
        {
            string captured = null;
            System.Action<string> handler = r => captured = r;
            PrisonSecurityAlerts.OnSuspicion += handler;
            try
            {
                PrisonSecurityAlerts.RaiseSuspicion("loitering");
                Assert.AreEqual("loitering", captured);
            }
            finally
            {
                PrisonSecurityAlerts.OnSuspicion -= handler;
            }
        }
    }
}

using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Prison
{
    /// <summary>
    /// Desk / container: rolls loot in <see cref="Start"/> if <see cref="rollLootOnStart"/> is on; otherwise use <see cref="RollLoot"/> from your own code.
    /// </summary>
    public class WorldContainer : MonoBehaviour, IInteractable
    {
        [Header("Loot")]
        public LootTable lootTable;
        [Min(0)]
        public int minItems = 1;
        [Min(0)]
        public int maxItems = 3;

        [Tooltip("If true, runs RollLoot() in Start. Otherwise call RollLoot() manually (e.g. from a custom manager).")]
        [SerializeField] private bool rollLootOnStart = false;

        [Header("UI (optional)")]
        [Tooltip("If set, this panel is shown with the list of items when the player searches.")]
        [SerializeField] private GameObject lootPanel;
        [SerializeField] private TMP_Text lootBodyText;
        [SerializeField] private Button closeButton;
        [SerializeField] private string interactVerb = "Search";
        [SerializeField] private string closeVerb = "Close";

        [Header("Raycast")]
        [Tooltip("Add a small collider if the mesh has none so F ray hits this object.")]
        [SerializeField] private bool addBoxColliderIfMissing = true;
        [SerializeField] private Vector3 boxCenter = new Vector3(0f, 0.3f, 0f);
        [SerializeField] private Vector3 boxSize = new Vector3(1.2f, 0.6f, 0.8f);

        private readonly List<ItemData> _contents = new List<ItemData>();
        public IReadOnlyList<ItemData> Contents => _contents;

        public InteractionInputType InputType => InteractionInputType.Press;
        public float HoldDuration => 0f;

        public void RollLoot()
        {
            _contents.Clear();
            if (lootTable == null) return;
            int a = minItems;
            int b = maxItems;
            if (a > b) (a, b) = (b, a);
            int count = a < b
                ? UnityEngine.Random.Range(a, b + 1)
                : a;
            for (int i = 0; i < count; i++)
            {
                ItemData pick = lootTable.GetRandomItem();
                if (pick != null) _contents.Add(pick);
            }
        }

        private void Start()
        {
            if (addBoxColliderIfMissing && GetComponentInChildren<Collider>(true) == null)
            {
                var b = gameObject.AddComponent<BoxCollider>();
                b.isTrigger = false;
                b.center = boxCenter;
                b.size = boxSize;
            }

            if (lootPanel != null)
                lootPanel.SetActive(false);
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseLootPanel);

            if (rollLootOnStart) RollLoot();
        }

        public string GetInteractionPrompt(PlayerInventory inventory)
        {
            if (lootPanel != null && lootPanel.activeSelf)
                return $"[F] {closeVerb}";

            if (lootTable == null || _contents.Count == 0)
                return "[F] Empty";
            return $"[F] {interactVerb}";
        }

        public bool CanInteract(PlayerInventory inventory)
        {
            return true;
        }

        public void Interact(PlayerInventory inventory)
        {
            if (lootPanel != null)
            {
                if (lootPanel.activeSelf) CloseLootPanel();
                else OpenLootPanel();
            }
            else
            {
                var sb = new StringBuilder(64);
                for (int i = 0; i < _contents.Count; i++)
                {
                    if (i > 0) sb.AppendLine();
                    sb.Append(_contents[i] != null ? _contents[i].itemName : "Unknown");
                }
                Debug.Log($"[WorldContainer] {gameObject.name} contents: {sb}");
            }
        }

        private void OpenLootPanel()
        {
            if (lootBodyText != null)
            {
                if (_contents.Count == 0) lootBodyText.text = "Nothing of interest here.";
                else
                {
                    var sb = new StringBuilder(128);
                    for (int i = 0; i < _contents.Count; i++)
                    {
                        if (i > 0) sb.AppendLine();
                        ItemData d = _contents[i];
                        if (d == null) continue;
                        string line = d.itemName;
                        if (d.rarity != ItemRarity.Common) line += $"  ({d.rarity})";
                        sb.Append(line);
                    }
                    lootBodyText.text = sb.ToString();
                }
            }
            if (lootPanel != null) lootPanel.SetActive(true);
        }

        private void CloseLootPanel()
        {
            if (lootPanel != null) lootPanel.SetActive(false);
        }
    }
}

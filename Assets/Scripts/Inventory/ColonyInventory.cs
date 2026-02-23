using System;
using System.Collections.Generic;
using UnityEngine;

namespace Outborn.Inventory
{
    /// <summary>
    /// Zentrales Inventar der Kolonie. Singleton, eine Instanz pro Szene.
    /// Ressourcen hinzufügen/entfernen; UI kann sich per OnInventoryChanged aktualisieren.
    /// </summary>
    public class ColonyInventory : MonoBehaviour
    {
        public static ColonyInventory Instance { get; private set; }

        /// <summary> Wird ausgelöst, wenn sich Mengen geändert haben. </summary>
        public event Action OnInventoryChanged;

        [Header("Start-Ressourcen (optional)")]
        [SerializeField] private int startWood = 50;
        [SerializeField] private int startStone = 30;
        [SerializeField] private int startSteel = 0;
        [SerializeField] private int startFood = 20;

        private readonly Dictionary<ResourceType, int> _stacks = new Dictionary<ResourceType, int>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _stacks[ResourceType.Wood] = startWood;
            _stacks[ResourceType.Stone] = startStone;
            _stacks[ResourceType.Steel] = startSteel;
            _stacks[ResourceType.Food] = startFood;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public int GetCount(ResourceType type)
        {
            return _stacks.TryGetValue(type, out int count) ? count : 0;
        }

        public void Add(ResourceType type, int amount)
        {
            if (amount <= 0) return;
            if (!_stacks.ContainsKey(type)) _stacks[type] = 0;
            _stacks[type] += amount;
            OnInventoryChanged?.Invoke();
        }

        /// <summary> Entfernt amount; gibt zurück, wie viel tatsächlich entfernt wurde. </summary>
        public int Remove(ResourceType type, int amount)
        {
            if (amount <= 0) return 0;
            int current = GetCount(type);
            int removed = Mathf.Min(current, amount);
            _stacks[type] = current - removed;
            if (removed > 0)
                OnInventoryChanged?.Invoke();
            return removed;
        }

        public bool HasAtLeast(ResourceType type, int amount)
        {
            return GetCount(type) >= amount;
        }

        /// <summary> Für UI: alle Typen durchgehen (z.B. für Anzeige). </summary>
        public IReadOnlyDictionary<ResourceType, int> GetAll()
        {
            return _stacks;
        }
    }
}

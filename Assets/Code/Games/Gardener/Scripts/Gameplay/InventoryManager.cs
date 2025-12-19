using System;
using System.Collections.Generic;
using System.Linq;
using Disposable;
using GameShorts.Gardener.Data;
using R3;
using UnityEngine;

namespace GameShorts.Gardener.Gameplay
{
    /// <summary>
    /// Manages the player's seed inventory with quantities
    /// </summary>
    internal class InventoryManager : DisposableBase
    {
        private readonly Dictionary<PlantSettings, ReactiveProperty<int>> _seedInventory 
            = new Dictionary<PlantSettings, ReactiveProperty<int>>();
        
        // Observable that notifies when inventory changes (for UI updates)
        private readonly ReactiveProperty<Unit> _inventoryChanged = new ReactiveProperty<Unit>(Unit.Default);
        public ReadOnlyReactiveProperty<Unit> InventoryChanged => _inventoryChanged;

        /// <summary>
        /// Adds seeds to the inventory
        /// </summary>
        public void AddSeeds(PlantSettings plant, int count = 1)
        {
            if (plant == null)
            {
                Debug.LogWarning("Cannot add null plant to inventory");
                return;
            }

            if (count <= 0)
            {
                Debug.LogWarning($"Cannot add {count} seeds (must be positive)");
                return;
            }

            if (_seedInventory.ContainsKey(plant))
            {
                _seedInventory[plant].Value += count;
            }
            else
            {
                _seedInventory[plant] = new ReactiveProperty<int>(count);
            }

            Debug.Log($"Added {count}x {plant.PlantName} to inventory. Total: {_seedInventory[plant].Value}");
            _inventoryChanged.Value = Unit.Default;
        }

        /// <summary>
        /// Removes seeds from the inventory
        /// </summary>
        public bool RemoveSeeds(PlantSettings plant, int count = 1)
        {
            if (plant == null || !_seedInventory.ContainsKey(plant))
            {
                Debug.LogWarning($"Cannot remove seeds - plant not in inventory");
                return false;
            }

            if (_seedInventory[plant].Value < count)
            {
                Debug.LogWarning($"Not enough seeds. Have: {_seedInventory[plant].Value}, Need: {count}");
                return false;
            }

            _seedInventory[plant].Value -= count;
            
            // Remove from dictionary if count reaches 0
            if (_seedInventory[plant].Value <= 0)
            {
                _seedInventory[plant].Dispose();
                _seedInventory.Remove(plant);
                Debug.Log($"Removed {plant.PlantName} from inventory (count reached 0)");
            }
            else
            {
                Debug.Log($"Removed {count}x {plant.PlantName}. Remaining: {_seedInventory[plant].Value}");
            }

            _inventoryChanged.Value = Unit.Default;
            return true;
        }

        /// <summary>
        /// Gets the count of a specific seed type
        /// </summary>
        public int GetSeedCount(PlantSettings plant)
        {
            if (plant == null || !_seedInventory.ContainsKey(plant))
                return 0;

            return _seedInventory[plant].Value;
        }

        /// <summary>
        /// Checks if player has at least one seed of this type
        /// </summary>
        public bool HasSeeds(PlantSettings plant)
        {
            return GetSeedCount(plant) > 0;
        }

        /// <summary>
        /// Gets all available seeds with their counts
        /// </summary>
        public Dictionary<PlantSettings, int> GetAvailableSeeds()
        {
            return _seedInventory
                .Where(kvp => kvp.Value.Value > 0)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value);
        }

        /// <summary>
        /// Gets observable for a specific plant's count
        /// </summary>
        public ReadOnlyReactiveProperty<int> GetSeedCountObservable(PlantSettings plant)
        {
            if (_seedInventory.ContainsKey(plant))
            {
                return _seedInventory[plant];
            }

            // Return a static property with 0 if not in inventory
            return new ReactiveProperty<int>(0);
        }

        protected override void OnDispose()
        {
            foreach (var reactive in _seedInventory.Values)
            {
                reactive.Dispose();
            }
            _seedInventory.Clear();
            _inventoryChanged.Dispose();
        }
    }
}



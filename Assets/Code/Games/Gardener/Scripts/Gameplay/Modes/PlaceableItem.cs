using System;
using GameShorts.Gardener.Data;
using UnityEngine;

namespace GameShorts.Gardener.Gameplay.Modes
{
    /// <summary>
    /// Описывает элемент, который можно разместить на сцене через drag-and-drop
    /// </summary>
    [Serializable]
    public class PlaceableItem
    {
        [SerializeField] private string _itemName;
        [SerializeField] private Sprite _icon;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _count;
        [SerializeField] private PlantSettings _plantSettings;
        
        public string ItemName
        {
            get => _itemName;
            set => _itemName = value;
        }
        
        public Sprite Icon
        {
            get => _icon;
            set => _icon = value;
        }
        
        public GameObject Prefab
        {
            get => _prefab;
            set => _prefab = value;
        }
        
        public int Count
        {
            get => _count;
            set => _count = value;
        }
        
        public PlantSettings PlantSettings
        {
            get => _plantSettings;
            set => _plantSettings = value;
        }
        
        public PlaceableItem()
        {
            _count = 1;
        }
        
        public PlaceableItem(string itemName, Sprite icon, GameObject prefab, int count = 1, PlantSettings plantSettings = null)
        {
            _itemName = itemName;
            _icon = icon;
            _prefab = prefab;
            _count = count;
            _plantSettings = plantSettings;
        }
    }
}


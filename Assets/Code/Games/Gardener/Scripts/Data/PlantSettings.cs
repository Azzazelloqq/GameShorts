using UnityEngine;

namespace GameShorts.Gardener.Data
{
    /// <summary>
    /// Настройки для растения
    /// </summary>
    [CreateAssetMenu(fileName = "PlantSettings", menuName = "Gardener/Plant Settings")]
    public class PlantSettings : ScriptableObject
    {
        [Header("Основные настройки")]
        [SerializeField] private string _plantName;
        [SerializeField] private float _growthTime = 60f; // Время роста в секундах
        [SerializeField] private float _wateringInterval = 30f; // Интервал полива в секундах
        [SerializeField] private bool _hasFruits; // Имеет ли растение плоды
        
        [Header("Экономика")]
        [SerializeField] private int _seedPrice = 10;
        [SerializeField] private int _harvestPrice = 50;
        
        [Header("UI")]
        [SerializeField] private Sprite _seedIcon;
        
        [Header("Модели стадий роста")]
        [SerializeField] private GameObject _seedModel;
        [SerializeField] private GameObject _sproutModel;
        [SerializeField] private GameObject _bushModel;
        [SerializeField] private GameObject _floweringModel;
        [SerializeField] private GameObject _fruitModel;
        
        // Свойства
        public string PlantName => _plantName;
        public float GrowthTime => _growthTime;
        public float WateringInterval => _wateringInterval;
        public bool HasFruits => _hasFruits;
        public int SeedPrice => _seedPrice;
        public int HarvestPrice => _harvestPrice;
        public Sprite SeedIcon => _seedIcon;
        public GameObject SeedModel => _seedModel;
        public GameObject SproutModel => _sproutModel;
        public GameObject BushModel => _bushModel;
        public GameObject FloweringModel => _floweringModel;
        public GameObject FruitModel => _fruitModel;
    }
}
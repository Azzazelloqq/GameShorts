using System.Collections.Generic;
using UnityEngine;

namespace GameShorts.FlyHumans.View
{
    /// <summary>
    /// View компонент для трафика внутри блока
    /// Добавляется на префаб WorldBlock
    /// Поддерживает машины и самолеты
    /// </summary>
    public class BlockTrafficView : MonoBehaviour
    {
        [Header("Ground Vehicles (Машины)")]
        [Tooltip("Список префабов машин для этого блока")]
        [SerializeField] private List<GameObject> _vehiclePrefabs = new List<GameObject>();
        
        [Tooltip("Список путей для машин на этом блоке")]
        [SerializeField] private List<TrafficPath> _vehiclePaths = new List<TrafficPath>();

        [Tooltip("Количество машин на этом блоке")]
        [Range(0, 20)]
        [SerializeField] private int _vehicleCount = 3;
        
        [Tooltip("Минимальная скорость машин")]
        [SerializeField] private float _vehicleMinSpeed = 30f;
        
        [Tooltip("Максимальная скорость машин")]
        [SerializeField] private float _vehicleMaxSpeed = 40f;
        
        [Tooltip("Задержка перед повторным запуском машины (в секундах)")]
        [SerializeField] private float _vehicleRestartDelay = 3f;

        [Header("Airplanes (Самолеты)")]
        [Tooltip("Список префабов самолетов для этого блока")]
        [SerializeField] private List<GameObject> _airplanePrefabs = new List<GameObject>();
        
        [Tooltip("Список путей для самолетов на этом блоке")]
        [SerializeField] private List<TrafficPath> _airplanePaths = new List<TrafficPath>();

        [Tooltip("Количество самолетов на этом блоке")]
        [Range(0, 10)]
        [SerializeField] private int _airplaneCount = 1;
        
        [Tooltip("Минимальная скорость самолетов")]
        [SerializeField] private float _airplaneMinSpeed = 50f;
        
        [Tooltip("Максимальная скорость самолетов")]
        [SerializeField] private float _airplaneMaxSpeed = 80f;
        
        [Tooltip("Частота спавна самолетов (секунд между спавнами)")]
        [SerializeField] private float _airplaneSpawnInterval = 5f;

        [Header("Containers")]
        [Tooltip("Контейнер для машин (необязательно, создастся автоматически)")]
        [SerializeField] private Transform _vehiclesContainer;
        
        [Tooltip("Контейнер для самолетов (необязательно, создастся автоматически)")]
        [SerializeField] private Transform _airplanesContainer;

        // Properties - Vehicles
        public List<GameObject> VehiclePrefabs => _vehiclePrefabs;
        public List<TrafficPath> VehiclePaths => _vehiclePaths;
        public int VehicleCount => _vehicleCount;
        public float VehicleMinSpeed => _vehicleMinSpeed;
        public float VehicleMaxSpeed => _vehicleMaxSpeed;
        public float VehicleRestartDelay => _vehicleRestartDelay;
        public Transform VehiclesContainer => _vehiclesContainer;

        // Properties - Airplanes
        public List<GameObject> AirplanePrefabs => _airplanePrefabs;
        public List<TrafficPath> AirplanePaths => _airplanePaths;
        public int AirplaneCount => _airplaneCount;
        public float AirplaneMinSpeed => _airplaneMinSpeed;
        public float AirplaneMaxSpeed => _airplaneMaxSpeed;
        public float AirplaneSpawnInterval => _airplaneSpawnInterval;
        public Transform AirplanesContainer => _airplanesContainer;

        /// <summary>
        /// Проверка, есть ли настройки для машин
        /// </summary>
        public bool HasVehicles => _vehiclePrefabs.Count > 0 && _vehiclePaths.Count > 0 && _vehicleCount > 0;

        /// <summary>
        /// Проверка, есть ли настройки для самолетов
        /// </summary>
        public bool HasAirplanes => _airplanePrefabs.Count > 0 && _airplanePaths.Count > 0 && _airplaneCount > 0;

        /// <summary>
        /// Проверка, есть ли любой трафик
        /// </summary>
        public bool HasTraffic => HasVehicles || HasAirplanes;

        private void Awake()
        {
            // Создаем контейнер для машин, если не задан
            if (_vehiclesContainer == null)
            {
                _vehiclesContainer = new GameObject("VehiclesContainer").transform;
                _vehiclesContainer.SetParent(transform);
                _vehiclesContainer.localPosition = Vector3.zero;
            }
            
            // Создаем контейнер для самолетов, если не задан
            if (_airplanesContainer == null)
            {
                _airplanesContainer = new GameObject("AirplanesContainer").transform;
                _airplanesContainer.SetParent(transform);
                _airplanesContainer.localPosition = Vector3.zero;
            }
        }

        private void OnValidate()
        {
            // Ограничиваем минимальное и максимальное значения скорости для машин
            if (_vehicleMinSpeed < 0.1f) _vehicleMinSpeed = 0.1f;
            if (_vehicleMaxSpeed < _vehicleMinSpeed) _vehicleMaxSpeed = _vehicleMinSpeed;
            
            // Ограничиваем минимальное и максимальное значения скорости для самолетов
            if (_airplaneMinSpeed < 0.1f) _airplaneMinSpeed = 0.1f;
            if (_airplaneMaxSpeed < _airplaneMinSpeed) _airplaneMaxSpeed = _airplaneMinSpeed;
            
            // Ограничиваем частоту спавна
            if (_airplaneSpawnInterval < 0.1f) _airplaneSpawnInterval = 0.1f;
        }
    }
}


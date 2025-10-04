using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FlyHumans
{
    /// <summary>
    /// Главный контроллер, управляющий всеми машинами на карте
    /// Машины выбирают случайные пути из списка
    /// </summary>
    public class TrafficController : MonoBehaviour
    {
        [Header("Vehicle Setup")]
        [Tooltip("Список префабов машин")]
        public List<GameObject> vehiclePrefabs = new List<GameObject>();
        
        [Tooltip("Список путей, по которым могут ездить машины")]
        public List<TrafficPath> paths = new List<TrafficPath>();

        [Header("Spawn Settings")]
        [Tooltip("Количество машин на сцене")]
        [Range(1, 50)]
        public int vehicleCount = 5;
        
        [Tooltip("Минимальная скорость машин")]
        public float minSpeed = 3f;
        
        [Tooltip("Максимальная скорость машин")]
        public float maxSpeed = 7f;
        
        [Tooltip("Задержка перед повторным запуском машины (в секундах)")]
        public float restartDelay = 3f;

        [Header("Global Settings")]
        [Tooltip("Автоматически запустить все машины при старте")]
        public bool autoStartOnAwake = true;

        private List<GameObject> spawnedVehicles = new List<GameObject>();

        private void Start()
        {
            if (autoStartOnAwake)
            {
                InitializeAllVehicles();
            }
        }

        /// <summary>
        /// Инициализировать и запустить все машины
        /// </summary>
        public void InitializeAllVehicles()
        {
            if (vehiclePrefabs.Count == 0)
            {
                Debug.LogWarning("TrafficController: список префабов машин пуст!");
                return;
            }

            if (paths.Count == 0)
            {
                Debug.LogWarning("TrafficController: список путей пуст!");
                return;
            }

            // Очищаем старые машины
            ClearAllVehicles();

            // Создаем нужное количество машин
            for (int i = 0; i < vehicleCount; i++)
            {
                SpawnRandomVehicle();
            }
        }

        private void SpawnRandomVehicle()
        {
            // Выбираем случайный префаб
            GameObject randomPrefab = vehiclePrefabs[Random.Range(0, vehiclePrefabs.Count)];
            
            // Выбираем случайный путь
            TrafficPath randomPath = paths[Random.Range(0, paths.Count)];
            
            // Создаем машину
            Vector3 startPosition = randomPath.GetWaypointPosition(0);
            GameObject vehicle = Instantiate(randomPrefab, startPosition, Quaternion.identity, transform);
            spawnedVehicles.Add(vehicle);
            
            // Настраиваем компонент VehicleMover
            VehicleMover mover = vehicle.GetComponent<VehicleMover>();
            if (mover == null)
            {
                mover = vehicle.AddComponent<VehicleMover>();
            }
            
            mover.path = randomPath;
            mover.speed = Random.Range(minSpeed, maxSpeed);
            mover.StartMoving();

            // Запускаем мониторинг завершения пути
            StartCoroutine(MonitorVehicle(vehicle, mover));
        }

        private IEnumerator MonitorVehicle(GameObject vehicle, VehicleMover mover)
        {
            while (vehicle != null)
            {
                // Ждем, пока машина не остановится (достигнет конца пути)
                if (!mover.IsMoving())
                {
                    // Ждем заданное время
                    yield return new WaitForSeconds(restartDelay);
                    
                    // Выбираем новый случайный путь и перезапускаем
                    if (vehicle != null && mover != null && paths.Count > 0)
                    {
                        TrafficPath newPath = paths[Random.Range(0, paths.Count)];
                        mover.path = newPath;
                        mover.speed = Random.Range(minSpeed, maxSpeed);
                        mover.StartMoving();
                    }
                }
                
                yield return null;
            }
        }

        /// <summary>
        /// Остановить все машины
        /// </summary>
        public void StopAllVehicles()
        {
            foreach (var vehicle in spawnedVehicles)
            {
                if (vehicle != null)
                {
                    VehicleMover mover = vehicle.GetComponent<VehicleMover>();
                    if (mover != null)
                    {
                        mover.StopMoving();
                    }
                }
            }

            StopAllCoroutines();
        }

        /// <summary>
        /// Очистить все созданные машины
        /// </summary>
        public void ClearAllVehicles()
        {
            StopAllCoroutines();
            
            foreach (var vehicle in spawnedVehicles)
            {
                if (vehicle != null)
                {
                    Destroy(vehicle);
                }
            }
            
            spawnedVehicles.Clear();
        }

        private void OnDestroy()
        {
            ClearAllVehicles();
        }

        private void OnValidate()
        {
            // Ограничиваем минимальное и максимальное значения скорости
            if (minSpeed < 0.1f) minSpeed = 0.1f;
            if (maxSpeed < minSpeed) maxSpeed = minSpeed;
        }
    }
}


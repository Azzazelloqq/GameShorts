using System.Collections.Generic;
using Disposable;
using Code.Core.Tools.Pool;
using GameShorts.FlyHumans.Logic;
using GameShorts.FlyHumans.View;
using LightDI.Runtime;
using UnityEngine;

namespace GameShorts.FlyHumans.Presenters
{
    /// <summary>
    /// Презентер для управления трафиком внутри одного блока
    /// Поддерживает машины и самолеты
    /// </summary>
    internal class BlockTrafficPm : DisposableBase
    {
        internal struct Ctx
        {
            public BlockTrafficView trafficView;
        }

        private readonly Ctx _ctx;
        private readonly IPoolManager _poolManager;
        
        // Словарь для хранения активных машин: ключ - префаб, значение - список объектов этого префаба
        private readonly Dictionary<GameObject, List<VehicleModel>> _activeVehicles = new Dictionary<GameObject, List<VehicleModel>>();
        
        // Словарь для хранения активных самолетов: ключ - префаб, значение - список объектов этого префаба
        private readonly Dictionary<GameObject, List<VehicleModel>> _activeAirplanes = new Dictionary<GameObject, List<VehicleModel>>();
        
        private float _airplaneSpawnTimer;
        private int _spawnedAirplanesCount;
        private bool _isRunning;

        public BlockTrafficPm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
        }

        /// <summary>
        /// Запустить трафик на блоке
        /// </summary>
        public void StartTraffic()
        {
            if (_ctx.trafficView == null || !_ctx.trafficView.HasTraffic)
            {
                return;
            }

            // Создаем машины (все сразу)
            if (_ctx.trafficView.HasVehicles)
            {
                for (int i = 0; i < _ctx.trafficView.VehicleCount; i++)
                {
                    SpawnRandomVehicle();
                }
            }

            // Инициализируем систему спавна самолетов
            if (_ctx.trafficView.HasAirplanes)
            {
                _airplaneSpawnTimer = 0f;
                _spawnedAirplanesCount = 0;
            }

            _isRunning = true;
        }

        /// <summary>
        /// Остановить трафик
        /// </summary>
        public void StopTraffic()
        {
            _isRunning = false;
            
            foreach (var vehicleList in _activeVehicles.Values)
            {
                foreach (var vehicle in vehicleList)
                {
                    vehicle.Stop();
                }
            }
            
            foreach (var airplaneList in _activeAirplanes.Values)
            {
                foreach (var airplane in airplaneList)
                {
                    airplane.Stop();
                }
            }
        }

        /// <summary>
        /// Обновление трафика (вызывается из презентера блоков)
        /// </summary>
        public void UpdateTraffic(float deltaTime)
        {
            if (!_isRunning) return;

            // Обновляем машины
            UpdateVehicles(deltaTime);
            
            // Обновляем самолеты и их спавн
            UpdateAirplanes(deltaTime);
        }

        private void UpdateVehicles(float deltaTime)
        {
            foreach (var vehicleList in _activeVehicles.Values)
            {
                for (int i = vehicleList.Count - 1; i >= 0; i--)
                {
                    var vehicle = vehicleList[i];
                    
                    // Обновляем движение
                    vehicle.Update(deltaTime);

                    // Если машина только что остановилась, устанавливаем задержку
                    if (vehicle.JustStopped)
                    {
                        vehicle.SetRestartDelay(_ctx.trafficView.VehicleRestartDelay);
                    }
                    
                    // Проверяем, нужно ли перезапустить машину на новом пути
                    if (vehicle.NeedsRestart)
                    {
                        RestartVehicleOnRandomPath(vehicle);
                    }
                }
            }
        }

        private void UpdateAirplanes(float deltaTime)
        {
            if (!_ctx.trafficView.HasAirplanes) return;

            // Обновляем таймер спавна самолетов
            _airplaneSpawnTimer += deltaTime;

            // Проверяем, нужно ли заспавнить новый самолет
            if (_spawnedAirplanesCount < _ctx.trafficView.AirplaneCount && 
                _airplaneSpawnTimer >= _ctx.trafficView.AirplaneSpawnInterval)
            {
                SpawnRandomAirplane();
                _airplaneSpawnTimer = 0f;
            }

            // Обновляем существующие самолеты
            foreach (var kvp in _activeAirplanes)
            {
                var prefab = kvp.Key;
                var airplaneList = kvp.Value;
                
                for (int i = airplaneList.Count - 1; i >= 0; i--)
                {
                    var airplane = airplaneList[i];
                    
                    // Обновляем движение
                    airplane.Update(deltaTime);

                    // Когда самолет завершает путь, удаляем его и спавним новый через интервал
                    if (airplane.JustStopped)
                    {
                        airplaneList.RemoveAt(i);
                        
                        if (airplane.GameObject != null)
                        {
                            _poolManager.Return(airplane.GameObject, airplane.GameObject);
                        }
                        
                        _spawnedAirplanesCount--;
                    }
                }
            }
        }

        private void SpawnRandomVehicle()
        {
            // Выбираем случайный префаб машины
            GameObject randomPrefab = _ctx.trafficView.VehiclePrefabs[
                Random.Range(0, _ctx.trafficView.VehiclePrefabs.Count)
            ];

            // Выбираем случайный путь для машин
            TrafficPath randomPath = _ctx.trafficView.VehiclePaths[
                Random.Range(0, _ctx.trafficView.VehiclePaths.Count)
            ];


            var startRotate = randomPath.Waypoints[1].transform.position
                              - randomPath.Waypoints[0].transform.position;
            var rotateQuat = Quaternion.LookRotation(startRotate, Vector3.up);

            // Получаем машину из пула
            GameObject vehicleObj = _poolManager.Get(randomPrefab, randomPath.Waypoints[0].transform.position,
                _ctx.trafficView.VehiclesContainer, rotateQuat);
            // Получаем или добавляем VehicleView
            VehicleView vehicleView = vehicleObj.GetComponent<VehicleView>();
            if (vehicleView == null)
            {
                vehicleView = vehicleObj.AddComponent<VehicleView>();
            }

            // Создаем модель и инициализируем
            VehicleModel vehicleModel = new VehicleModel(vehicleView);
            float speed = Random.Range(_ctx.trafficView.VehicleMinSpeed, _ctx.trafficView.VehicleMaxSpeed);
            vehicleModel.Initialize(randomPath, speed);

            // Добавляем в словарь по ключу префаба
            if (!_activeVehicles.ContainsKey(randomPrefab))
            {
                _activeVehicles[randomPrefab] = new List<VehicleModel>();
            }
            _activeVehicles[randomPrefab].Add(vehicleModel);
        }

        private void SpawnRandomAirplane()
        {
            // Выбираем случайный префаб самолета
            GameObject randomPrefab = _ctx.trafficView.AirplanePrefabs[
                Random.Range(0, _ctx.trafficView.AirplanePrefabs.Count)
            ];

            // Выбираем случайный путь для самолетов
            TrafficPath randomPath = _ctx.trafficView.AirplanePaths[
                Random.Range(0, _ctx.trafficView.AirplanePaths.Count)
            ];

            var startRotate = randomPath.Waypoints[1].transform.position
                              - randomPath.Waypoints[0].transform.position;
            
            startRotate.Normalize();
            var rotateQuat = Quaternion.LookRotation(startRotate, Vector3.up);

            // Получаем самолет из пула
            GameObject airplaneObj = _poolManager.Get(randomPrefab, randomPath.Waypoints[0].transform.position,
                _ctx.trafficView.AirplanesContainer, rotateQuat);
            
            // Получаем или добавляем VehicleView
            VehicleView airplaneView = airplaneObj.GetComponent<VehicleView>();
            if (airplaneView == null)
            {
                airplaneView = airplaneObj.AddComponent<VehicleView>();
            }

            // Создаем модель и инициализируем
            VehicleModel airplaneModel = new VehicleModel(airplaneView);
            float speed = Random.Range(_ctx.trafficView.AirplaneMinSpeed, _ctx.trafficView.AirplaneMaxSpeed);
            airplaneModel.Initialize(randomPath, speed);

            // Добавляем в словарь по ключу префаба
            if (!_activeAirplanes.ContainsKey(randomPrefab))
            {
                _activeAirplanes[randomPrefab] = new List<VehicleModel>();
            }
            _activeAirplanes[randomPrefab].Add(airplaneModel);
            _spawnedAirplanesCount++;
        }

        private void RestartVehicleOnRandomPath(VehicleModel vehicle)
        {
            // Выбираем новый случайный путь для машин
            TrafficPath newPath = _ctx.trafficView.VehiclePaths[
                Random.Range(0, _ctx.trafficView.VehiclePaths.Count)
            ];

            // Выбираем новую скорость
            float newSpeed = Random.Range(_ctx.trafficView.VehicleMinSpeed, _ctx.trafficView.VehicleMaxSpeed);

            // Перезапускаем
            vehicle.Restart(newPath, newSpeed);
        }

        /// <summary>
        /// Очистить весь трафик (машины и самолеты)
        /// </summary>
        private void ClearAllVehicles()
        {
            // Очищаем машины
            foreach (var vehicleList in _activeVehicles.Values)
            {
                foreach (var vehicle in vehicleList)
                {
                    if (vehicle.GameObject != null)
                    {
                        _poolManager.Return(vehicle.GameObject, vehicle.GameObject);
                    }
                }
            }
            _activeVehicles.Clear();

            // Очищаем самолеты
            foreach (var airplaneList in _activeAirplanes.Values)
            {
                foreach (var airplane in airplaneList)
                {
                    if (airplane.GameObject != null)
                    {
                        _poolManager.Return(airplane.GameObject, airplane.GameObject);
                    }
                }
            }
            _activeAirplanes.Clear();
            _spawnedAirplanesCount = 0;
        }

        protected override void OnDispose()
        {
            StopTraffic();
            ClearAllVehicles();
            base.OnDispose();
        }
    }
}


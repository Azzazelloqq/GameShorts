using GameShorts.FlyHumans.View;
using UnityEngine;

namespace GameShorts.FlyHumans.Logic
{
    /// <summary>
    /// Модель для одной машины - содержит состояние и логику движения
    /// </summary>
    internal class VehicleModel
    {
        private readonly VehicleView _view;
        private TrafficPath _currentPath;
        private int _currentWaypointIndex;
        private float _speed;
        private bool _isMoving;
        private float _restartTimer;
        private bool _hasRestartDelay;

        public bool IsMoving => _isMoving;
        public bool NeedsRestart => !_isMoving && _restartTimer <= 0f;
        public bool JustStopped => !_isMoving && !_hasRestartDelay;
        public GameObject GameObject => _view.gameObject;

        public VehicleModel(VehicleView view)
        {
            _view = view;
        }

        /// <summary>
        /// Инициализировать машину с путем и скоростью
        /// </summary>
        public void Initialize(TrafficPath path, float speed)
        {
            _currentPath = path;
            _speed = speed;
            _currentWaypointIndex = 0;
            _isMoving = true;
            _restartTimer = 0f;
            _hasRestartDelay = false;

            // Устанавливаем машину в начальную позицию
            if (_currentPath != null && _currentPath.WaypointCount > 0)
            {
                _view.SetPosition(_currentPath.GetWaypointPosition(0));
            }
        }

        /// <summary>
        /// Обновление движения машины
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isMoving)
            {
                // Обновляем таймер рестарта
                if (_restartTimer > 0f)
                {
                    _restartTimer -= deltaTime;
                }
                return;
            }

            if (_currentPath == null || _currentPath.WaypointCount == 0)
            {
                Stop();
                return;
            }

            MoveTowardsCurrentWaypoint(deltaTime);
        }

        private void MoveTowardsCurrentWaypoint(float deltaTime)
        {
            Vector3 currentPosition = _view.transform.position;
            Vector3 targetPosition = _currentPath.GetWaypointPosition(_currentWaypointIndex);
            Vector3 direction = (targetPosition - currentPosition).normalized;

            // Движение к цели
            Vector3 newPosition = currentPosition + direction * _speed * deltaTime;
            _view.SetPosition(newPosition);

            // Поворот в сторону движения
            _view.SmoothRotate(direction, deltaTime);

            // Проверка достижения точки
            float distance = Vector3.Distance(newPosition, targetPosition);
            if (distance < _view.WaypointThreshold)
            {
                _currentWaypointIndex++;

                // Если достигли конца пути
                if (_currentWaypointIndex >= _currentPath.WaypointCount)
                {
                    OnPathCompleted();
                }
            }
        }

        private void OnPathCompleted()
        {
            _isMoving = false;
            _currentWaypointIndex = 0;
            _hasRestartDelay = false;
            // Таймер будет установлен извне через презентер
        }

        /// <summary>
        /// Остановить машину и начать отсчет до рестарта
        /// </summary>
        public void Stop(float restartDelay = 0f)
        {
            _isMoving = false;
            _currentWaypointIndex = 0;
            _restartTimer = restartDelay;
            _hasRestartDelay = true;
        }

        /// <summary>
        /// Установить задержку рестарта
        /// </summary>
        public void SetRestartDelay(float delay)
        {
            _restartTimer = delay;
            _hasRestartDelay = true;
        }

        /// <summary>
        /// Перезапустить машину на новом пути
        /// </summary>
        public void Restart(TrafficPath newPath, float newSpeed)
        {
            Initialize(newPath, newSpeed);
        }
    }
}


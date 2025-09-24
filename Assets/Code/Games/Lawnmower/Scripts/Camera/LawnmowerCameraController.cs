using UnityEngine;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Player;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Level;
using TickHandler;
using LightDI.Runtime;
using R3;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Camera
{
    internal class LawnmowerCameraController : BaseDisposable
    {
        public struct Ctx
        {
            public UnityEngine.Camera camera;
            public LawnmowerPlayerPm playerPm;
            public LawnmowerLevelManager levelManager;
            public LawnmowerCameraSettings settings;
        }

        private readonly Ctx _ctx;
        private readonly ITickHandler _tickHandler;
        
        private Vector3 _currentVelocity; // Для SmoothDamp
        private Vector3 _lookAheadOffset; // Текущий офсет "взгляда вперед"
        private Vector3 _targetPosition;

        public LawnmowerCameraController(Ctx ctx, [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            
            InitializeCamera();
            StartFollowing();
        }

        private void InitializeCamera()
        {
            if (_ctx.camera == null)
            {
                Debug.LogError("LawnmowerCameraController: Camera is null!");
                return;
            }

            // Устанавливаем начальные параметры камеры
            if (_ctx.settings != null)
            {
                _ctx.camera.orthographicSize = _ctx.settings.DefaultOrthographicSize;
                
                if (_ctx.settings.AdaptiveZoom)
                {
                    AdaptZoomToLevel();
                }
            }
            
            // Устанавливаем начальную позицию камеры на игрока
            if (_ctx.playerPm != null)
            {
                Vector3 playerPos = _ctx.playerPm.GetPlayerPosition();
                Vector3 initialPos = playerPos + _ctx.settings.Offset;
                _ctx.camera.transform.position = initialPos;
                _targetPosition = initialPos;
            }
        }

        private void StartFollowing()
        {
            if (_ctx.playerPm == null) return;

            // Подписываемся на обновления позиции игрока
            var playerModel = _ctx.playerPm.GetPlayerModel();
            if (playerModel != null)
            {
                AddDispose(playerModel.Position.Subscribe(OnPlayerPositionChanged));
                AddDispose(playerModel.MovementDirection.Subscribe(OnPlayerDirectionChanged));
            }

            // Подписываемся на обновления камеры
            _tickHandler.FrameUpdate += UpdateCamera;
        }

        private void OnPlayerPositionChanged(Vector2 newPlayerPosition)
        {
            UpdateTargetPosition(newPlayerPosition);
        }

        private void OnPlayerDirectionChanged(Vector2 movementDirection)
        {
            if (_ctx.settings?.EnableLookAhead == true)
            {
                UpdateLookAheadOffset(movementDirection);
            }
        }

        private void UpdateTargetPosition(Vector2 playerPosition)
        {
            Vector3 baseTargetPosition = new Vector3(playerPosition.x, playerPosition.y, 0f) + _ctx.settings.Offset;
            
            // Добавляем "взгляд вперед"
            if (_ctx.settings.EnableLookAhead)
            {
                baseTargetPosition += _lookAheadOffset;
            }
            
            // Добавляем динамическое смещение
            if (_ctx.settings.DynamicOffset)
            {
                var playerModel = _ctx.playerPm.GetPlayerModel();
                if (playerModel != null)
                {
                    Vector2 direction = playerModel.MovementDirection.Value;
                    Vector3 dynamicOffset = new Vector3(direction.x, direction.y, 0f) * _ctx.settings.DynamicOffsetStrength;
                    baseTargetPosition += dynamicOffset;
                }
            }
            
            // Ограничиваем границами уровня
            if (_ctx.settings.UseLevelBounds)
            {
                baseTargetPosition = ClampToBounds(baseTargetPosition);
            }
            
            _targetPosition = baseTargetPosition;
        }

        private void UpdateLookAheadOffset(Vector2 movementDirection)
        {
            Vector3 targetLookAhead = Vector3.zero;
            
            if (movementDirection.magnitude > 0.1f)
            {
                targetLookAhead = new Vector3(movementDirection.x, movementDirection.y, 0f) * _ctx.settings.LookAheadDistance;
            }
            
            // Плавно обновляем офсет взгляда вперед
            _lookAheadOffset = Vector3.Lerp(_lookAheadOffset, targetLookAhead, _ctx.settings.LookAheadSpeed * Time.deltaTime);
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            var currentLevel = _ctx.levelManager?.GetCurrentLevel();
            if (currentLevel?.LevelBounds == null) return position;
            
            var bounds = currentLevel.LevelBounds.bounds;
            var cameraBounds = GetCameraBounds();
            
            // Учитываем размер камеры и отступы
            float minX = bounds.min.x + cameraBounds.x + _ctx.settings.BoundsPadding;
            float maxX = bounds.max.x - cameraBounds.x - _ctx.settings.BoundsPadding;
            float minY = bounds.min.y + cameraBounds.y + _ctx.settings.BoundsPadding;
            float maxY = bounds.max.y - cameraBounds.y - _ctx.settings.BoundsPadding;
            
            float clampedX = Mathf.Clamp(position.x, minX, maxX);
            float clampedY = Mathf.Clamp(position.y, float.MinValue, maxY);
            
            return new Vector3(clampedX, clampedY, position.z);
        }

        private Vector2 GetCameraBounds()
        {
            if (_ctx.camera == null) return Vector2.zero;
            
            float halfHeight = _ctx.camera.orthographicSize;
            float halfWidth = halfHeight * _ctx.camera.aspect;
            
            return new Vector2(halfWidth, halfHeight);
        }

        private void AdaptZoomToLevel()
        {
            var currentLevel = _ctx.levelManager?.GetCurrentLevel();
            if (currentLevel?.LevelBounds == null) return;
            
            var bounds = currentLevel.LevelBounds.bounds;
            
            // Вычисляем оптимальный зум для показа всего уровня
            float levelWidth = bounds.size.x;
            float levelHeight = bounds.size.y;
            
            float requiredHeight = levelHeight * 0.6f; // 60% от высоты уровня
            float requiredWidth = levelWidth * 0.6f / _ctx.camera.aspect;
            
            float optimalSize = Mathf.Max(requiredHeight, requiredWidth);
            optimalSize = Mathf.Clamp(optimalSize, _ctx.settings.MinZoom, _ctx.settings.MaxZoom);
            
            _ctx.camera.orthographicSize = optimalSize;
        }

        private void UpdateCamera(float deltaTime)
        {
            if (_ctx.camera == null || _ctx.settings == null) return;

            Vector3 currentPosition = _ctx.camera.transform.position;
            
            if (_ctx.settings.SmoothFollow)
            {
                // Плавное следование с использованием SmoothDamp
                Vector3 newPosition = Vector3.SmoothDamp(
                    currentPosition, 
                    _targetPosition, 
                    ref _currentVelocity, 
                    _ctx.settings.SmoothDamping
                );
                _ctx.camera.transform.position = newPosition;
            }
            else
            {
                // Линейное следование
                Vector3 newPosition = Vector3.Lerp(
                    currentPosition, 
                    _targetPosition, 
                    _ctx.settings.FollowSpeed * deltaTime
                );
                _ctx.camera.transform.position = newPosition;
            }
        }

        public void SetTarget(LawnmowerPlayerPm newPlayerPm)
        {
            // Обновляем цель камеры (полезно при смене игрока или уровня)
            if (_ctx.playerPm != newPlayerPm)
            {
                // Отписываемся от старого игрока и подписываемся на нового
                // (реализация зависит от архитектуры)
            }
        }

        public void SetCameraSettings(LawnmowerCameraSettings newSettings)
        {
            // Обновляем настройки камеры во время игры
            if (newSettings != null)
            {
                _ctx.camera.orthographicSize = newSettings.DefaultOrthographicSize;
                
                if (newSettings.AdaptiveZoom)
                {
                    AdaptZoomToLevel();
                }
            }
        }

        protected override void OnDispose()
        {
            _tickHandler.FrameUpdate -= UpdateCamera;
            base.OnDispose();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_ctx.camera == null || _ctx.settings == null) return;
            
            // Отрисовываем границы камеры
            Vector3 cameraPos = _ctx.camera.transform.position;
            Vector2 cameraBounds = GetCameraBounds();
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(cameraPos, new Vector3(cameraBounds.x * 2, cameraBounds.y * 2, 0.1f));
            
            // Отрисовываем целевую позицию
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_targetPosition, 0.5f);
            
            // Отрисовываем офсет взгляда вперед
            if (_ctx.settings.EnableLookAhead && _lookAheadOffset.magnitude > 0.1f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(cameraPos, cameraPos + _lookAheadOffset);
            }
        }
#endif
    }
}

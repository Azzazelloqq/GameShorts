using UnityEngine;
using Disposable;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Player;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Level;
using TickHandler;
using LightDI.Runtime;
using R3;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Camera
{
    internal class LawnmowerCameraController : DisposableBase
    {
        internal struct Ctx
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
        private bool _adaptiveZoomApplied;
        private Vector3 _lastPlayerWorldPosition;
        private bool _hasLastPlayerWorldPosition;
        private bool _initialClampApplied;

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

            if (_ctx.settings == null)
            {
                Debug.LogError("LawnmowerCameraController: Settings is null!");
                return;
            }

            // Устанавливаем начальные параметры камеры
            _ctx.camera.orthographicSize = _ctx.settings.DefaultOrthographicSize;
            _adaptiveZoomApplied = false;
            if (_ctx.settings.AdaptiveZoom && CanAdaptZoomToLevelNow())
            {
                AdaptZoomToLevel();
                _adaptiveZoomApplied = true;
            }
            
            // Ставим камеру сразу в корректную целевую позицию (с учетом bounds),
            // чтобы не ждать первого движения игрока/первого эмита позиции.
            SyncToPlayerImmediate();
        }

        private void StartFollowing()
        {
            if (_ctx.playerPm == null) return;

            // Подписываемся на обновления позиции игрока
            var playerModel = _ctx.playerPm.GetPlayerModel();
            if (playerModel != null)
            {
                AddDisposable(playerModel.MovementDirection.Subscribe(OnPlayerDirectionChanged));
            }

            // Подписываемся на обновления камеры
            _tickHandler.FrameUpdate += UpdateCamera;
        }

        private void OnPlayerDirectionChanged(Vector2 movementDirection)
        {
            if (_ctx.settings?.EnableLookAhead == true)
            {
                UpdateLookAheadOffset(movementDirection);
            }
        }

        private void UpdateTargetPosition(Vector2 playerPosition, bool forceClampToLevel = false)
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
            
            // Ограничиваем границами уровня.
            if (_ctx.settings.UseLevelBounds)
            {
                var currentLevel = _ctx.levelManager?.GetCurrentLevel();
                if (currentLevel != null && (forceClampToLevel || currentLevel.IsPositionInBounds(playerPosition)))
                {
                    baseTargetPosition = ClampToBounds(baseTargetPosition);
                }
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
            if (bounds.size.x < 0.1f || bounds.size.y < 0.1f)
            {
                return position;
            }
            var cameraBounds = GetCameraBounds();
            
            // Учитываем размер камеры и отступы
            float minX = bounds.min.x + cameraBounds.x + _ctx.settings.BoundsPadding;
            float maxX = bounds.max.x - cameraBounds.x - _ctx.settings.BoundsPadding;
            float minY = bounds.min.y + cameraBounds.y + _ctx.settings.BoundsPadding;
            float maxY = bounds.max.y - cameraBounds.y - _ctx.settings.BoundsPadding;
            
            float clampedX = Mathf.Clamp(position.x, minX, maxX);
            float clampedY = Mathf.Clamp(position.y, position.y, maxY);
            
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
            
            float requiredHeight = levelHeight * 0.7f; 
            float requiredWidth = levelWidth * 0.7f / _ctx.camera.aspect;
            
            float optimalSize = Mathf.Max(requiredHeight, requiredWidth);
            optimalSize = Mathf.Clamp(optimalSize, _ctx.settings.MinZoom, _ctx.settings.MaxZoom);
            
            _ctx.camera.orthographicSize = optimalSize;
        }

        private bool CanAdaptZoomToLevelNow()
        {
            var currentLevel = _ctx.levelManager?.GetCurrentLevel();
            return currentLevel?.LevelBounds != null;
        }

        private void SyncToPlayerImmediate()
        {
            if (_ctx.camera == null || _ctx.settings == null || _ctx.playerPm == null) return;

            if (TryGetCurrentPlayerWorldPosition(out Vector3 playerPos3))
            {
                UpdateTargetPosition(new Vector2(playerPos3.x, playerPos3.y), true);
                _initialClampApplied = CanClampToLevelNow();
            }
            else
            {
                return;
            }

            _ctx.camera.transform.position = _targetPosition;
            _currentVelocity = Vector3.zero;
        }

        private void UpdateCamera(float deltaTime)
        {
            if (_ctx.camera == null || _ctx.settings == null) return;

            // Важно: уровень может "стартовать" уже после создания камеры.
            // Если игрок стоит, Position может не эмититься — поэтому пересчитываем цель каждый кадр.
            if (_ctx.playerPm != null)
            {
                if (TryGetCurrentPlayerWorldPosition(out Vector3 playerPos3))
                {
                    UpdateTargetPosition(new Vector2(playerPos3.x, playerPos3.y));
                }
            }

            // Если adaptive zoom включен, но в момент инициализации bounds еще не было — применяем позже.
            if (_ctx.settings.AdaptiveZoom && !_adaptiveZoomApplied && CanAdaptZoomToLevelNow())
            {
                AdaptZoomToLevel();
                _adaptiveZoomApplied = true;
            }

            if (!_initialClampApplied && CanClampToLevelNow())
            {
                Vector3 playerPos3 = _lastPlayerWorldPosition;
                UpdateTargetPosition(new Vector2(playerPos3.x, playerPos3.y), true);
                _ctx.camera.transform.position = _targetPosition;
                _currentVelocity = Vector3.zero;
                _initialClampApplied = true;
            }

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

        private bool TryGetCurrentPlayerWorldPosition(out Vector3 playerPos3)
        {
            var playerView = _ctx.playerPm?.GetPlayerView();
            if (playerView != null)
            {
                playerPos3 = playerView.transform.position;
                _lastPlayerWorldPosition = playerPos3;
                _hasLastPlayerWorldPosition = true;
                return true;
            }

            if (_hasLastPlayerWorldPosition)
            {
                playerPos3 = _lastPlayerWorldPosition;
                return true;
            }

            playerPos3 = Vector3.zero;
            return false;
        }

        private bool CanClampToLevelNow()
        {
            if (!_ctx.settings.UseLevelBounds) return false;

            var currentLevel = _ctx.levelManager?.GetCurrentLevel();
            if (currentLevel?.LevelBounds == null) return false;

            var bounds = currentLevel.LevelBounds.bounds;
            return bounds.size.x >= 0.1f && bounds.size.y >= 0.1f;
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

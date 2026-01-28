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
        private Vector3 _targetPosition;
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

            // Ставим камеру сразу в корректную целевую позицию (с учетом bounds),
            // чтобы не ждать первого движения игрока/первого эмита позиции.
            SyncToPlayerImmediate();
        }

        private void StartFollowing()
        {
            if (_ctx.playerPm == null) return;
            
            // Подписываемся на обновления камеры
            _tickHandler.FrameLateUpdate += UpdateCamera;
        }

        private void UpdateTargetPosition(Vector2 playerPosition, bool forceClampToLevel = false)
        {
            Vector3 baseTargetPosition = new Vector3(playerPosition.x, playerPosition.y, 0f) + _ctx.settings.Offset;
            
            // Ограничиваем границами уровня (по новым правилам – через точки / грид).
            if ( (forceClampToLevel || CanClampToLevelNow()))
            {
                baseTargetPosition = ClampToBounds(baseTargetPosition);
            }
            
            _targetPosition = baseTargetPosition;
        }


        private Vector3 ClampToBounds(Vector3 position)
        {
            var currentLevel = _ctx.levelManager?.GetCurrentLevel();
            if (currentLevel == null) return position;

            if (!currentLevel.TryGetCameraBounds(out Vector2 levelMin, out Vector2 levelMax))
            {
                return position;
            }

            var cameraBounds = GetCameraBounds();

            // Учитываем размер камеры и отступы
            float minX = levelMin.x + cameraBounds.x ;
            float maxX = levelMax.x - cameraBounds.x ;
            float maxY = levelMax.y - cameraBounds.y ;
            
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

        protected override void OnDispose()
        {
            _tickHandler.FrameLateUpdate -= UpdateCamera;
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
            var currentLevel = _ctx.levelManager?.GetCurrentLevel();
            if (currentLevel == null) return false;

            if (!currentLevel.TryGetCameraBounds(out Vector2 levelMin, out Vector2 levelMax))
            {
                return false;
            }

            var size = levelMax - levelMin;
            return size.x >= 0.1f && size.y >= 0.1f;
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
        }
#endif
    }
}

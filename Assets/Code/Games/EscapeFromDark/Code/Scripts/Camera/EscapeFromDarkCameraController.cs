using UnityEngine;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Player;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level;
using TickHandler;
using LightDI.Runtime;
using R3;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Camera
{
    internal class EscapeFromDarkCameraController : BaseDisposable
    {
        public struct Ctx
        {
            public UnityEngine.Camera camera;
            public EscapeFromDarkPlayerPm playerPm;
            public EscapeFromDarkLevelPm levelPm;
        }

        private readonly Ctx _ctx;
        private readonly ITickHandler _tickHandler;
        
        private Vector3 _currentVelocity; // Для SmoothDamp
        private Vector3 _targetPosition;
        
        // Настройки камеры (упрощенные по сравнению с Lawnmower)
        private readonly float _followSpeed = 5f;
        private readonly bool _smoothFollow = true;
        private readonly float _smoothDamping = 0.3f;
        private readonly Vector3 _offset = new Vector3(0, 0, -10f);
        private readonly float _orthographicSize = 15f;

        public EscapeFromDarkCameraController(Ctx ctx, [Inject] ITickHandler tickHandler)
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
                Debug.LogError("EscapeFromDarkCameraController: Camera is null!");
                return;
            }

            // Устанавливаем параметры камеры
            _ctx.camera.orthographicSize = _orthographicSize;
            
            // Устанавливаем начальную позицию камеры на игрока
            if (_ctx.playerPm != null)
            {
                Vector3 playerPos = _ctx.playerPm.GetPlayerPosition();
                Vector3 initialPos = playerPos + _offset;
                _ctx.camera.transform.position = initialPos;
                _targetPosition = initialPos;
                
                Debug.Log($"EscapeFromDarkCameraController: Camera positioned at {initialPos}, player at {playerPos}");
            }
            else
            {
                Debug.LogWarning("EscapeFromDarkCameraController: PlayerPm is null during camera initialization!");
            }

            Debug.Log("EscapeFromDarkCameraController: Camera initialized");
        }

        private void StartFollowing()
        {
            if (_ctx.playerPm == null) 
            {
                Debug.LogError("EscapeFromDarkCameraController: PlayerPm is null in StartFollowing!");
                return;
            }

            // Подписываемся на обновления позиции игрока
            var playerModel = _ctx.playerPm.GetPlayerModel();
            if (playerModel != null)
            {
                AddDispose(playerModel.Position.Subscribe(OnPlayerPositionChanged));
                Debug.Log("EscapeFromDarkCameraController: Subscribed to player position changes");
            }
            else
            {
                Debug.LogError("EscapeFromDarkCameraController: PlayerModel is null!");
            }

            // Подписываемся на обновления камеры
            _tickHandler.FrameUpdate += UpdateCamera;
            
            Debug.Log("EscapeFromDarkCameraController: Started following player");
        }

        private void OnPlayerPositionChanged(Vector3 newPlayerPosition)
        {
            Debug.Log($"EscapeFromDarkCameraController: Player position changed to {newPlayerPosition}");
            UpdateTargetPosition(newPlayerPosition);
        }

        private void UpdateTargetPosition(Vector3 playerPosition)
        {
            Vector3 baseTargetPosition = playerPosition + _offset;
            
            Debug.Log($"EscapeFromDarkCameraController: Base target position {baseTargetPosition} (player: {playerPosition} + offset: {_offset})");
            
            // Временно отключаем ограничение границами для отладки
            // baseTargetPosition = ClampToMazeBounds(baseTargetPosition);
            
            _targetPosition = baseTargetPosition;
            
            Debug.Log($"EscapeFromDarkCameraController: Final target position {_targetPosition}");
        }

        private Vector3 ClampToMazeBounds(Vector3 position)
        {
            if (_ctx.levelPm?.LevelView == null) 
            {
                Debug.Log("EscapeFromDarkCameraController: LevelPm or LevelView is null, no bounds clamping");
                return position;
            }

            // Получаем размеры лабиринта
            int mazeSize = _ctx.levelPm.MazeSize;
            
            // Вычисляем границы лабиринта в мировых координатах
            Vector3 mazeMin = _ctx.levelPm.LevelView.GetWorldPosition(0, 0);
            Vector3 mazeMax = _ctx.levelPm.LevelView.GetWorldPosition(mazeSize - 1, mazeSize - 1);
            
            Debug.Log($"EscapeFromDarkCameraController: Maze bounds - Min: {mazeMin}, Max: {mazeMax}");
            
            // Получаем размеры камеры
            float halfHeight = _ctx.camera.orthographicSize;
            float halfWidth = halfHeight * _ctx.camera.aspect;
            
            // Добавляем небольшой отступ
            float padding = 1f;
            
            // Ограничиваем позицию камеры
            float clampedX = Mathf.Clamp(position.x, 
                mazeMin.x + halfWidth + padding, 
                mazeMax.x - halfWidth - padding);
            float clampedY = Mathf.Clamp(position.y, 
                mazeMin.y + halfHeight + padding, 
                mazeMax.y - halfHeight - padding);
            
            Vector3 clampedPosition = new Vector3(clampedX, clampedY, position.z);
            
            Debug.Log($"EscapeFromDarkCameraController: Clamping {position} -> {clampedPosition}");
            
            return clampedPosition;
        }

        private void UpdateCamera(float deltaTime)
        {
            if (_ctx.camera == null) return;

            Vector3 currentPosition = _ctx.camera.transform.position;
            float distanceToTarget = Vector3.Distance(currentPosition, _targetPosition);
            
            if (_smoothFollow)
            {
                // Плавное следование с использованием SmoothDamp
                Vector3 newPosition = Vector3.SmoothDamp(
                    currentPosition, 
                    _targetPosition, 
                    ref _currentVelocity, 
                    _smoothDamping
                );
                _ctx.camera.transform.position = newPosition;
            }
            else
            {
                // Линейное следование
                Vector3 newPosition = Vector3.Lerp(
                    currentPosition, 
                    _targetPosition, 
                    _followSpeed * deltaTime
                );
                _ctx.camera.transform.position = newPosition;
            }
        }

        public void FocusOnPlayer()
        {
            // Принудительно позиционируем камеру на игроке
            if (_ctx.playerPm != null && _ctx.camera != null)
            {
                Vector3 playerPos = _ctx.playerPm.GetPlayerPosition();
                Vector3 targetPos = playerPos + _offset;
                
                _ctx.camera.transform.position = targetPos;
                _targetPosition = targetPos;
                _currentVelocity = Vector3.zero; // Сбрасываем скорость для SmoothDamp
                
                Debug.Log($"EscapeFromDarkCameraController: Focused on player at {playerPos}, camera at {targetPos}");
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
            if (_ctx.camera == null) return;
            
            // Отрисовываем границы камеры
            Vector3 cameraPos = _ctx.camera.transform.position;
            float halfHeight = _ctx.camera.orthographicSize;
            float halfWidth = halfHeight * _ctx.camera.aspect;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(cameraPos, new Vector3(halfWidth * 2, halfHeight * 2, 0.1f));
            
            // Отрисовываем целевую позицию
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_targetPosition, 0.5f);
        }
#endif
    }
}

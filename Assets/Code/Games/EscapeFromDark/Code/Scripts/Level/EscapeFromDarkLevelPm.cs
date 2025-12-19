using System;
using System.Threading;
using Disposable;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View;
using UnityEngine;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level
{
    internal class EscapeFromDarkLevelPm : DisposableBase
    {
        internal struct Ctx
        {
            public EscapeFromDarkSceneContextView sceneContextView;
            public int levelNumber;
            public Action onLevelCompleted;
            public CancellationToken cancellationToken;
            public Transform playerTransform; // Для передачи в ExitSpot
        }

        private readonly Ctx _ctx;
        private readonly MazeGenerator _mazeGenerator;
        private EscapeFromDarkLevelView _levelView;
        private int[,] _currentMazeData;
        private int _mazeSize;

        public int[,] CurrentMazeData => _currentMazeData;
        public int MazeSize => _mazeSize;
        public EscapeFromDarkLevelView LevelView => _levelView;

        public EscapeFromDarkLevelPm(Ctx ctx)
        {
            _ctx = ctx;
            _mazeGenerator = new MazeGenerator();
            
            GenerateLevel();
        }

        private void GenerateLevel()
        {
            // Вычисляем размер лабиринта для текущего уровня
            _mazeSize = CalculateMazeSize(_ctx.levelNumber);
            
            Debug.Log($"EscapeFromDarkLevelPm: Generating level {_ctx.levelNumber} with size {_mazeSize}x{_mazeSize}");
            
            // Генерируем лабиринт
            _currentMazeData = _mazeGenerator.GetLabirint(_mazeSize, _mazeSize);
            
            // Создаем и инициализируем View
            CreateLevelView();
        }

        private void CreateLevelView()
        {
            _levelView = _ctx.sceneContextView.LevelView;
            
            if (_levelView == null)
            {
                Debug.LogError("EscapeFromDarkLevelPm: LevelView not found in scene! Please add EscapeFromDarkLevelView component to scene.");
                return;
            }
            
            // Инициализируем View с данными лабиринта
            var viewCtx = new EscapeFromDarkLevelView.Ctx
            {
                mazeData = _currentMazeData,
                mazeWidth = _mazeSize,
                mazeHeight = _mazeSize,
                playerTransform = _ctx.playerTransform
            };
            
            _levelView.SetCtx(viewCtx);
            
        }

        private int CalculateMazeSize(int levelNumber)
        {
            // Начинаем с 9x9, каждые 2 уровня увеличиваем размер на 2
            int baseSizeIncrease = ((levelNumber - 1) / 2) * 2;
            return 9 + baseSizeIncrease;
        }

        public Vector2Int GetStartPosition()
        {
            return _levelView?.GetStartPosition() ?? Vector2Int.zero;
        }

        public Vector2Int GetExitPosition()
        {
            return _levelView?.GetExitPosition() ?? new Vector2Int(_mazeSize - 1, _mazeSize / 2);
        }

        public Vector3 GetWorldPosition(int mazeX, int mazeY)
        {
            return _levelView?.GetWorldPosition(mazeX, mazeY) ?? Vector3.zero;
        }

        public Vector2Int GetMazePosition(Vector3 worldPosition)
        {
            return _levelView?.GetMazePosition(worldPosition) ?? Vector2Int.zero;
        }

        public bool IsValidMazePosition(int mazeX, int mazeY)
        {
            return _levelView?.IsValidMazePosition(mazeX, mazeY) ?? false;
        }

        public bool IsExitPosition(int mazeX, int mazeY)
        {
            Vector2Int exitPos = GetExitPosition();
            return mazeX == exitPos.x && mazeY == exitPos.y;
        }

        // Метод для получения информации о текущем уровне
        public string GetLevelInfo()
        {
            Vector2Int start = GetStartPosition();
            Vector2Int exit = GetExitPosition();
            
            return $"Level {_ctx.levelNumber}: Size {_mazeSize}x{_mazeSize}, Start: {start}, Exit: {exit}";
        }

        // Метод для обновления ссылки на игрока в ExitSpot
        public void UpdatePlayerReference(Transform playerTransform)
        {
            if (_levelView != null)
            {
                // Находим ExitSpotController и обновляем ссылку на игрока
                ExitSpotController exitController = _levelView.SpawnedExitSpot;
                if (exitController != null)
                {
                    exitController.SetPlayer(playerTransform);
                    Debug.Log("EscapeFromDarkLevelPm: Updated player reference in ExitSpot");
                }
            }
        }

        protected override void OnDispose()
        {
            // Очищаем ExitSpot перед уничтожением уровня
            if (_levelView?.SpawnedExitSpot != null)
            {
                _levelView.SpawnedExitSpot.DisableLight();
                UnityEngine.Object.DestroyImmediate(_levelView.SpawnedExitSpot.gameObject);
                Debug.Log("EscapeFromDarkLevelPm: ExitSpot cleaned up on dispose");
            }
        }
    }
}

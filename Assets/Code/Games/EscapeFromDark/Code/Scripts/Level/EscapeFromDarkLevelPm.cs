using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View;
using UnityEngine;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level
{
    internal class EscapeFromDarkLevelPm : BaseDisposable
    {
        internal struct Ctx
        {
            public EscapeFromDarkSceneContextView sceneContextView;
            public int levelNumber;
            public Action onLevelCompleted;
            public CancellationToken cancellationToken;
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
            // TODO: Загрузить префаб уровня или найти существующий View
            // Пока что ищем View в сцене или создаем программно
            _levelView = UnityEngine.Object.FindObjectOfType<EscapeFromDarkLevelView>();
            
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
                mazeHeight = _mazeSize
            };
            
            _levelView.SetCtx(viewCtx);
            
            Debug.Log($"EscapeFromDarkLevelPm: Level view initialized for level {_ctx.levelNumber}");
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

        public void OnPlayerReachedExit()
        {
            Debug.Log($"EscapeFromDarkLevelPm: Player reached exit on level {_ctx.levelNumber}!");
            _ctx.onLevelCompleted?.Invoke();
        }

        // Метод для перегенерации уровня (для отладки или рестарта)
        public void RegenerateLevel()
        {
            Debug.Log($"EscapeFromDarkLevelPm: Regenerating level {_ctx.levelNumber}");
            GenerateLevel();
        }

        // Метод для получения информации о текущем уровне
        public string GetLevelInfo()
        {
            Vector2Int start = GetStartPosition();
            Vector2Int exit = GetExitPosition();
            
            return $"Level {_ctx.levelNumber}: Size {_mazeSize}x{_mazeSize}, Start: {start}, Exit: {exit}";
        }

        protected override void OnDispose()
        {
            // Очистка ресурсов, если необходимо
            if (_levelView != null)
            {
                // View будет очищен автоматически Unity
            }
        }
    }
}

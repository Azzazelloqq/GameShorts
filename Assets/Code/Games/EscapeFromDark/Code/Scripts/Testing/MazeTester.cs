using UnityEngine;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Testing
{
    internal class MazeTester : MonoBehaviour
    {
        [Header("Maze Settings")]
        [SerializeField] private int mazeWidth = 9;
        [SerializeField] private int mazeHeight = 9;
        [SerializeField] private bool generateOnStart = true;
        
        private MazeGenerator _mazeGenerator;
        private int _currentLevel = 1;

        void Start()
        {
            _mazeGenerator = new MazeGenerator();
            
            if (generateOnStart)
            {
                GenerateAndDisplayMaze();
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                GenerateAndDisplayMaze();
            }
        }

        private void GenerateAndDisplayMaze()
        {
            // Увеличиваем размер лабиринта каждые 2 уровня
            (int x, int y) currentSize = CalculateMazeSize(_currentLevel);
            
            Debug.Log($"=== GENERATING MAZE - LEVEL {_currentLevel} - SIZE {currentSize}x{currentSize} ===");
            
            int[,] maze = _mazeGenerator.GetLabirint(currentSize.x, currentSize.y);
            DisplayMazeInConsole(maze, currentSize.x, currentSize.y);
            
            _currentLevel++;
        }

        private (int x, int y) CalculateMazeSize(int level)
        {
            // Начинаем с 9x9, каждые 2 уровня увеличиваем размер на 2
            int baseSizeIncrease = ((level - 1) / 2) * 2;
            return (mazeWidth + baseSizeIncrease, mazeHeight + baseSizeIncrease);
        }

        private void DisplayMazeInConsole(int[,] maze, int width, int height)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"Maze {width}x{height}:");
            sb.AppendLine("Legend: 1 = Wall, 0 = Path, S = Start, E = Exit");
            sb.AppendLine();

            // Находим вход и выход на периметре
            Vector2Int startPos = FindEntrancePosition(maze, width, height);
            Vector2Int exitPos = FindExitPosition(maze, width, height, startPos);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Отмечаем стартовую позицию
                    if (x == startPos.x && y == startPos.y)
                    {
                        sb.Append("S");
                    }
                    // Отмечаем выход
                    else if (x == exitPos.x && y == exitPos.y)
                    {
                        sb.Append("E");
                    }
                    else if (maze[y, x] == 1)
                    {
                        sb.Append("0"); // Стена
                    }
                    else
                    {
                        sb.Append(" "); // Проход
                    }
                }
                sb.AppendLine();
            }
            
            sb.AppendLine();
            sb.AppendLine($"Start position: [{startPos.x}, {startPos.y}]");
            sb.AppendLine($"Exit position: [{exitPos.x}, {exitPos.y}]");
            sb.AppendLine("Press SPACE for next maze");
            sb.AppendLine("================================");

            Debug.Log(sb.ToString());
        }

        // Метод для ручной генерации с конкретными размерами
        [ContextMenu("Generate 9x9")]
        public void Generate9x9()
        {
            GenerateSpecificSize(9, 9);
        }

        [ContextMenu("Generate 11x11")]
        public void Generate11x11()
        {
            GenerateSpecificSize(11, 11);
        }

        [ContextMenu("Generate 13x13")]
        public void Generate13x13()
        {
            GenerateSpecificSize(13, 13);
        }

        private void GenerateSpecificSize(int width, int height)
        {
            if (_mazeGenerator == null)
                _mazeGenerator = new MazeGenerator();

            Debug.Log($"=== MANUAL GENERATION - SIZE {width}x{height} ===");
            
            int[,] maze = _mazeGenerator.GetLabirint(width, height);
            DisplayMazeInConsole(maze, width, height);
        }

        // Метод для сброса уровня
        [ContextMenu("Reset Level")]
        public void ResetLevel()
        {
            _currentLevel = 1;
            Debug.Log("Level reset to 1");
        }

        private Vector2Int FindEntrancePosition(int[,] maze, int width, int height)
        {
            // Ищем вход на периметре
            for (int y = 0; y < height; y++)
            {
                if (maze[y, 0] == 0) return new Vector2Int(0, y); // Левая сторона
                if (maze[y, width - 1] == 0) return new Vector2Int(width - 1, y); // Правая сторона
            }
            
            for (int x = 0; x < width; x++)
            {
                if (maze[0, x] == 0) return new Vector2Int(x, 0); // Верхняя сторона
                if (maze[height - 1, x] == 0) return new Vector2Int(x, height - 1); // Нижняя сторона
            }
            
            return new Vector2Int(0, height / 2); // Fallback
        }

        private Vector2Int FindExitPosition(int[,] maze, int width, int height, Vector2Int startPos)
        {
            // Ищем выход на периметре (исключаем вход)
            for (int y = 0; y < height; y++)
            {
                Vector2Int leftPos = new Vector2Int(0, y);
                if (maze[y, 0] == 0 && leftPos != startPos) return leftPos;
                
                Vector2Int rightPos = new Vector2Int(width - 1, y);
                if (maze[y, width - 1] == 0 && rightPos != startPos) return rightPos;
            }
            
            for (int x = 0; x < width; x++)
            {
                Vector2Int topPos = new Vector2Int(x, 0);
                if (maze[0, x] == 0 && topPos != startPos) return topPos;
                
                Vector2Int bottomPos = new Vector2Int(x, height - 1);
                if (maze[height - 1, x] == 0 && bottomPos != startPos) return bottomPos;
            }
            
            return new Vector2Int(width - 1, height / 2); // Fallback
        }
    }
}

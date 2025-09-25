using UnityEngine;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Testing
{
    public class MazeTester : MonoBehaviour
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

            // Находим выход на правой стороне
            int exitY = -1;
            for (int y = 0; y < height; y++)
            {
                if (maze[y, width - 1] == 0)
                {
                    exitY = y;
                    break;
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Отмечаем стартовую позицию
                    if (x == 0 && y == Mathf.Min(5, height / 2))
                    {
                        sb.Append("S");
                    }
                    // Отмечаем выход
                    else if (x == width - 1 && y == exitY)
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
            sb.AppendLine($"Start position: [0, {Mathf.Min(5, height / 2)}]");
            sb.AppendLine($"Exit position: [{width - 1}, {exitY}]");
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
    }
}

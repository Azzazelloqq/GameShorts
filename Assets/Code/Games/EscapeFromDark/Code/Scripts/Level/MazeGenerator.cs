using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level
{
    public class MazeGenerator
    {
        private int[,] _maze;
        private int _width;
        private int _height;
        private Vector2Int _startPosition;
        private System.Random _random;

        public int[,] GetLabirint(int width, int height)
        {
            _width = width;
            _height = height;
            _random = new System.Random();
            
            // Инициализируем лабиринт стенами (1)
            _maze = new int[_height, _width];
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _maze[y, x] = 1; // 1 = стена
                }
            }

            // Стартовая позиция всегда [0, 5] (или центр если высота меньше)
            int startY = Math.Min(5, _height / 2);
            _startPosition = new Vector2Int(0, startY);
            
            // Создаем путь от стартовой позиции к противоположной стороне
            GeneratePath();
            
            // Убеждаемся что периметр - это стены, кроме входа и выхода
            EnsurePerimeterWalls();

            return _maze;
        }

        private void GeneratePath()
        {
            // Сначала создаем основной путь от входа до выхода
            List<Vector2Int> mainPathSegments = GenerateMainPath();
            
            // Добавляем ложные пути в середине длинных прямых участков
            GenerateFalsePathsInStraightSegments(mainPathSegments);
            
            // Затем добавляем тупиковые пути
            GenerateDeadEndPaths();
            
            // Финальная шлифовка - добавляем изолированные пути
            GenerateIsolatedPaths();
        }

        private List<Vector2Int> GenerateMainPath()
        {
            List<Vector2Int> mainPath = new List<Vector2Int>();
            List<Vector2Int> straightSegments = new List<Vector2Int>(); // Для отслеживания прямых участков
            Vector2Int currentPos = _startPosition;
            Vector2Int currentDirection = Vector2Int.right; // Начинаем движение вправо
            int stepsInCurrentDirection = 0;
            int maxStepsInDirection = _random.Next(1, 4); // 1-3 клетки в одном направлении
            Vector2Int segmentStart = currentPos; // Начало текущего прямого участка
            
            // Отмечаем стартовую позицию как проход
            _maze[currentPos.y, currentPos.x] = 0;
            mainPath.Add(currentPos);
            
            // Генерируем путь к правой стороне
            while (currentPos.x < _width - 2) // останавливаемся перед правой стеной
            {
                List<Vector2Int> possibleDirections = GetMainPathDirections(currentPos);
                
                if (possibleDirections.Count > 0)
                {
                    Vector2Int nextDirection;
                    bool willTurn = false;
                    
                    // Проверяем, нужно ли поворачивать
                    if (stepsInCurrentDirection >= maxStepsInDirection)
                    {
                        // Обязательный поворот - исключаем текущее направление
                        List<Vector2Int> turnDirections = new List<Vector2Int>(possibleDirections);
                        turnDirections.Remove(currentDirection);
                        
                        if (turnDirections.Count > 0)
                        {
                            // Предпочитаем направления, которые ведут к цели
                            nextDirection = ChooseBestTurnDirection(turnDirections, currentPos);
                            willTurn = true;
                        }
                        else
                        {
                            // Если поворот невозможен, продолжаем прямо
                            nextDirection = currentDirection;
                            stepsInCurrentDirection++;
                        }
                    }
                    else
                    {
                        // Можем продолжить в том же направлении или повернуть
                        if (possibleDirections.Contains(currentDirection) && _random.Next(100) < 60)
                        {
                            // Продолжаем в том же направлении
                            nextDirection = currentDirection;
                            stepsInCurrentDirection++;
                        }
                        else
                        {
                            // Добровольный поворот
                            nextDirection = possibleDirections[_random.Next(possibleDirections.Count)];
                            if (nextDirection != currentDirection)
                            {
                                willTurn = true;
                            }
                            else
                            {
                                stepsInCurrentDirection++;
                            }
                        }
                    }
                    
                    // Если поворачиваем, сохраняем прямой участок
                    if (willTurn && stepsInCurrentDirection >= 2) // Участок из 3+ клеток
                    {
                        straightSegments.Add(segmentStart);
                        straightSegments.Add(currentPos);
                        straightSegments.Add(currentDirection); // Направление прямого участка
                    }
                    
                    // Обновляем состояние при повороте
                    if (willTurn)
                    {
                        currentDirection = nextDirection;
                        stepsInCurrentDirection = 0;
                        maxStepsInDirection = _random.Next(1, 4);
                        segmentStart = currentPos; // Новый участок начинается с текущей позиции
                    }
                    
                    currentPos += nextDirection;
                    _maze[currentPos.y, currentPos.x] = 0;
                    mainPath.Add(currentPos);
                }
                else
                {
                    // Если застряли, делаем шаг назад и пробуем другое направление
                    if (mainPath.Count > 1)
                    {
                        mainPath.RemoveAt(mainPath.Count - 1);
                        currentPos = mainPath[mainPath.Count - 1];
                        // Сбрасываем счетчик направления при backtrack
                        stepsInCurrentDirection = 0;
                        maxStepsInDirection = _random.Next(1, 4);
                        segmentStart = currentPos;
                    }
                    else
                    {
                        break; // Не можем продолжить
                    }
                }
            }
            
            // Создаем финальный проход к выходу
            if (currentPos.x == _width - 2)
            {
                Vector2Int exitPos = new Vector2Int(_width - 1, currentPos.y);
                _maze[exitPos.y, exitPos.x] = 0;
            }
            
            return straightSegments;
        }

        private List<Vector2Int> GetMainPathDirections(Vector2Int currentPos)
        {
            List<Vector2Int> directions = new List<Vector2Int>();
            Vector2Int[] possibleDirections = { Vector2Int.right, Vector2Int.up, Vector2Int.down };

            foreach (Vector2Int direction in possibleDirections)
            {
                Vector2Int newPos = currentPos + direction;
                
                // Проверяем границы (оставляем место для стен по периметру)
                if (newPos.x >= 1 && newPos.x < _width - 1 && 
                    newPos.y >= 1 && newPos.y < _height - 1)
                {
                    // Проверяем, что это стена и не создаст пересечение с существующим путем
                    if (_maze[newPos.y, newPos.x] == 1 && !WillCreateIntersection(newPos))
                    {
                        directions.Add(direction);
                    }
                }
            }

            return directions;
        }

        private bool WillCreateIntersection(Vector2Int pos)
        {
            // Проверяем, что рядом нет других проходов (кроме того, откуда пришли)
            int pathCount = 0;
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int adjacentPos = pos + direction;
                if (IsValidPosition(adjacentPos) && _maze[adjacentPos.y, adjacentPos.x] == 0)
                {
                    pathCount++;
                }
            }

            return pathCount > 1; // Больше одного соседнего прохода = пересечение
        }

        private Vector2Int ChooseBestTurnDirection(List<Vector2Int> turnDirections, Vector2Int currentPos)
        {
            // Приоритет направлений для достижения цели (правая сторона)
            Vector2Int[] priorityOrder = { Vector2Int.right, Vector2Int.up, Vector2Int.down, Vector2Int.left };
            
            foreach (Vector2Int priority in priorityOrder)
            {
                if (turnDirections.Contains(priority))
                {
                    return priority;
                }
            }
            
            // Если ничего не найдено по приоритету, возвращаем случайное
            return turnDirections[_random.Next(turnDirections.Count)];
        }

        private void GenerateFalsePathsInStraightSegments(List<Vector2Int> straightSegments)
        {
            // Обрабатываем каждый прямой участок (каждые 3 элемента: начало, конец, направление)
            for (int i = 0; i < straightSegments.Count; i += 3)
            {
                if (i + 2 < straightSegments.Count)
                {
                    Vector2Int segmentStart = straightSegments[i];
                    Vector2Int segmentEnd = straightSegments[i + 1];
                    Vector2Int segmentDirection = straightSegments[i + 2];
                    
                    CreateFalsePathInSegment(segmentStart, segmentEnd, segmentDirection);
                }
            }
        }

        private void CreateFalsePathInSegment(Vector2Int segmentStart, Vector2Int segmentEnd, Vector2Int segmentDirection)
        {
            // Вычисляем среднюю точку прямого участка
            Vector2Int middlePoint = new Vector2Int(
                (segmentStart.x + segmentEnd.x) / 2,
                (segmentStart.y + segmentEnd.y) / 2
            );
            
            // Определяем перпендикулярные направления для ложного пути
            List<Vector2Int> perpendicularDirections = GetPerpendicularDirections(segmentDirection);
            
            foreach (Vector2Int falseDirection in perpendicularDirections)
            {
                // Создаем ложный путь длиной 1-2 клетки в перпендикулярном направлении
                int falsePathLength = _random.Next(1, 3);
                Vector2Int currentPos = middlePoint;
                
                for (int step = 0; step < falsePathLength; step++)
                {
                    Vector2Int nextPos = currentPos + falseDirection;
                    
                    // Проверяем, можно ли создать ложный путь
                    if (CanCreateFalsePath(nextPos))
                    {
                        _maze[nextPos.y, nextPos.x] = 0;
                        currentPos = nextPos;
                    }
                    else
                    {
                        break; // Не можем продолжить ложный путь
                    }
                }
                
                // Создаем только один ложный путь на участок
                if (currentPos != middlePoint)
                {
                    break;
                }
            }
        }

        private List<Vector2Int> GetPerpendicularDirections(Vector2Int direction)
        {
            List<Vector2Int> perpendicular = new List<Vector2Int>();
            
            if (direction == Vector2Int.right || direction == Vector2Int.left)
            {
                // Для горизонтального направления - вертикальные перпендикуляры
                perpendicular.Add(Vector2Int.up);
                perpendicular.Add(Vector2Int.down);
            }
            else if (direction == Vector2Int.up || direction == Vector2Int.down)
            {
                // Для вертикального направления - горизонтальные перпендикуляры
                perpendicular.Add(Vector2Int.left);
                perpendicular.Add(Vector2Int.right);
            }
            
            return perpendicular;
        }

        private bool CanCreateFalsePath(Vector2Int pos)
        {
            // Проверяем базовые границы (не касаемся периметра)
            if (pos.x <= 0 || pos.x >= _width - 1 || pos.y <= 0 || pos.y >= _height - 1)
                return false;
            
            // Проверяем, что это стена
            if (_maze[pos.y, pos.x] != 1)
                return false;
            
            // Проверяем, что создание прохода не нарушит структуру
            // (рядом должно быть не более одного прохода)
            return CountAdjacentPaths(pos) <= 1;
        }


        private bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < _width && pos.y >= 0 && pos.y < _height;
        }


        private void EnsurePerimeterWalls()
        {
            // Находим выход на правой стороне
            Vector2Int exitPosition = FindExitPosition();
            
            // Делаем весь периметр стенами
            for (int x = 0; x < _width; x++)
            {
                _maze[0, x] = 1; // верхняя стена
                _maze[_height - 1, x] = 1; // нижняя стена
            }
            
            for (int y = 0; y < _height; y++)
            {
                _maze[y, 0] = 1; // левая стена
                _maze[y, _width - 1] = 1; // правая стена
            }
            
            // Делаем вход (стартовая позиция)
            _maze[_startPosition.y, _startPosition.x] = 0;
            
            // Делаем единственный выход
            if (exitPosition != Vector2Int.one * -1)
            {
                _maze[exitPosition.y, exitPosition.x] = 0;
            }
        }

        private Vector2Int FindExitPosition()
        {
            // Ищем проход на правой стороне лабиринта
            for (int y = 1; y < _height - 1; y++) // не включаем углы
            {
                if (_maze[y, _width - 1] == 0)
                {
                    return new Vector2Int(_width - 1, y);
                }
            }
            
            // Если не нашли выход, создаем его в центре правой стороны
            int exitY = _height / 2;
            return new Vector2Int(_width - 1, exitY);
        }

        private int CountAdjacentPaths(Vector2Int pos)
        {
            int pathCount = 0;
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int adjacentPos = pos + direction;
                if (IsValidPosition(adjacentPos) && _maze[adjacentPos.y, adjacentPos.x] == 0)
                {
                    pathCount++;
                }
            }

            return pathCount;
        }

        private void GenerateDeadEndPaths()
        {
            // Создаем 3-4 тупиковых пути
            int deadEndCount = _random.Next(3, 5);
            
            for (int i = 0; i < deadEndCount; i++)
            {
                CreateDeadEndPath();
            }
        }

        private void CreateDeadEndPath()
        {
            // Находим случайную позицию на основном пути для создания ответвления
            Vector2Int startPos = FindRandomMainPathPosition();
            if (startPos == Vector2Int.one * -1) return; // Не найдена подходящая позиция
            
            // Создаем тупиковый путь длиной от 2 до 5 клеток
            int pathLength = _random.Next(2, 6);
            Vector2Int currentPos = startPos;
            
            for (int step = 0; step < pathLength; step++)
            {
                List<Vector2Int> availableDirections = GetDeadEndDirections(currentPos);
                
                if (availableDirections.Count > 0)
                {
                    Vector2Int direction = availableDirections[_random.Next(availableDirections.Count)];
                    currentPos += direction;
                    _maze[currentPos.y, currentPos.x] = 0;
                }
                else
                {
                    break; // Не можем продолжить тупик
                }
            }
        }

        private Vector2Int FindRandomMainPathPosition()
        {
            List<Vector2Int> mainPathPositions = new List<Vector2Int>();
            
            // Ищем позиции основного пути, которые подходят для создания ответвлений
            for (int y = 1; y < _height - 1; y++)
            {
                for (int x = 1; x < _width - 2; x++) // не включаем последний столбец
                {
                    if (_maze[y, x] == 0) // это проход
                    {
                        // Проверяем, что рядом есть место для тупика
                        if (HasSpaceForDeadEnd(new Vector2Int(x, y)))
                        {
                            mainPathPositions.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }
            
            return mainPathPositions.Count > 0 
                ? mainPathPositions[_random.Next(mainPathPositions.Count)] 
                : Vector2Int.one * -1;
        }

        private bool HasSpaceForDeadEnd(Vector2Int pos)
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            foreach (Vector2Int direction in directions)
            {
                Vector2Int checkPos = pos + direction;
                if (IsValidPosition(checkPos) && 
                    checkPos.x >= 1 && checkPos.x < _width - 1 && 
                    checkPos.y >= 1 && checkPos.y < _height - 1 &&
                    _maze[checkPos.y, checkPos.x] == 1)
                {
                    // Проверяем, что это место изолировано (окружено стенами)
                    int wallCount = CountAdjacentWalls(checkPos);
                    if (wallCount >= 3) // минимум 3 стены вокруг для хорошего тупика
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        private List<Vector2Int> GetDeadEndDirections(Vector2Int currentPos)
        {
            List<Vector2Int> directions = new List<Vector2Int>();
            Vector2Int[] possibleDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int direction in possibleDirections)
            {
                Vector2Int newPos = currentPos + direction;
                
                // Проверяем границы (не касаемся периметра)
                if (newPos.x >= 1 && newPos.x < _width - 1 && 
                    newPos.y >= 1 && newPos.y < _height - 1)
                {
                    // Проверяем, что это стена и создание прохода не нарушит структуру
                    if (_maze[newPos.y, newPos.x] == 1 && CountAdjacentPaths(newPos) <= 1)
                    {
                        directions.Add(direction);
                    }
                }
            }

            return directions;
        }

        private int CountAdjacentWalls(Vector2Int pos)
        {
            int wallCount = 0;
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int adjacentPos = pos + direction;
                if (IsValidPosition(adjacentPos) && _maze[adjacentPos.y, adjacentPos.x] == 1)
                {
                    wallCount++;
                }
            }

            return wallCount;
        }

        private void GenerateIsolatedPaths()
        {
            // Ищем области со стенами, которые не граничат с существующими путями
            List<Vector2Int> isolatedAreas = FindIsolatedWallAreas();
            
            // Создаем небольшие изолированные пути в найденных областях
            int isolatedPathsCount = Math.Min(isolatedAreas.Count, _random.Next(2, 5));
            
            for (int i = 0; i < isolatedPathsCount; i++)
            {
                if (i < isolatedAreas.Count)
                {
                    CreateIsolatedPath(isolatedAreas[i]);
                }
            }
        }

        private List<Vector2Int> FindIsolatedWallAreas()
        {
            List<Vector2Int> isolatedAreas = new List<Vector2Int>();
            
            // Проходим по всему лабиринту, исключая периметр
            for (int y = 2; y < _height - 2; y++)
            {
                for (int x = 2; x < _width - 2; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    // Проверяем, что это стена и область изолирована от существующих путей
                    if (_maze[y, x] == 1 && IsIsolatedWallArea(pos))
                    {
                        isolatedAreas.Add(pos);
                    }
                }
            }
            
            return isolatedAreas;
        }

        private bool IsIsolatedWallArea(Vector2Int pos)
        {
            // Проверяем область 3x3 вокруг позиции
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    Vector2Int checkPos = pos + new Vector2Int(dx, dy);
                    
                    if (IsValidPosition(checkPos))
                    {
                        // Если в области 3x3 есть проходы, то область не изолирована
                        if (_maze[checkPos.y, checkPos.x] == 0)
                        {
                            return false;
                        }
                    }
                }
            }
            
            return true;
        }

        private void CreateIsolatedPath(Vector2Int startPos)
        {
            // Создаем небольшой изолированный путь длиной 2-4 клетки
            int pathLength = _random.Next(2, 5);
            Vector2Int currentPos = startPos;
            List<Vector2Int> pathPositions = new List<Vector2Int>();
            
            // Отмечаем стартовую позицию как проход
            _maze[currentPos.y, currentPos.x] = 0;
            pathPositions.Add(currentPos);
            
            for (int step = 0; step < pathLength; step++)
            {
                List<Vector2Int> availableDirections = GetIsolatedPathDirections(currentPos);
                
                if (availableDirections.Count > 0)
                {
                    Vector2Int direction = availableDirections[_random.Next(availableDirections.Count)];
                    currentPos += direction;
                    
                    _maze[currentPos.y, currentPos.x] = 0;
                    pathPositions.Add(currentPos);
                }
                else
                {
                    break; // Не можем продолжить изолированный путь
                }
            }
            
            // Иногда создаем небольшое ответвление в изолированном пути
            if (pathPositions.Count >= 3 && _random.Next(100) < 50)
            {
                CreateIsolatedBranch(pathPositions);
            }
        }

        private List<Vector2Int> GetIsolatedPathDirections(Vector2Int currentPos)
        {
            List<Vector2Int> directions = new List<Vector2Int>();
            Vector2Int[] possibleDirections = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int direction in possibleDirections)
            {
                Vector2Int newPos = currentPos + direction;
                
                // Проверяем границы (не касаемся периметра)
                if (newPos.x >= 1 && newPos.x < _width - 1 && 
                    newPos.y >= 1 && newPos.y < _height - 1)
                {
                    // Проверяем, что это стена и создание прохода сохранит изоляцию
                    if (_maze[newPos.y, newPos.x] == 1 && WillRemainIsolated(newPos))
                    {
                        directions.Add(direction);
                    }
                }
            }

            return directions;
        }

        private bool WillRemainIsolated(Vector2Int pos)
        {
            // Проверяем, что создание прохода в этой позиции не соединит с основными путями
            int pathCount = 0;
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int adjacentPos = pos + direction;
                if (IsValidPosition(adjacentPos) && _maze[adjacentPos.y, adjacentPos.x] == 0)
                {
                    pathCount++;
                }
            }

            // Разрешаем создание прохода только если рядом максимум один существующий проход
            // (тот, откуда мы пришли в изолированном пути)
            return pathCount <= 1;
        }

        private void CreateIsolatedBranch(List<Vector2Int> mainPath)
        {
            // Выбираем случайную позицию из основного изолированного пути (не первую и не последнюю)
            if (mainPath.Count < 3) return;
            
            int branchIndex = _random.Next(1, mainPath.Count - 1);
            Vector2Int branchStart = mainPath[branchIndex];
            
            // Создаем короткое ответвление длиной 1-2 клетки
            int branchLength = _random.Next(1, 3);
            Vector2Int currentPos = branchStart;
            
            for (int step = 0; step < branchLength; step++)
            {
                List<Vector2Int> availableDirections = GetIsolatedPathDirections(currentPos);
                
                if (availableDirections.Count > 0)
                {
                    Vector2Int direction = availableDirections[_random.Next(availableDirections.Count)];
                    currentPos += direction;
                    _maze[currentPos.y, currentPos.x] = 0;
                }
                else
                {
                    break; // Не можем продолжить ответвление
                }
            }
        }

    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Code.Games.Lawnmower.Scripts.Grass
{
    /// <summary>
    /// GPU Instanced версия сетки травы для максимальной производительности
    /// </summary>
    internal class GrassGridInstanced : MonoBehaviour, IGrassGrid
    {
        [Header("Grid Settings")] [SerializeField]
        private GameObject grassPrefab; // Альтернатива для извлечения меша и материала

        [SerializeField] private int gridWidth = 50;
        [SerializeField] private int gridHeight = 50;
        [SerializeField] private Vector2 _tileSize = new Vector2(1f, 1f);
        public Vector2 tileSize => _tileSize;
        [SerializeField] private Vector2 gridOffset = Vector2.zero;
        [SerializeField] private float baseSeed = 1.0f;

        [Header("Randomization")] [SerializeField]
        private float lengthVariation = 3f;

        [SerializeField] private float widthVariation = 2f;

        [Header("Input")] [SerializeField] private Camera mainCamera;
        [SerializeField] private bool enableInput = true;

        [Header("Performance")] [SerializeField]
        private int maxInstancesPerBatch = 1023; // Unity limit

        [Header("Animation")] [SerializeField] private float shrinkDuration = 1f;

        [Header("Editor Preview")] [SerializeField]
        private bool showGrassPreviewInEditor = true;

        [SerializeField] private Color grassPreviewColor = Color.green;
        [SerializeField] private Color cutGrassPreviewColor = Color.red;

        // GPU Instancing data
        private List<Matrix4x4> matrices;
        private List<MaterialPropertyBlock> propertyBlocks;
        private List<float[]> seedArrays;
        private List<float[]> shrinkArrays;
        private List<float[]> lengthArrays;
        private List<float[]> widthArrays;

        // Grid state
        private float[,] tileStates; // 0 = visible, 1 = cut
        private bool isInitialized = false;

        // Маппинг для обновления состояний тайлов
        private Dictionary<(int x, int y), (int batchIndex, int localIndex)> tileMapping;

        // Система анимации
        private Dictionary<(int x, int y), Coroutine> activeAnimations;
        private Dictionary<(int x, int y), float> targetShrinkValues;

        // Property IDs
        private static readonly int SeedPropertyID = Shader.PropertyToID("_InstanceSeed");
        private static readonly int ShrinkPropertyID = Shader.PropertyToID("_InstanceShrink");
        private static readonly int GrassLengthPropertyID = Shader.PropertyToID("_InstanceGrassLength");
        private static readonly int GrassWidthPropertyID = Shader.PropertyToID("_InstanceGrassWidth");

        private Material grassMaterial;
        private Mesh grassMesh;

        private void Start()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (isInitialized)
            {
                return;
            }

            InitializeGrid();
        }

        private void Update()
        {
            if (isInitialized)
            {
                RenderInstances();
            }
        }

        /// <summary>
        /// Извлечение материала и меша из префаба
        /// </summary>
        private void ExtractFromPrefab()
        {
            if (grassPrefab == null) return;

            // Пытаемся получить MeshRenderer и MeshFilter
            MeshRenderer meshRenderer = grassPrefab.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = grassPrefab.GetComponent<MeshFilter>();

            // Если не найдены в корне, ищем в дочерних объектах
            if (meshRenderer == null)
                meshRenderer = grassPrefab.GetComponentInChildren<MeshRenderer>();
            if (meshFilter == null)
                meshFilter = grassPrefab.GetComponentInChildren<MeshFilter>();

            // Пытаемся получить SpriteRenderer (для 2D префабов)
            if (meshRenderer == null)
            {
                SpriteRenderer spriteRenderer = grassPrefab.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                    spriteRenderer = grassPrefab.GetComponentInChildren<SpriteRenderer>();

                if (spriteRenderer != null)
                {
                    grassMaterial = spriteRenderer.sharedMaterial;
                    // Для спрайтов используем quad меш
                    grassMesh = CreateQuadMesh();
                    Debug.Log("GrassGridInstanced: Extracted material from SpriteRenderer and created quad mesh");
                    return;
                }
            }

            // Извлекаем материал и меш
            if (meshRenderer != null && grassMaterial == null)
            {
                grassMaterial = meshRenderer.sharedMaterial;
            }

            if (meshFilter != null && grassMesh == null)
            {
                grassMesh = meshFilter.sharedMesh;
            }

            if (grassMaterial != null || grassMesh != null)
            {
                Debug.Log(
                    $"GrassGridInstanced: Extracted from prefab - Material: {grassMaterial?.name}, Mesh: {grassMesh?.name}");
            }
        }

        /// <summary>
        /// Создание простого quad меша для спрайтов
        /// </summary>
        private Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Generated Quad";

            // Вершины квада
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3(0.5f, -0.5f, 0),
                new Vector3(-0.5f, 0.5f, 0),
                new Vector3(0.5f, 0.5f, 0)
            };

            // UV координаты
            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };

            // Треугольники
            int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        /// <summary>
        /// Инициализация сетки
        /// </summary>
        private void InitializeGrid()
        {
            // Попытка получить материал и меш из префаба, если они не назначены напрямую
            if ((grassMaterial == null || grassMesh == null) && grassPrefab != null)
            {
                ExtractFromPrefab();
            }

            if (grassMaterial == null || grassMesh == null)
            {
                Debug.LogError(
                    "GrassGridInstanced: Material or Mesh is not assigned! Either assign them directly or provide a grassPrefab.");
                return;
            }

            int totalTiles = gridWidth * gridHeight;
            int batchCount = Mathf.CeilToInt((float)totalTiles / maxInstancesPerBatch);

            // Инициализируем списки для батчей
            matrices = new List<Matrix4x4>();
            propertyBlocks = new List<MaterialPropertyBlock>();
            seedArrays = new List<float[]>();
            shrinkArrays = new List<float[]>();
            lengthArrays = new List<float[]>();
            widthArrays = new List<float[]>();

            // Инициализируем состояния тайлов
            tileStates = new float[gridWidth, gridHeight];
            tileMapping = new Dictionary<(int x, int y), (int batchIndex, int localIndex)>();
            activeAnimations = new Dictionary<(int x, int y), Coroutine>();
            targetShrinkValues = new Dictionary<(int x, int y), float>();

            // Создаем список тайлов в правильном порядке отрисовки (снизу вверх)
            var orderedTiles = new List<(int x, int y, int originalIndex)>();
            for (int y = gridHeight - 1; y >= 0; y--) // Сверху вниз в списке, но будет рисоваться снизу вверх
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    int originalIndex = y * gridWidth + x;
                    orderedTiles.Add((x, y, originalIndex));
                }
            }

            // Создаем данные для каждого батча
            for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                int startIndex = batchIndex * maxInstancesPerBatch;
                int endIndex = Mathf.Min(startIndex + maxInstancesPerBatch, totalTiles);
                int instancesInBatch = endIndex - startIndex;

                var batchMatrices = new Matrix4x4[instancesInBatch];
                var batchSeeds = new float[instancesInBatch];
                var batchShrinks = new float[instancesInBatch];
                var batchLengths = new float[instancesInBatch];
                var batchWidths = new float[instancesInBatch];

                for (int i = 0; i < instancesInBatch; i++)
                {
                    int listIndex = startIndex + i;
                    var tileData = orderedTiles[listIndex];
                    int x = tileData.x;
                    int y = tileData.y;

                    // Позиция тайла
                    Vector3 position = GetTileWorldPosition(x, y);
                    Vector3
                        scale = new Vector3(_tileSize.x, _tileSize.x,
                            1f); // Квадратный тайл по X, но может быть вытянут по Y для наложения травы
                    batchMatrices[i] = Matrix4x4.TRS(position, Quaternion.identity, scale);

                    // Генерируем уникальные параметры
                    float uniqueSeed = baseSeed + (x * 17.3f) + (y * 23.7f) + (x * y * 0.1f);
                    System.Random random = new System.Random((int)(uniqueSeed * 1000));

                    batchSeeds[i] = uniqueSeed;
                    batchShrinks[i] = 0f; // Изначально трава видна

                    // Случайные параметры
                    float baseLength = grassMaterial.GetFloat("_GrassLength");
                    if (baseLength == 0) baseLength = 5f;
                    batchLengths[i] = baseLength + (float)(random.NextDouble() * lengthVariation * 2 - lengthVariation);
                    batchLengths[i] = Mathf.Max(batchLengths[i], 2f);

                    float baseWidth = grassMaterial.GetFloat("_GrassWidth");
                    if (baseWidth == 0) baseWidth = 0.8f;
                    batchWidths[i] = baseWidth + (float)(random.NextDouble() * widthVariation * 2 - widthVariation);
                    batchWidths[i] = Mathf.Max(batchWidths[i], 0.3f);

                    // Инициализируем состояние тайла
                    tileStates[x, y] = 0f;

                    // Сохраняем маппинг для быстрого доступа к обновлению состояний
                    tileMapping[(x, y)] = (batchIndex, i);
                }

                // Создаем MaterialPropertyBlock для батча
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetFloatArray(SeedPropertyID, batchSeeds);
                propertyBlock.SetFloatArray(ShrinkPropertyID, batchShrinks);
                propertyBlock.SetFloatArray(GrassLengthPropertyID, batchLengths);
                propertyBlock.SetFloatArray(GrassWidthPropertyID, batchWidths);

                // Добавляем в списки
                matrices.AddRange(batchMatrices);
                propertyBlocks.Add(propertyBlock);
                seedArrays.Add(batchSeeds);
                shrinkArrays.Add(batchShrinks);
                lengthArrays.Add(batchLengths);
                widthArrays.Add(batchWidths);
            }

            isInitialized = true;
            Debug.Log(
                $"GrassGridInstanced: Initialized {gridWidth}x{gridHeight} grid with {totalTiles} tiles in {batchCount} batches");
        }

        /// <summary>
        /// Отрисовка инстансов
        /// </summary>
        private void RenderInstances()
        {
            int batchCount = propertyBlocks.Count;
            int matrixIndex = 0;

            for (int batchIndex = 0; batchIndex < batchCount; batchIndex++)
            {
                int instancesInBatch = Mathf.Min(maxInstancesPerBatch, matrices.Count - matrixIndex);
                var batchMatrices = new Matrix4x4[instancesInBatch];

                for (int i = 0; i < instancesInBatch; i++)
                {
                    batchMatrices[i] = matrices[matrixIndex + i];
                }

                Graphics.DrawMeshInstanced(
                    grassMesh,
                    0,
                    grassMaterial,
                    batchMatrices,
                    instancesInBatch,
                    propertyBlocks[batchIndex]
                );

                matrixIndex += instancesInBatch;
            }
        }

        /// <summary>
        /// Убрать траву в позиции
        /// </summary>
        public bool CutGrassAtPosition(Vector3 worldPosition)
        {
            Vector2Int gridPos = GetGridIndices(worldPosition);
            return CutGrassAt(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// Убрать траву в определенной позиции сетки
        /// </summary>
        public bool CutGrassAt(int gridX, int gridY)
        {
            if (!IsValidGridPosition(gridX, gridY)) return false;

            if (tileStates[gridX, gridY] < 0.5f) // Если трава еще видна
            {
                tileStates[gridX, gridY] = 1f; // Помечаем как срезанную
                StartShrinkAnimation(gridX, gridY);
                return true; // Трава была подстрижена
            }

            return false; // Трава уже была подстрижена
        }

        /// <summary>
        /// Восстановить траву в определенной позиции сетки (мгновенно)
        /// </summary>
        public void RestoreGrassAt(int gridX, int gridY)
        {
            if (!IsValidGridPosition(gridX, gridY)) return;

            tileStates[gridX, gridY] = 0f;

            // Останавливаем анимацию сжатия, если она активна
            var key = (gridX, gridY);
            if (activeAnimations.ContainsKey(key))
            {
                StopCoroutine(activeAnimations[key]);
                activeAnimations.Remove(key);
            }

            // Мгновенно восстанавливаем
            UpdateTileState(gridX, gridY, 0f);
        }

        /// <summary>
        /// Обновить состояние тайла
        /// </summary>
        private void UpdateTileState(int gridX, int gridY, float shrinkValue)
        {
            if (tileMapping.TryGetValue((gridX, gridY), out var mapping))
            {
                int batchIndex = mapping.batchIndex;
                int localIndex = mapping.localIndex;

                if (batchIndex < shrinkArrays.Count && localIndex < shrinkArrays[batchIndex].Length)
                {
                    shrinkArrays[batchIndex][localIndex] = shrinkValue;
                    propertyBlocks[batchIndex].SetFloatArray(ShrinkPropertyID, shrinkArrays[batchIndex]);
                }
            }
        }

        /// <summary>
        /// Получить мировую позицию тайла
        /// </summary>
        public Vector3 GetTileWorldPosition(int gridX, int gridY)
        {
            float localX = gridX * _tileSize.x + gridOffset.x;
            float localY = gridY * _tileSize.y + gridOffset.y;
            Vector3 localPosition = new Vector3(localX, localY, 0f);

            // Преобразуем локальную позицию в мировую относительно позиции объекта
            return transform.TransformPoint(localPosition);
        }

        /// <summary>
        /// Получить индексы сетки по мировой позиции
        /// </summary>
        public Vector2Int GetGridIndices(Vector3 worldPosition)
        {
            // Преобразуем мировую позицию в локальную относительно объекта
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            int gridX = Mathf.FloorToInt((localPosition.x - gridOffset.x) / _tileSize.x);
            int gridY = Mathf.FloorToInt((localPosition.y - gridOffset.y) / _tileSize.y);
            return new Vector2Int(gridX, gridY);
        }

        /// <summary>
        /// Проверить валидность позиции сетки
        /// </summary>
        private bool IsValidGridPosition(int gridX, int gridY)
        {
            return gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight;
        }

        /// <summary>
        /// Восстановить всю траву
        /// </summary>
        public void RestoreAllGrass()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    tileStates[x, y] = 0f;
                    UpdateTileState(x, y, 0f);
                }
            }
        }

        /// <summary>
        /// Убрать всю траву
        /// </summary>
        public void CutAllGrass()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    tileStates[x, y] = 1f;
                    UpdateTileState(x, y, 1f);
                }
            }
        }

        /// <summary>
        /// Получить размер сетки
        /// </summary>
        public Vector2Int GetGridSize()
        {
            return new Vector2Int(gridWidth, gridHeight);
        }

        /// <summary>
        /// Проверить, инициализована ли сетка
        /// </summary>
        public bool IsInitialized()
        {
            return isInitialized;
        }

        /// <summary>
        /// Получить процент скошенной травы
        /// </summary>
        public float GetCutPercentage()
        {
            if (!isInitialized) return 0f;

            int totalTiles = gridWidth * gridHeight;
            int cutTiles = 0;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (tileStates[x, y] > 0.5f) // Трава срезана
                    {
                        cutTiles++;
                    }
                }
            }

            return totalTiles > 0 ? (float)cutTiles / totalTiles : 0f;
        }

        /// <summary>
        /// Проверить, срезана ли трава в определенной позиции
        /// </summary>
        public bool IsGrassCutAt(int gridX, int gridY)
        {
            if (!IsValidGridPosition(gridX, gridY)) return false;
            return tileStates[gridX, gridY] > 0.5f;
        }

        /// <summary>
        /// Начать анимацию сжатия травы
        /// </summary>
        private void StartShrinkAnimation(int gridX, int gridY)
        {
            var key = (gridX, gridY);

            // Останавливаем текущую анимацию, если есть
            if (activeAnimations.ContainsKey(key))
            {
                StopCoroutine(activeAnimations[key]);
                activeAnimations.Remove(key);
            }

            // Запускаем новую анимацию
            var animation = StartCoroutine(ShrinkAnimationCoroutine(gridX, gridY));
            activeAnimations[key] = animation;
            targetShrinkValues[key] = 1f;
        }


        /// <summary>
        /// Корутина анимации сжатия травы
        /// </summary>
        private System.Collections.IEnumerator ShrinkAnimationCoroutine(int gridX, int gridY)
        {
            var key = (gridX, gridY);
            float startTime = Time.time;
            float currentShrink = GetCurrentShrinkValue(gridX, gridY);

            while (Time.time - startTime < shrinkDuration)
            {
                float progress = (Time.time - startTime) / shrinkDuration;
                float shrinkValue = Mathf.Lerp(currentShrink, 1f, progress);
                UpdateTileState(gridX, gridY, shrinkValue);
                yield return null;
            }

            // Финальное значение
            UpdateTileState(gridX, gridY, 1f);

            // Убираем из активных анимаций
            activeAnimations.Remove(key);
        }


        /// <summary>
        /// Получить текущее значение shrink для тайла
        /// </summary>
        private float GetCurrentShrinkValue(int gridX, int gridY)
        {
            if (tileMapping.TryGetValue((gridX, gridY), out var mapping))
            {
                int batchIndex = mapping.batchIndex;
                int localIndex = mapping.localIndex;

                if (batchIndex < shrinkArrays.Count && localIndex < shrinkArrays[batchIndex].Length)
                {
                    return shrinkArrays[batchIndex][localIndex];
                }
            }

            return 0f;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Отрисовка предварительного просмотра сетки травы в редакторе
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGrassPreviewInEditor) return;

            // Если сетка не инициализирована, показываем предварительный просмотр
            if (!isInitialized || !Application.isPlaying)
            {
                DrawPreviewGrid();
            }
            else
            {
                // В режиме игры показываем текущее состояние
                DrawRuntimeGrid();
            }
        }

        /// <summary>
        /// Отрисовка предварительного просмотра сетки (в редакторе)
        /// </summary>
        private void DrawPreviewGrid()
        {
            Gizmos.color = grassPreviewColor;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 tileCenter = GetTileWorldPosition(x, y);
                    Vector3 tileSize3D = new Vector3(_tileSize.x, _tileSize.y, 0.1f);

                    // Отрисовываем куб для каждого тайла травы
                    Gizmos.DrawCube(tileCenter, tileSize3D * 0.8f); // Немного меньше для видимости границ

                    // Отрисовываем рамку
                    Gizmos.color = Color.white * 0.5f;
                    Gizmos.DrawWireCube(tileCenter, tileSize3D);
                    Gizmos.color = grassPreviewColor;
                }
            }

            // Отрисовываем границы всей сетки
            Vector3 localGridCenter = new Vector3(
                (gridWidth - 1) * _tileSize.x * 0.5f + gridOffset.x,
                (gridHeight - 1) * _tileSize.y * 0.5f + gridOffset.y,
                0f
            );
            Vector3 worldGridCenter = transform.TransformPoint(localGridCenter);
            Vector3 gridSize = new Vector3(gridWidth * _tileSize.x, gridHeight * _tileSize.y, 0.1f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(worldGridCenter, gridSize);
        }

        /// <summary>
        /// Отрисовка сетки в режиме выполнения (показывает срезанную траву)
        /// </summary>
        private void DrawRuntimeGrid()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 tileCenter = GetTileWorldPosition(x, y);
                    Vector3 tileSize3D = new Vector3(_tileSize.x, _tileSize.y, 0.1f);

                    // Выбираем цвет в зависимости от состояния травы
                    float grassState = tileStates[x, y];
                    if (grassState > 0.5f)
                    {
                        // Трава срезана
                        Gizmos.color = cutGrassPreviewColor;
                    }
                    else
                    {
                        // Трава цела
                        Gizmos.color = grassPreviewColor;

                        // Если есть анимация сжатия, показываем промежуточный цвет
                        var key = (x, y);
                        if (activeAnimations.ContainsKey(key))
                        {
                            float currentShrink = GetCurrentShrinkValue(x, y);
                            Gizmos.color = Color.Lerp(grassPreviewColor, cutGrassPreviewColor, currentShrink);
                        }
                    }

                    // Отрисовываем куб для каждого тайла
                    Gizmos.DrawCube(tileCenter, tileSize3D * 0.8f);

                    // Отрисовываем рамку
                    Gizmos.color = Color.white * 0.3f;
                    Gizmos.DrawWireCube(tileCenter, tileSize3D);
                }
            }
        }
#endif
    }
}
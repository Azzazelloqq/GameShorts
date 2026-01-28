using UnityEngine;
using System.Collections.Generic;
using Code.Games.Lawnmower.Scripts.Grass;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Level
{
    internal class LevelView : MonoBehaviour
    {
        [Header("Level Info")]
        [SerializeField] private string levelName = "Level 1";
        [SerializeField] private int levelIndex = 0;
        
        [Header("Grass Fields")]
        [SerializeField] private GrassFieldView[] grassFields;
        
        [Header("Player Spawn")]
        [SerializeField] private Transform playerSpawnPoint;
        
        [Header("Level Bounds")]
        [SerializeField] private Collider2D levelBounds;
        [SerializeField] private Transform cameraMinPoint;
        [SerializeField] private Transform cameraMaxPoint;
        
        [SerializeField] private EmptyingZoneView emptyingZone;
        
        // Properties
        public string LevelName => levelName;
        public int LevelIndex => levelIndex;
        public GrassFieldView[] GrassFields => grassFields;
        public Transform PlayerSpawnPoint => playerSpawnPoint;
        public Collider2D LevelBounds => levelBounds;

        public EmptyingZoneView EmptyingZone => emptyingZone;

        // Level completion tracking
        private int _completedFields = 0;
        public bool IsCompleted => _completedFields >= grassFields.Length;
        public float CompletionProgress => grassFields.Length > 0 ? (float)_completedFields / grassFields.Length : 1f;
        
        
        public void StartLevel()
        {
            _completedFields = 0;
            
            // Подписываемся на события завершения полей
            foreach (var field in grassFields)
            {
                field.OnFieldCompleted += OnFieldCompleted;
                field.ResetField();
            }
            
            gameObject.SetActive(true);
        }
        
        public void StopLevel()
        {
            // Отписываемся от событий
            foreach (var field in grassFields)
            {
                field.OnFieldCompleted -= OnFieldCompleted;
            }
            
            gameObject.SetActive(false);
        }
        
        private void OnFieldCompleted(GrassFieldView completedField)
        {
            _completedFields++;
            
            if (IsCompleted)
            {
                OnLevelCompleted?.Invoke(this);
            }
        }
        
        public System.Action<LevelView> OnLevelCompleted;
        
        public Vector3 GetPlayerSpawnPosition()
        {
            return playerSpawnPoint != null ? playerSpawnPoint.position : transform.position;
        }
        
        public bool IsPositionInBounds(Vector3 position)
        {
            if (levelBounds == null) return true;
            
            return levelBounds.bounds.Contains(position);
        }

        /// <summary>
        /// Попытаться получить мировые границы уровня для камеры.
        /// Приоритет:
        /// 1) Две точки (левая нижняя и правая верхняя).
        /// 2) Параметры сетки травы (GrassGrid) c фиксированным офсетом.
        /// </summary>
        public bool TryGetCameraBounds(out Vector2 min, out Vector2 max)
        {
            // Инициализация выходных значений по умолчанию
            min = Vector2.zero;
            max = Vector2.zero;

            // 1. Используем явно заданные точки, если обе присутствуют
            if (cameraMinPoint != null && cameraMaxPoint != null)
            {
                Vector3 p1 = cameraMinPoint.position;
                Vector3 p2 = cameraMaxPoint.position;

                min = new Vector2(Mathf.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y));
                max = new Vector2(Mathf.Max(p1.x, p2.x), Mathf.Max(p1.y, p2.y));

                // Проверка на валидный размер
                return (max.x - min.x) >= 0.1f && (max.y - min.y) >= 0.1f;
            }

            
            // 2. Фолбэк — пытаемся взять пределы по первому доступному GrassGrid
            GrassGridInstanced grid = GetPrimaryGrassGrid();
            if (grid != null)
            {
                Vector2 tileSize = grid.tileSize;
                if (tileSize.x <= 0f || tileSize.y <= 0f)
                {
                    return false;
                }

                int width = grid.GridWidth;
                int height = grid.GridHeight;
                if (width <= 0 || height <= 0)
                {
                    return false;
                }

                Vector2 offset = grid.GridOffset;

                // Локальные координаты прямоугольника грида
                Vector2 localMin = offset;
                Vector2 localMax = offset + new Vector2(width * tileSize.x, height * tileSize.y);

                // Переводим в мировые координаты и нормализуем по осям
                Vector3 worldMin3 = grid.transform.TransformPoint(localMin);
                Vector3 worldMax3 = grid.transform.TransformPoint(localMax);

                Vector2 rawMin = new Vector2(
                    Mathf.Min(worldMin3.x, worldMax3.x),
                    Mathf.Min(worldMin3.y, worldMax3.y));
                Vector2 rawMax = new Vector2(
                    Mathf.Max(worldMin3.x, worldMax3.x),
                    Mathf.Max(worldMin3.y, worldMax3.y));

                // Фиксированный офсет, уменьшающий доступную область
                const float padding = 10f;
                min = rawMin + new Vector2(padding, padding);
                max = rawMax - new Vector2(padding, padding);

                return (max.x - min.x) >= 0.1f && (max.y - min.y) >= 0.1f;
            }

            return false;
        }

        /// <summary>
        /// Возвращает первый доступный GrassGrid для вычисления границ уровня.
        /// </summary>
        private GrassGridInstanced GetPrimaryGrassGrid()
        {
            if (grassFields == null)
            {
                return null;
            }

            foreach (var field in grassFields)
            {
                if (field == null)
                {
                    continue;
                }

                var grid = field.GrassGrid;
                if (grid != null)
                {
                    return grid;
                }
            }

            return null;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Отрисовываем spawn point
            if (playerSpawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(playerSpawnPoint.position, 0.5f);
                Gizmos.DrawLine(playerSpawnPoint.position, playerSpawnPoint.position + Vector3.up);
            }
            
            // Отрисовываем границы уровня
            if (levelBounds != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(levelBounds.bounds.center, levelBounds.bounds.size);
            }

            // Отрисовываем точки для границ камеры
            if (cameraMinPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(cameraMinPoint.position, 0.4f);
            }

            if (cameraMaxPoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(cameraMaxPoint.position, 0.4f);
            }
        }
#endif
    }
}

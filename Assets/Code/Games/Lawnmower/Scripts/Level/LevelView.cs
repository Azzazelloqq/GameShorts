using UnityEngine;
using System.Collections.Generic;

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
        
        private void Awake()
        {
            // Находим все поля травы, если не назначены вручную
            if (grassFields == null || grassFields.Length == 0)
            {
                grassFields = GetComponentsInChildren<GrassFieldView>();
            }
            
            // Находим spawn point, если не назначен
            if (playerSpawnPoint == null)
            {
                var spawnObject = transform.Find("PlayerSpawn");
                if (spawnObject != null)
                    playerSpawnPoint = spawnObject;
                else
                    playerSpawnPoint = transform; // Используем центр уровня
            }
            
            // Находим bounds, если не назначены
            if (levelBounds == null)
            {
                levelBounds = GetComponent<Collider2D>();
            }
        }
        
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
        }
#endif
    }
}

using UnityEngine;

namespace GameShorts.FlyHumans.View
{
    /// <summary>
    /// Представляет блок мира с точками сцепки (View компонент)
    /// </summary>
    public class WorldBlock : MonoBehaviour
    {
        [Header("Attachment Points")]
        [SerializeField] private Transform _startPoint;  // Точка сцепки в начале блока
        [SerializeField] private Transform _endPoint;    // Точка сцепки в конце блока
        
        [Header("Trigger Settings")]
        [SerializeField] private float _spawnNextBlockDistance = 20f; // На каком расстоянии от конца блока спавнить следующий
        
        [Header("Traffic")]
        [SerializeField] private BlockTrafficView _trafficView; // Трафик на этом блоке (необязательно)
        
        private bool _hasSpawnedNext = false;
        [SerializeField] private bool _showGizmos;

        // Properties
        public Transform StartPoint => _startPoint;
        public Transform EndPoint => _endPoint;
        public float SpawnNextBlockDistance => _spawnNextBlockDistance;
        public BlockTrafficView TrafficView => _trafficView;
        public bool HasSpawnedNext 
        { 
            get => _hasSpawnedNext; 
            set => _hasSpawnedNext = value; 
        }
        
        /// <summary>
        /// Проверяет, нужно ли спавнить следующий блок на основе позиции персонажа
        /// </summary>
        public bool ShouldSpawnNext(Vector3 characterPosition)
        {
            if (_hasSpawnedNext) return false;
            
            // Вычисляем расстояние от персонажа до конца блока
            float distanceToEnd = Vector3.Distance(characterPosition, _endPoint.position);
            
            return distanceToEnd <= _spawnNextBlockDistance;
        }
        
        /// <summary>
        /// Выставляет блок в позицию, выравнивая его начальную точку с target
        /// </summary>
        public void AlignStartPointTo(Vector3 targetPosition)
        {
            Vector3 offset = targetPosition - _startPoint.position;
            transform.position += offset;
        }
        
        private void OnDrawGizmos()
        {
            if (!_showGizmos)
                return;
            // Визуализация точек сцепки в редакторе
            if (_startPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_startPoint.position, 0.3f);
                Gizmos.DrawLine(_startPoint.position, _startPoint.position + Vector3.up * 2f);
            }
            
            if (_endPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(_endPoint.position, 0.3f);
                Gizmos.DrawLine(_endPoint.position, _endPoint.position + Vector3.up * 2f);
                
                // Визуализация расстояния спавна следующего блока
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_endPoint.position, _spawnNextBlockDistance);
            }
        }
    }
}


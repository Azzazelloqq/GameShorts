using UnityEngine;

namespace GameShorts.FlyHumans.View
{
    /// <summary>
    /// Определяет путь для движения машин (View компонент)
    /// </summary>
    internal class TrafficPath : MonoBehaviour
    {
        [Header("Path Settings")]
        [Tooltip("Точки пути. Машины будут двигаться от первой к последней")]
        [SerializeField] private Transform[] _waypoints;

        [Header("Visualization")]
        [Tooltip("Показывать путь в редакторе")]
        [SerializeField] private bool _showPath = true;
        
        [Tooltip("Цвет линии пути")]
        [SerializeField] private Color _pathColor = Color.yellow;

        public Transform[] Waypoints => _waypoints;
        public int WaypointCount => _waypoints?.Length ?? 0;

        private void OnDrawGizmos()
        {
            if (!_showPath || _waypoints == null || _waypoints.Length < 2)
                return;

            Gizmos.color = _pathColor;
            
            for (int i = 0; i < _waypoints.Length - 1; i++)
            {
                if (_waypoints[i] != null && _waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(_waypoints[i].position, _waypoints[i + 1].position);
                    Gizmos.DrawWireSphere(_waypoints[i].position, 0.3f);
                }
            }
            
            // Рисуем последнюю точку
            if (_waypoints[_waypoints.Length - 1] != null)
            {
                Gizmos.DrawWireSphere(_waypoints[_waypoints.Length - 1].position, 0.3f);
            }
        }

        /// <summary>
        /// Получить позицию точки пути по индексу
        /// </summary>
        public Vector3 GetWaypointPosition(int index)
        {
            if (_waypoints == null || index < 0 || index >= _waypoints.Length || _waypoints[index] == null)
                return Vector3.zero;
            
            return _waypoints[index].position;
        }
    }
}


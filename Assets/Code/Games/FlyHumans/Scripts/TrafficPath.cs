using UnityEngine;

namespace FlyHumans
{
    /// <summary>
    /// Определяет путь для движения машин с помощью точек (waypoints)
    /// </summary>
    public class TrafficPath : MonoBehaviour
    {
        [Header("Path Settings")]
        [Tooltip("Точки пути. Машины будут двигаться от первой к последней")]
        public Transform[] waypoints;

        [Header("Visualization")]
        [Tooltip("Показывать путь в редакторе")]
        public bool showPath = true;
        
        [Tooltip("Цвет линии пути")]
        public Color pathColor = Color.yellow;

        private void OnDrawGizmos()
        {
            if (!showPath || waypoints == null || waypoints.Length < 2)
                return;

            Gizmos.color = pathColor;
            
            for (int i = 0; i < waypoints.Length - 1; i++)
            {
                if (waypoints[i] != null && waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
                }
            }
            
            // Рисуем последнюю точку
            if (waypoints[waypoints.Length - 1] != null)
            {
                Gizmos.DrawWireSphere(waypoints[waypoints.Length - 1].position, 0.3f);
            }
        }

        /// <summary>
        /// Получить позицию точки пути по индексу
        /// </summary>
        public Vector3 GetWaypointPosition(int index)
        {
            if (waypoints == null || index < 0 || index >= waypoints.Length || waypoints[index] == null)
                return Vector3.zero;
            
            return waypoints[index].position;
        }

        /// <summary>
        /// Получить количество точек в пути
        /// </summary>
        public int GetWaypointCount()
        {
            return waypoints != null ? waypoints.Length : 0;
        }
    }
}


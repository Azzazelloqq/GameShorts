using UnityEngine;

namespace FlyHumans
{
    /// <summary>
    /// Управляет движением одной машины по заданному пути
    /// </summary>
    public class VehicleMover : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Скорость движения машины")]
        public float speed = 5f;
        
        [Tooltip("Минимальное расстояние до точки для перехода к следующей")]
        public float waypointThreshold = 0.5f;

        [Header("References")]
        [Tooltip("Путь, по которому движется машина")]
        public TrafficPath path;

        private int currentWaypointIndex = 0;
        private bool isMoving = true;

        private void Update()
        {
            if (!isMoving || path == null || path.GetWaypointCount() == 0)
                return;

            MoveTowardsCurrentWaypoint();
        }

        private void MoveTowardsCurrentWaypoint()
        {
            Vector3 targetPosition = path.GetWaypointPosition(currentWaypointIndex);
            Vector3 direction = (targetPosition - transform.position).normalized;
            
            // Движение к цели
            transform.position += direction * speed * Time.deltaTime;
            
            // Поворот в сторону движения
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 7f);
            }

            // Проверка достижения точки
            float distance = Vector3.Distance(transform.position, targetPosition);
            if (distance < waypointThreshold)
            {
                currentWaypointIndex++;
                
                // Если достигли конца пути
                if (currentWaypointIndex >= path.GetWaypointCount())
                {
                    OnPathCompleted();
                }
            }
        }

        private void OnPathCompleted()
        {
            // Останавливаем движение, контроллер перезапустит через заданное время
            isMoving = false;
            currentWaypointIndex = 0;
        }

        /// <summary>
        /// Запустить движение по пути
        /// </summary>
        public void StartMoving()
        {
            if (path == null || path.GetWaypointCount() == 0)
            {
                Debug.LogWarning($"Vehicle {gameObject.name}: путь не задан или пуст!");
                return;
            }

            isMoving = true;
            currentWaypointIndex = 0;
            
            // Устанавливаем машину в начальную позицию
            transform.position = path.GetWaypointPosition(0);
        }

        /// <summary>
        /// Остановить движение
        /// </summary>
        public void StopMoving()
        {
            isMoving = false;
        }

        /// <summary>
        /// Проверка, движется ли машина сейчас
        /// </summary>
        public bool IsMoving()
        {
            return isMoving;
        }
    }
}


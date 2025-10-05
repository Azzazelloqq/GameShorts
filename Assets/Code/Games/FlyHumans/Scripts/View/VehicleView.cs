using UnityEngine;

namespace GameShorts.FlyHumans.View
{
    /// <summary>
    /// View компонент для одной машины (только визуализация)
    /// </summary>
    public class VehicleView : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Минимальное расстояние до точки для перехода к следующей")]
        [SerializeField] private float _waypointThreshold = 0.5f;

        public float WaypointThreshold => _waypointThreshold;
        
        /// <summary>
        /// Переместить машину в заданную позицию
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
        
        /// <summary>
        /// Повернуть машину в заданном направлении
        /// </summary>
        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }
        
        /// <summary>
        /// Плавно повернуть машину в направлении движения
        /// </summary>
        public void SmoothRotate(Vector3 direction, float deltaTime, float rotationSpeed = 5f)
        {
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, deltaTime * rotationSpeed);
            }
        }
    }
}


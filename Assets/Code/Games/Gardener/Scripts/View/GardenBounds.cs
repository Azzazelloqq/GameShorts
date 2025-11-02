using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;

namespace GameShorts.Gardener.View
{
    /// <summary>
    /// Компонент, определяющий границы огорода
    /// Используется для проверки валидности размещения грядок
    /// </summary>
    public class GardenBounds : BaseMonoBehaviour
    {
        [SerializeField] private BoxCollider _boundsCollider;
        [SerializeField] private Vector3 _minBounds = new Vector3(-5, 0, -5);
        [SerializeField] private Vector3 _maxBounds = new Vector3(5, 0, 5);
        
        
        /// <summary>
        /// Проверяет, находится ли точка в пределах границ огорода
        /// </summary>
        public bool IsWithinBounds(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            
            return localPosition.x >= _minBounds.x && localPosition.x <= _maxBounds.x &&
                   localPosition.z >= _minBounds.z && localPosition.z <= _maxBounds.z;
        }
        
        /// <summary>
        /// Ограничивает позицию границами огорода
        /// </summary>
        public Vector3 ClampToBounds(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
            
            localPosition.x = Mathf.Clamp(localPosition.x, _minBounds.x, _maxBounds.x);
            localPosition.y = 0; // Грядки всегда на уровне земли
            localPosition.z = Mathf.Clamp(localPosition.z, _minBounds.z, _maxBounds.z);
            
            return transform.TransformPoint(localPosition);
        }
        
        /// <summary>
        /// Визуализация границ в редакторе
        /// </summary>
        // private void OnDrawGizmos()
        // {
        //     Gizmos.color = Color.green;
        //     Vector3 min = transform.TransformPoint(_minBounds);
        //     Vector3 max = transform.TransformPoint(_maxBounds);
        //     
        //     Vector3 size = max - min;
        //     Vector3 center = (min + max) / 2f;
        //     
        //     Gizmos.DrawWireCube(center, size);
        // }
        public void Init()
        {
            var bounds = _boundsCollider.bounds;
            _minBounds = transform.InverseTransformPoint(bounds.min);
            _maxBounds = transform.InverseTransformPoint(bounds.max);
        }
    }
}


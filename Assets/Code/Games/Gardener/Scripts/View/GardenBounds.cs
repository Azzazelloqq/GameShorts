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
        private Vector3 _minBounds = new Vector3(-5, 0, -5);
        private Vector3 _maxBounds = new Vector3(5, 0, 5);
        private bool _isInitialized = false;
        
        
        /// <summary>
        /// Проверяет, находится ли точка в пределах границ огорода
        /// </summary>
        public bool IsWithinBounds(Vector3 worldPosition)
        {
            if (!_isInitialized)
            {
                Init();
            }
            
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
        
        public void Init()
        {
            if (_boundsCollider == null)
            {
                Debug.LogError("GardenBounds: _boundsCollider is null!");
                return;
            }
            
            // Используем локальные размеры и центр коллайдера
            // вместо мировых bounds для избежания проблем с rotation/scale
            Vector3 colliderCenter = _boundsCollider.center;
            Vector3 colliderSize = _boundsCollider.size;
            
            _minBounds = colliderCenter - colliderSize / 2f;
            _maxBounds = colliderCenter + colliderSize / 2f;
            _isInitialized = true;
        }
    }
}


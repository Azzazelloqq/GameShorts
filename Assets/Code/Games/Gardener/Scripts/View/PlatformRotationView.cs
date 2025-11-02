using System;
using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameShorts.Gardener.View
{
    /// <summary>
    /// View компонент для обработки ввода пользователя для вращения платформы
    /// Реализует интерфейсы EventSystem для получения событий от мыши/тача
    /// </summary>
    internal class PlatformRotationView : BaseMonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogs = false;
        
        /// <summary>
        /// Событие вызывается при начале зажатия
        /// </summary>
        public event Action OnDragStarted;
        
        /// <summary>
        /// Событие вызывается при движении с зажатой кнопкой
        /// Передает дельту движения в пикселях
        /// </summary>
        public event Action<Vector2> OnDragDelta;
        
        /// <summary>
        /// Событие вызывается при отпускании кнопки
        /// </summary>
        public event Action OnDragEnded;
        
        private Vector2 _lastPointerPosition;
        private bool _isDragging;

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            _lastPointerPosition = eventData.position;
            
            if (_enableDebugLogs)
            {
                Debug.Log($"[PlatformRotationView] OnPointerDown at {eventData.position}");
            }
            
            OnDragStarted?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;
            
            Vector2 currentPosition = eventData.position;
            Vector2 delta = currentPosition - _lastPointerPosition;
            _lastPointerPosition = currentPosition;
            
            if (_enableDebugLogs && delta.sqrMagnitude > 1f) // Логируем только если есть заметное движение
            {
                Debug.Log($"[PlatformRotationView] OnDrag delta: {delta}");
            }
            
            OnDragDelta?.Invoke(delta);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging)
                return;
            
            if (_enableDebugLogs)
            {
                Debug.Log($"[PlatformRotationView] OnPointerUp");
            }
            
            _isDragging = false;
            OnDragEnded?.Invoke();
        }
    }
}


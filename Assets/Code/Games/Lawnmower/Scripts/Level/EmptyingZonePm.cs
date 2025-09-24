using System;
using UnityEngine;
using Code.Core.BaseDMDisposable.Scripts;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Level
{
    /// <summary>
    /// Presenter для управления зоной опустошения контейнера
    /// </summary>
    internal class EmptyingZonePm : BaseDisposable
    {
        internal struct Ctx
        {
            public EmptyingZoneView view;
            public string zoneName;
            public Color normalColor;
            public Color activeColor;
        }

        private readonly Ctx _ctx;
        
        // Events
        public event Action<GameObject> OnPlayerEntered;
        public event Action<GameObject> OnPlayerExited;

        public EmptyingZonePm(Ctx ctx)
        {
            _ctx = ctx;
            
            // Инициализируем View
            _ctx.view.SetCtx(new EmptyingZoneView.Ctx
            {
                zoneName = _ctx.zoneName,
                normalColor = _ctx.normalColor,
                activeColor = _ctx.activeColor
            });
            
            // Подписываемся на события коллайдера
            var triggerHandler = _ctx.view.gameObject.GetComponent<EmptyingZoneTriggerHandler>();
            if (triggerHandler == null)
            {
                triggerHandler = _ctx.view.gameObject.AddComponent<EmptyingZoneTriggerHandler>();
            }
            
            triggerHandler.OnTriggerEntered += HandlePlayerEntered;
            triggerHandler.OnTriggerExited += HandlePlayerExited;
            
            Debug.Log($"EmptyingZonePm initialized for zone: {_ctx.zoneName}");
        }

        protected override void OnDispose()
        {
            // Отписываемся от событий
            if (_ctx.view != null)
            {
                var triggerHandler = _ctx.view.gameObject.GetComponent<EmptyingZoneTriggerHandler>();
                if (triggerHandler != null)
                {
                    triggerHandler.OnTriggerEntered -= HandlePlayerEntered;
                    triggerHandler.OnTriggerExited -= HandlePlayerExited;
                }
            }
            
            base.OnDispose();
        }

        private void HandlePlayerEntered(Collider2D other)
        {
            Debug.Log($"Trigger entered by: {other.name} with tag: {other.tag}");
            
            // Проверяем, что это игрок
            if (other.CompareTag("Player"))
            {
                _ctx.view.SetPlayerInside(true);
                OnPlayerEntered?.Invoke(other.gameObject);
                
                Debug.Log($"Player entered emptying zone: {_ctx.zoneName}");
            }
        }

        private void HandlePlayerExited(Collider2D other)
        {
            // Проверяем, что это игрок
            if (other.CompareTag("Player"))
            {
                _ctx.view.SetPlayerInside(false);
                OnPlayerExited?.Invoke(other.gameObject);
                
                Debug.Log($"Player exited emptying zone: {_ctx.zoneName}");
            }
        }

        // Публичные методы для доступа к View
        public bool IsPlayerInside => _ctx.view.IsPlayerInside;
        public string ZoneName => _ctx.view.ZoneName;
        public bool IsPositionInZone(Vector3 worldPosition) => _ctx.view.IsPositionInZone(worldPosition);
        public Vector3 GetZoneCenter() => _ctx.view.GetZoneCenter();
        public Vector3 GetZoneSize() => _ctx.view.GetZoneSize();
    }
}

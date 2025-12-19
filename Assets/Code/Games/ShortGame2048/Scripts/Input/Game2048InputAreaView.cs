using System;
using Disposable;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.Games
{
    internal class Game2048InputAreaView : MonoBehaviourDisposable, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        internal struct Ctx
        {
            public Action<Vector2> onPointerDown;
            public Action<Vector2> onPointerMove;
            public Action<Vector2> onPointerUp;
        }

        private Ctx _ctx;
        private Camera _camera;

        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
            _camera = Camera.main;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Передаём экранные координаты без перевода
            _ctx.onPointerDown?.Invoke(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Передаём экранные координаты без перевода
            _ctx.onPointerMove?.Invoke(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Передаём экранные координаты без перевода
            _ctx.onPointerUp?.Invoke(eventData.position);
        }

        private Vector2 GetWorldPosition(Vector2 screenPosition)
        {
            if (_camera == null)
                return screenPosition;
                
            return _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, _camera.nearClipPlane));
        }
    }
}

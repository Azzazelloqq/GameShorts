using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.Games.FruitSlasher.Scripts.Input
{
    public class InputAreaView: MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public struct Ctx
        {
            public Action<Vector2> onPointerDown;
            public Action<Vector2> onPointerMove;
            public Action<Vector2> onPointerUp;
        }

        private Ctx _ctx;

        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
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
    }
}
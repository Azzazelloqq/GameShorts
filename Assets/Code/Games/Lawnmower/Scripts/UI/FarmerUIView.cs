using UnityEngine;
using Code.Core.BaseDMDisposable.Scripts;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.UI
{
    /// <summary>
    /// View компонент для UI фермера
    /// </summary>
    internal class FarmerUIView : BaseMonoBehaviour
    {
        internal struct Ctx
        {
            public Canvas targetCanvas;
            public UnityEngine.Camera worldCamera;
            public Vector2 offset;
        }

        [Header("Container UI")]
        [SerializeField] private GrassContainerView containerView;

        private Ctx _ctx;
        private RectTransform _rectTransform;

        public GrassContainerView ContainerView => containerView;

        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
            _rectTransform = GetComponent<RectTransform>();
        }

        public void UpdatePosition(Vector2 playerWorldPosition)
        {
            if (_rectTransform == null || _ctx.worldCamera == null || _ctx.targetCanvas == null)
                return;

            // Конвертируем мировую позицию игрока в экранную позицию
            Vector3 worldPos = new Vector3(playerWorldPosition.x, playerWorldPosition.y, 0);
            Vector2 screenPosition = ConvertWorldToScreenPosition(worldPos);

            // Добавляем смещение и устанавливаем позицию
            _rectTransform.anchoredPosition = screenPosition + _ctx.offset;
        }

        private Vector2 ConvertWorldToScreenPosition(Vector3 worldPosition)
        {
            if (_ctx.worldCamera == null || _ctx.targetCanvas == null)
                return Vector2.zero;

            // Конвертируем мировую позицию в экранную
            Vector3 screenPoint = _ctx.worldCamera.WorldToScreenPoint(worldPosition);

            // Конвертируем экранную позицию в локальную позицию канваса
            RectTransform canvasRectTransform = _ctx.targetCanvas.GetComponent<RectTransform>();

            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPoint,
                _ctx.targetCanvas.worldCamera,
                out localPoint
            );

            return localPoint;
        }
    }
}

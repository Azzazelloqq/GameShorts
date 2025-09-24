using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Code.Core.BaseDMDisposable.Scripts;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.UI
{
    /// <summary>
    /// View компонент для отображения состояния контейнера травы
    /// </summary>
    internal class GrassContainerView : BaseMonoBehaviour
    {
        internal struct Ctx
        {
            // Контекст пустой, так как View только отображает данные
        }

        [Header("Container UI")]
        [SerializeField] private Slider containerSlider;

        private Ctx _ctx;

        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
            
            // Инициализируем слайдер
            if (containerSlider != null)
            {
                containerSlider.minValue = 0f;
                containerSlider.maxValue = 1f;
                containerSlider.value = 0f;
            }
        }

        public void UpdateContainer(float currentAmount, float maxCapacity)
        {
            // Обновляем слайдер
            if (containerSlider != null)
            {
                float fillPercentage = maxCapacity > 0 ? currentAmount / maxCapacity : 0f;
                containerSlider.value = fillPercentage;
            }
        }
    }
}

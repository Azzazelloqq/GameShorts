using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Code.Core.BaseDMDisposable.Scripts;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.UI
{
    /// <summary>
    /// View компонент для основного игрового UI
    /// </summary>
    internal class MainGameUIView : BaseMonoBehaviour
    {
        internal struct Ctx
        {
            // Контекст пустой, так как View только отображает данные
        }

        [Header("Level Progress UI")]
        [SerializeField] private Slider levelProgressSlider;
        [SerializeField] private TextMeshProUGUI levelProgressText;
        [SerializeField] private TextMeshProUGUI levelNameText;

        private Ctx _ctx;

        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
            
            // Инициализируем слайдер прогресса уровня
            if (levelProgressSlider != null)
            {
                levelProgressSlider.minValue = 0f;
                levelProgressSlider.maxValue = 1f;
                levelProgressSlider.value = 0f;
            }
        }

        public void UpdateLevelProgress(float progressPercentage, string levelName)
        {
            // Обновляем слайдер прогресса
            if (levelProgressSlider != null)
            {
                levelProgressSlider.value = Mathf.Clamp01(progressPercentage);
            }
            
            // Обновляем текст прогресса
            if (levelProgressText != null)
            {
                levelProgressText.text = $"{progressPercentage * 100f:F0}%";
            }
            
            // Обновляем название уровня
            if (levelNameText != null)
            {
                levelNameText.text = levelName;
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}

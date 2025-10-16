using TMPro;
using UnityEngine;

namespace Lightseeker
{
    internal class LightseekerMainUIView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _levelText;
        
        [SerializeField]
        private TextMeshProUGUI _starsText;

        public void UpdateLevel(int currentLevel, int maxLevel)
        {
            if (_levelText != null)
            {
                _levelText.text = $"Level: {currentLevel}/{maxLevel}";
            }
        }

        public void UpdateStars(int collectedStars, int maxStars)
        {
            if (_starsText != null)
            {
                _starsText.text = $"Stars: {collectedStars}/{maxStars}";
            }
        }
    }
}


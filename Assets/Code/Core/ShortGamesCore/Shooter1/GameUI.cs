using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shooter1
{
    /// <summary>
    /// UI компонент для игры Shooter1
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Canvas uiCanvas;

        private int score = 0;

        public void UpdateTimer(float remainingTime)
        {
            if (timerText != null)
            {
                timerText.text = $"Time: {Mathf.Ceil(remainingTime):F0}";
            }
        }

        public void AddScore(int points = 1)
        {
            score += points;
            if (scoreText != null)
            {
                scoreText.text = $"Hits: {score}";
            }
        }

        public void ResetScore()
        {
            score = 0;
            if (scoreText != null)
            {
                scoreText.text = "Hits: 0";
            }
        }

        public int GetScore()
        {
            return score;
        }
    }
}



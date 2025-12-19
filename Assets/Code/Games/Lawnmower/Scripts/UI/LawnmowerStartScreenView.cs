using Disposable;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.UI
{
    internal class LawnmowerStartScreenView : MonoBehaviourDisposable
    {
        [Header("UI Elements")]
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text instructionText;

        public Button StartButton => startButton;
        public TMP_Text TitleText => titleText;
        public TMP_Text InstructionText => instructionText;

        internal struct Ctx
        {
        }

        public void SetCtx(Ctx ctx)
        {
            if (titleText != null)
            {
                titleText.text = "Lawnmower";
            }
            
            if (instructionText != null)
            {
                instructionText.text = "Tap to Start";
            }
        }
    }
}


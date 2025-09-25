using Code.Core.BaseDMDisposable.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.UI
{
    internal class StartScreenView : BaseMonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text instructionText;

        public Button StartButton => startButton;
        public TMP_Text InstructionText => instructionText;

        internal struct Ctx
        {
        }

        public void SetCtx(Ctx ctx)
        {
            if (instructionText != null)
            {
                instructionText.text = "Tap to Start";
            }
        }
    }
}

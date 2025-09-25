using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.UI
{
    internal class StartScreenView : BaseMonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button startButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text instructionText;

        public Button StartButton => startButton;
        public Text TitleText => titleText;
        public Text InstructionText => instructionText;

        internal struct Ctx
        {
            // Контекст для инициализации, если потребуется
        }

        public void SetCtx(Ctx ctx)
        {
            // Инициализация View компонента
            if (titleText != null)
            {
                titleText.text = "ESCAPE FROM DARK";
            }
            
            if (instructionText != null)
            {
                instructionText.text = "Tap to Start";
            }
            
            Debug.Log("StartScreenView: Context set");
        }
    }
}

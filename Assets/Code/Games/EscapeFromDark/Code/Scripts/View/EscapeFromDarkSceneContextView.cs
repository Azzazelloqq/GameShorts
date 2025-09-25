using UnityEngine;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Player;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View
{
    internal class EscapeFromDarkSceneContextView : BaseMonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private EscapeFromDarkPlayerView playerPrefab;
        
        [Header("UI")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private Transform startScreenParent;
        [SerializeField] private Transform finishScreenParent;
        
        [Header("Input")]
        [SerializeField] private FixedJoystick joystick;
        
        [Header("Camera")]
        [SerializeField] private UnityEngine.Camera mainCamera;
        
        [Header("Level")]
        [SerializeField] private Transform levelParent;
        
        // Properties
        public EscapeFromDarkPlayerView PlayerPrefab => playerPrefab;
        public Canvas UiCanvas => uiCanvas;
        public Transform StartScreenParent => startScreenParent;
        public Transform FinishScreenParent => finishScreenParent;
        public FixedJoystick Joystick => joystick;
        public UnityEngine.Camera MainCamera => mainCamera;
        public Transform LevelParent => levelParent;
        
        public struct Ctx
        {
            // Контекст для инициализации View, если потребуется
        }

        public void SetCtx(Ctx ctx)
        {
            // Инициализация View компонента через контекст
            Debug.Log("EscapeFromDarkSceneContextView: Context set");
        }
    }
}

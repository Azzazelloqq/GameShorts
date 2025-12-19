using UnityEngine;
using Disposable;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Player;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.UI;
using UnityEngine.Serialization;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View
{
    internal class EscapeFromDarkSceneContextView : MonoBehaviourDisposable
    {
        [Header("Player")]
        [SerializeField] private EscapeFromDarkPlayerView playerPrefab;
        
        [Header("UI")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private StartScreenView startScreenView;
        [SerializeField] private Transform finishScreenParent;
        
        [Header("Input")]
        [SerializeField] private FixedJoystick joystick;
        
        [Header("Camera")]
        [SerializeField] private UnityEngine.Camera mainCamera;
        
        [Header("Level")]
        [SerializeField] private EscapeFromDarkLevelView _levelView;
        
        // Properties
        public EscapeFromDarkPlayerView PlayerPrefab => playerPrefab;
        public Canvas UiCanvas => uiCanvas;
        public StartScreenView StartScreenView => startScreenView;
        public Transform FinishScreenParent => finishScreenParent;
        public FixedJoystick Joystick => joystick;
        public UnityEngine.Camera MainCamera => mainCamera;
        public EscapeFromDarkLevelView LevelView => _levelView;
        
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

using UnityEngine;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Player;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Level;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.UI;
using UnityEngine.Serialization;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.View
{
    internal class LawnmowerSceneContextView : MonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private PlayerView playerPrefab;
        
        [Header("Level")]
        [SerializeField] private LevelView[] levels;
        [SerializeField] private int currentLevelIndex = 0;
        
        [Header("UI")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private LawnmowerStartScreenView startScreenView;
        [SerializeField] private Transform startScreenParent;
        [SerializeField] private MainGameUIView mainUi;
        [SerializeField] private Transform finishScreenParent;
        
        [Header("Input")]
        [SerializeField] private FixedJoystick joystick;
        
        [Header("Camera")]
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private LawnmowerCameraSettings cameraSettingsAsset;
        
        [Header("Player Settings")]
        [SerializeField] private LawnmowerPlayerSettings playerSettingsAsset;
        
        public PlayerView PlayerPrefab => playerPrefab;
        public LevelView[] Levels => levels;
        public LevelView CurrentLevel => currentLevelIndex < levels.Length ? levels[currentLevelIndex] : null;
        public int CurrentLevelIndex => currentLevelIndex;
        public Canvas UiCanvas => uiCanvas;
        public LawnmowerStartScreenView StartScreenView => startScreenView;
        public Transform StartScreenParent => startScreenParent;
        public MainGameUIView MainUi => mainUi;
        public Transform FinishScreenParent => finishScreenParent;
        public FixedJoystick Joystick => joystick;
        public UnityEngine.Camera MainCamera => mainCamera;
        public LawnmowerCameraSettings CameraSettingsAsset => cameraSettingsAsset;
        public LawnmowerPlayerSettings PlayerSettingsAsset => playerSettingsAsset;
        
        public void SetCurrentLevel(int levelIndex)
        {
            if (levelIndex >= 0 && levelIndex < levels.Length)
            {
                // Деактивируем текущий уровень
                if (currentLevelIndex < levels.Length)
                {
                    levels[currentLevelIndex].gameObject.SetActive(false);
                }
                
                currentLevelIndex = levelIndex;
                
                // Активируем новый уровень
                levels[currentLevelIndex].gameObject.SetActive(true);
            }
        }
        
        public void NextLevel()
        {
            if (currentLevelIndex + 1 < levels.Length)
            {
                SetCurrentLevel(currentLevelIndex + 1);
            }
        }
    }
}

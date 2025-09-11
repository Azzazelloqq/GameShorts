using UnityEngine;
using Code.Core.GameSwiper;
using Code.Core.ShortGamesCore.Source.GameCore;

namespace Code.Core.GameEntryPoint
{
    /// <summary>
    /// Простой тестер для проверки работы GameSwiper через контроллер
    /// </summary>
    public class GameSwiperTester : MonoBehaviour
    {
        [Header("Testing")]
        [SerializeField] private bool _enableKeyboardTesting = true;
        [SerializeField] private bool _enableControllerTesting = true;
        
        private ISwiperGame _gameSwiper;
        private GameSwiperController _controller;
        
        private void Start()
        {
            var entryPoint = FindObjectOfType<GameEntryPoint>();
            if (entryPoint != null)
            {
                Debug.Log("GameSwiperTester: GameEntryPoint found, waiting for initialization...");
            }
        }
        
        private void Update()
        {
            if (!_enableKeyboardTesting) return;
            
            // Тестирование с клавиатуры
            if (Input.GetKeyDown(KeyCode.N))
            {
                Debug.Log("GameSwiperTester: Next game requested via keyboard");
                TestNextGame();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                Debug.Log("GameSwiperTester: Previous game requested via keyboard");
                TestPreviousGame();
            }
            else if (Input.GetKeyDown(KeyCode.C) && _enableControllerTesting)
            {
                Debug.Log("GameSwiperTester: Testing controller connection");
                TestControllerConnection();
            }
        }
        
        public void SetGameSwiper(ISwiperGame gameSwiper)
        {
            _gameSwiper = gameSwiper;
            Debug.Log("GameSwiperTester: GameSwiper set successfully");
        }
        
        public void SetController(GameSwiperController controller)
        {
            _controller = controller;
            Debug.Log("GameSwiperTester: GameSwiperController set successfully");
        }
        
        private async void TestNextGame()
        {
            if (_gameSwiper == null)
            {
                Debug.LogWarning("GameSwiperTester: GameSwiper is not set");
                return;
            }
            
            try
            {
                var nextGame = await _gameSwiper.NextGameAsync();
                if (nextGame != null)
                {
                    Debug.Log($"GameSwiperTester: Successfully switched to next game: {nextGame.GetType().Name}");
                }
                else
                {
                    Debug.LogWarning("GameSwiperTester: Failed to switch to next game");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameSwiperTester: Error switching to next game: {ex.Message}");
            }
        }
        
        private async void TestPreviousGame()
        {
            if (_gameSwiper == null)
            {
                Debug.LogWarning("GameSwiperTester: GameSwiper is not set");
                return;
            }
            
            try
            {
                var previousGame = await _gameSwiper.PreviousGameAsync();
                if (previousGame != null)
                {
                    Debug.Log($"GameSwiperTester: Successfully switched to previous game: {previousGame.GetType().Name}");
                }
                else
                {
                    Debug.LogWarning("GameSwiperTester: Failed to switch to previous game");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameSwiperTester: Error switching to previous game: {ex.Message}");
            }
        }
        
        private void TestControllerConnection()
        {
            if (_controller == null)
            {
                Debug.LogWarning("GameSwiperTester: Controller is not set");
                return;
            }
            
            Debug.Log("GameSwiperTester: Controller is connected and working");
        }
    }
}

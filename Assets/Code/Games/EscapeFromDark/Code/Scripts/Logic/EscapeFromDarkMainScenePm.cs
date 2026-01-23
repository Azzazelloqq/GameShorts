using System;
using System.Threading;
using Disposable;
using Code.Core.InputManager;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.UI;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Player;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Camera;
using Code.Core.Tools;
using LightDI.Runtime;
using UnityEngine;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Logic
{
    internal class EscapeFromDarkMainScenePm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public EscapeFromDarkSceneContextView sceneContextView;
            public IReadOnlyReactiveTrigger startGame;
        }

        private readonly Ctx _ctx;
        private readonly IInputManager _inputManager;
        private EscapeFromDarkGameState _currentState;
        
        private StartScreenPm _startScreenPm;
        private EscapeFromDarkPlayerPm _playerPm;
        private EscapeFromDarkLevelPm _levelPm;
        private EscapeFromDarkCameraPm _cameraPm;
        private int _currentLevel = 1;

        public EscapeFromDarkMainScenePm(Ctx ctx,
            [Inject] IInputManager inputManager)
        {
            _ctx = ctx;
            _inputManager = inputManager;
            _currentState = EscapeFromDarkGameState.WaitingToStart;
            AddDisposable(_ctx.startGame.SubscribeOnce(StartGame));
        }

        private void ShowStartScreen()
        {
            if (_currentState != EscapeFromDarkGameState.WaitingToStart)
                return;

            _currentState = EscapeFromDarkGameState.WaitingToStart;
            _inputManager?.SetJoystickOptions(AxisOptions.None);

            StartScreenPm.Ctx startScreenCtx = new StartScreenPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                startGameClicked = StartGame,
                cancellationToken = _ctx.cancellationToken
            };
            
            _startScreenPm = new StartScreenPm(startScreenCtx);
            AddDisposable(_startScreenPm);
            
            Debug.Log("EscapeFromDark: Start screen created");
        }

        private void StartGame()
        {
            if (_currentState != EscapeFromDarkGameState.WaitingToStart)
                return;
            
            if (_ctx.sceneContextView.Joystick == null)
            {
                Debug.LogError("EscapeFromDarkMainScenePm: Joystick is null in SceneContextView!");
            }
            else
            {
                _inputManager.Initialize(_ctx.sceneContextView.Joystick);
                Debug.Log("EscapeFromDarkMainScenePm: InputManager initialized with joystick");
            }

            _inputManager.SetJoystickOptions(AxisOptions.Both);
            _currentState = EscapeFromDarkGameState.Playing;

            // Убираем стартовый экран
            _startScreenPm?.Dispose();
            _startScreenPm = null;

            Debug.Log("EscapeFromDark: Game started!");
            
            // Создаем уровень
            CreateLevel();
            
            // Создаем игрока
            CreatePlayer();
            
            // Создаем камеру (с небольшой задержкой для инициализации игрока)
            CreateCamera();
            
            // Дополнительно фокусируем камеру на игроке после создания
            _cameraPm?.FocusOnPlayer();
        }

        private void CreateLevel()
        {
            EscapeFromDarkLevelPm.Ctx levelCtx = new EscapeFromDarkLevelPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                levelNumber = _currentLevel,
                onLevelCompleted = OnLevelCompleted,
                cancellationToken = _ctx.cancellationToken,
                playerTransform = _playerPm?.GetPlayerView()?.transform // Передаем Transform игрока если он есть
            };
            
            _levelPm = new EscapeFromDarkLevelPm(levelCtx);
            AddDisposable(_levelPm);
            
            Debug.Log($"EscapeFromDark: Level {_currentLevel} created");
        }

        private void CreatePlayer()
        {
            if (_levelPm == null)
            {
                Debug.LogError("EscapeFromDarkMainScenePm: Cannot create player without level!");
                return;
            }

            EscapeFromDarkPlayerPm.Ctx playerCtx = new EscapeFromDarkPlayerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                levelPm = _levelPm,
                onLevelCompleted = OnLevelCompleted,
                cancellationToken = _ctx.cancellationToken
            };
            
            _playerPm = EscapeFromDarkPlayerPmFactory.CreateEscapeFromDarkPlayerPm(playerCtx);
            AddDisposable(_playerPm);
            
            // Обновляем ссылку на игрока в ExitSpot
            _levelPm?.UpdatePlayerReference(_playerPm.GetPlayerView()?.transform);
            
            Debug.Log("EscapeFromDark: Player created");
        }

        private void CreateCamera()
        {
            if (_levelPm == null || _playerPm == null)
            {
                Debug.LogError("EscapeFromDarkMainScenePm: Cannot create camera without level and player!");
                return;
            }

            EscapeFromDarkCameraPm.Ctx cameraCtx = new EscapeFromDarkCameraPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                playerPm = _playerPm,
                levelPm = _levelPm,
                cancellationToken = _ctx.cancellationToken
            };
            
            _cameraPm = new EscapeFromDarkCameraPm(cameraCtx);
            AddDisposable(_cameraPm);
            
            Debug.Log("EscapeFromDark: Camera created");
        }

        private void OnLevelCompleted()
        {
            if (_currentState != EscapeFromDarkGameState.Playing)
                return;

            Debug.Log($"EscapeFromDark: Level {_currentLevel} completed!");
            
            // Переходим к следующему уровню
            _currentLevel++;
            
            // Очищаем текущие компоненты
            _cameraPm?.Dispose();
            _cameraPm = null;
            _playerPm?.Dispose();
            _playerPm = null;
            _levelPm?.Dispose();
            _levelPm = null;
            
            // Создаем новый уровень
            CreateLevel();
            CreatePlayer();
            CreateCamera();
            
            Debug.Log($"EscapeFromDark: Started level {_currentLevel}");
        }

        private void ShowFinishScreen()
        {
            if (_currentState != EscapeFromDarkGameState.Playing)
                return;

            _currentState = EscapeFromDarkGameState.Finished;
            _inputManager?.SetJoystickOptions(AxisOptions.None);

            // Очищаем игровые компоненты
            _playerPm?.Dispose();
            _playerPm = null;

            Debug.Log("EscapeFromDark: Game finished!");
            
            // TODO: Показать экран завершения игры с кнопкой рестарта
        }

        private void CompleteGame()
        {
            _currentState = EscapeFromDarkGameState.Completed;
            _inputManager.SetJoystickOptions(AxisOptions.None);
            
            Debug.Log("EscapeFromDark: Game completed successfully!");
        }
    }

    internal enum EscapeFromDarkGameState
    {
        WaitingToStart,
        Playing,
        Finished,
        Completed
    }
}

using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.View;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.UI;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Player;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Level;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Core;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Camera;
using LightDI;
using LightDI.Runtime;
using TickHandler;
using TickHandler.UnityTickHandler;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Logic
{
    internal class LawnmowerMainScenePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public LawnmowerSceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;
        private readonly IInputManager _inputManager;
        private LawnmowerStartScreenPm _startScreenPm;
        private LawnmowerPlayerPm _playerPm;
        private LawnmowerLevelManager _levelManager;
        private LawnmowerCameraPm _cameraPm;
        private LawnmowerGameState _currentState;

        public LawnmowerMainScenePm(Ctx ctx,
            [Inject] IInputManager inputManager)
        {
            _ctx = ctx;
            _inputManager = inputManager;
            
            if (_ctx.sceneContextView.Joystick == null)
            {
                Debug.LogError("LawnmowerMainScenePm: Joystick is null in SceneContextView!");
            }
            else
            {
                _inputManager.Initialize(_ctx.sceneContextView.Joystick);
                Debug.Log("LawnmowerMainScenePm: InputManager initialized with joystick");
            }
            
            _currentState = LawnmowerGameState.WaitingToStart;
            
            // Инициализируем менеджер уровней
            LawnmowerLevelManager.Ctx levelManagerCtx = new LawnmowerLevelManager.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken
            };
            _levelManager = new LawnmowerLevelManager(levelManagerCtx);
            AddDispose(_levelManager);
            
            ShowStartScreen();
        }

        private void ShowStartScreen()
        {
            if (_currentState != LawnmowerGameState.WaitingToStart)
                return;

            _currentState = LawnmowerGameState.WaitingToStart;
            _inputManager?.SetJoystickOptions(AxisOptions.None);

            LawnmowerStartScreenPm.Ctx startScreenCtx = new LawnmowerStartScreenPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                startGameClicked = StartGame,
                cancellationToken = _ctx.cancellationToken
            };
            
            _startScreenPm = new LawnmowerStartScreenPm(startScreenCtx);
            AddDispose(_startScreenPm);
            
            Debug.Log("Lawnmower: Start screen created");
        }

        private void InitializeCamera()
        {
            var cameraCtx = new LawnmowerCameraPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                playerPm = _playerPm,
                levelManager = _levelManager
            };
            
            _cameraPm = new LawnmowerCameraPm(cameraCtx);
            AddDispose(_cameraPm);
            
            Debug.Log("LawnmowerMainScenePm: Camera system initialized");
        }

        private void StartGame()
        {
            if (_currentState != LawnmowerGameState.WaitingToStart)
                return;

            _inputManager.SetJoystickOptions(AxisOptions.Both);
            _currentState = LawnmowerGameState.Playing;

            // Убираем стартовый экран
            _startScreenPm?.Dispose();
            _startScreenPm = null;

            Debug.Log("Lawnmower: Game started!");

            // Создаем игрока
            LawnmowerPlayerPm.Ctx playerCtx = new LawnmowerPlayerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                levelManager = _levelManager,
                onLevelCompleted = OnLevelCompleted,
                cancellationToken = _ctx.cancellationToken
            };
            
            _playerPm = LawnmowerPlayerPmFactory.CreateLawnmowerPlayerPm(playerCtx);
            AddDispose(_playerPm);
            
            // Создаем камеру
            InitializeCamera();
            
            // Начинаем первый уровень
            _levelManager.StartCurrentLevel();
        }

        private void OnLevelCompleted()
        {
            if (_currentState != LawnmowerGameState.Playing)
                return;

            // Проверяем, есть ли следующий уровень
            if (_levelManager.HasNextLevel())
            {
                _levelManager.NextLevel();
                _levelManager.StartCurrentLevel();
                _playerPm.ReinitializeForNewLevel(); // Переинициализируем контейнер для нового уровня
                _playerPm.ResetToSpawn();
            }
            else
            {
                // Игра завершена - все уровни пройдены
                CompleteGame();
            }
        }

        private void CompleteGame()
        {
            _currentState = LawnmowerGameState.Completed;
            _inputManager.SetJoystickOptions(AxisOptions.None);
            
            // TODO: Показать экран победы
            UnityEngine.Debug.Log("Lawnmower Game Completed! All levels finished!");
        }
    }
}

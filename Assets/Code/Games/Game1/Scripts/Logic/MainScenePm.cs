using System;
using System.Threading;
using Asteroids.Code.Games.Game1.Scripts.Logic;
using Asteroids.Code.Games.Game1.Scripts.View;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using LightDI.Runtime;
using Logic.Enemy;
using Logic.Player;
using Logic.UI;

namespace Code.Core.ShortGamesCore.Game1.Scripts.Logic
{
    internal class MainScenePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public MainSceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;
        private EntitiesControllerPm _entitiesController;
        private IDisposable _enemyManager;
        private IDisposable _mainScreen;
        private IDisposable _startScreen;
        private PlayerSpawnerPm _playerSpawnerPm;
        private GameState _currentState;
        private readonly IInputManager _inputManager;

        public MainScenePm(Ctx ctx,
            [Inject] IInputManager inputManager)
        {
            _ctx = ctx;
            _inputManager = inputManager;
            _inputManager.Initialize(_ctx.sceneContextView.Joystick);
            _currentState = GameState.WaitingToStart;
            _entitiesController = new EntitiesControllerPm(new EntitiesControllerPm.Ctx());
            AddDispose(_entitiesController);

            ShowStartScreen();
        }

        private void ShowStartScreen()
        {
            if (_currentState != GameState.WaitingToStart)
                return;

            _currentState = GameState.WaitingToStart;
            _inputManager?.SetJoystickOptions(AxisOptions.None);

            StartScreenPm.Ctx startScreenCtx = new StartScreenPm.Ctx
            {
                mainSceneContextView = _ctx.sceneContextView,
                startGameClicked = StartGame,
                cancellationToken = _ctx.cancellationToken
            };
            _startScreen = StartScreenPmFactory.CreateStartScreenPm(startScreenCtx);
            AddDispose(_startScreen);
        }

        private void StartGame()
        {
            if (_currentState != GameState.WaitingToStart)
                return;

            _inputManager.SetJoystickOptions(AxisOptions.Both);

            _currentState = GameState.Playing;

            // Dispose start screen
            _startScreen?.Dispose();
            _startScreen = null;

            // Initialize game components
            PlayerSpawnerPm.Ctx playerSpawnerCtx = new PlayerSpawnerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                entitiesController = _entitiesController,
                playerDead = ShowFinishScreen,
                cancellationToken = _ctx.cancellationToken
            };
            _playerSpawnerPm = new PlayerSpawnerPm(playerSpawnerCtx);
            AddDispose(_playerSpawnerPm);

            EnemyManagerPm.Ctx enemyManagerCtx = new EnemyManagerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                entitiesController = _entitiesController,
                cancellationToken = _ctx.cancellationToken
            };
            _enemyManager = new EnemyManagerPm(enemyManagerCtx);
            AddDispose(_enemyManager);

            MainScreenPm.Ctx mainScreenCtx = new MainScreenPm.Ctx
            {
                entitiesController = _entitiesController,
                mainSceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken
            };
            _mainScreen = MainScreenPmFactory.CreateMainScreenPm(mainScreenCtx);
            AddDispose(_mainScreen);
        }

        private void ShowFinishScreen()
        {
            if (_currentState != GameState.Playing)
                return;

            _currentState = GameState.Finished;

            _enemyManager?.Dispose();
            _mainScreen?.Dispose();
            _playerSpawnerPm?.Dispose();
            _entitiesController.Clear();
            _inputManager?.SetJoystickOptions(AxisOptions.None);

            FinishScreenPm.Ctx FinishScreenCtx = new FinishScreenPm.Ctx
            {
                entitiesController = _entitiesController,
                mainSceneContextView = _ctx.sceneContextView,
                restartGame = _ctx.restartGame,
                cancellationToken = _ctx.cancellationToken
            };
            AddDispose(FinishScreenPmFactory.CreateFinishScreenPm(FinishScreenCtx));
        }
    }
    internal enum GameState
    {
        WaitingToStart,
        Playing,
        Finished
    }
}
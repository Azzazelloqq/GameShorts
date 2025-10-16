using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using LightDI.Runtime;
using R3;
using UnityEngine;

namespace Lightseeker
{
    internal class LightseekerMainScenePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public LightseekerSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private LightseekerGameState _currentState;
        
        private LightseekerGameModel _gameModel;
        private LightseekerLevelPm _levelPm;
        private LightseekerPlayerPm _playerPm;
        private LightseekerMainUIPm _mainUIPm;
        private readonly IInputManager _inputManager;

        public LightseekerMainScenePm(Ctx ctx,
            [Inject] IInputManager inputManager)
        {
            _ctx = ctx;
            _inputManager = inputManager;
            _currentState = LightseekerGameState.WaitingToStart;
            
            if (_ctx.sceneContextView.Joystick == null)
            {
                Debug.LogError("EscapeFromDarkMainScenePm: Joystick is null in SceneContextView!");
            }
            else
            {
                _inputManager.Initialize(_ctx.sceneContextView.Joystick);
                Debug.Log("EscapeFromDarkMainScenePm: InputManager initialized with joystick");
            }
            
            StartGame();
        }

        private void StartGame()
        {
            if (_currentState != LightseekerGameState.WaitingToStart)
                return;

            _inputManager.SetJoystickOptions(AxisOptions.Both);
            _currentState = LightseekerGameState.Playing;

            // Сбрасываем статическое состояние игрока при полном перезапуске
            LightseekerPlayerPm.ResetStaticState();

            InitializeModel();
            InitializeUI();
            InitializeLevel();
            InitializePlayer();
        }

        private void InitializeModel()
        {
            _gameModel = new LightseekerGameModel();
        }

        private void InitializeUI()
        {
            var uiCtx = new LightseekerMainUIPm.Ctx
            {
                view = _ctx.sceneContextView.MainUIView,
                gameModel = _gameModel,
                cancellationToken = _ctx.cancellationToken
            };
            
            _mainUIPm = new LightseekerMainUIPm(uiCtx);
            AddDispose(_mainUIPm);
        }

        private void InitializeLevel()
        {
            var levelCtx = new LightseekerLevelPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                gameModel = _gameModel,
                onStarCollected = OnStarCollected,
                cancellationToken = _ctx.cancellationToken
            };
            
            _levelPm =  LightseekerLevelPmFactory.CreateLightseekerLevelPm(levelCtx);
            AddDispose(_levelPm);
        }

        private void InitializePlayer()
        {
            var playerCtx = new LightseekerPlayerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                isPaused = _ctx.isPaused
            };
            
            _playerPm = LightseekerPlayerPmFactory.CreateLightseekerPlayerPm(playerCtx);
            AddDispose(_playerPm);
        }

        private void OnStarCollected(int collectedStars)
        {
            // Здесь можно добавить дополнительную логику при сборе звезды
            // Например, звуковые эффекты или визуальные эффекты
        }
    }

    internal enum LightseekerGameState
    {
        WaitingToStart,
        Playing,
        Finished
    }
}

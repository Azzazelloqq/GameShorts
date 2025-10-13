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
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public LightseekerSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private LightseekerGameState _currentState;
        
        private LightseekerLevelPm _levelPm;
        private LightseekerCharacterController _characterController;
        private readonly IInputManager _inputManager;

        public LightseekerMainScenePm(Ctx ctx,
            [Inject] IInputManager inputManager)
        {
            _ctx = ctx;
            _inputManager = inputManager;
            
            if (_ctx.sceneContextView.Joystick == null)
            {
                Debug.LogError("EscapeFromDarkMainScenePm: Joystick is null in SceneContextView!");
            }
            else
            {
                _inputManager.Initialize(_ctx.sceneContextView.Joystick);
                Debug.Log("EscapeFromDarkMainScenePm: InputManager initialized with joystick");
            }
            
            _currentState = LightseekerGameState.WaitingToStart;
            
            StartGame();
        }

        private void StartGame()
        {
            if (_currentState != LightseekerGameState.WaitingToStart)
                return;

            _inputManager.SetJoystickOptions(AxisOptions.Both);
            _currentState = LightseekerGameState.Playing;

            InitializeLevel();
            InitializeCharacterController();
        }

        private void InitializeLevel()
        {
            var levelCtx = new LightseekerLevelPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken
            };
            
            _levelPm = new LightseekerLevelPm(levelCtx);
            AddDispose(_levelPm);
        }

        private void InitializeCharacterController()
        {
            var characterCtx = new LightseekerCharacterController.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                isPaused = _ctx.isPaused
            };
            
            _characterController = LightseekerCharacterControllerFactory.CreateLightseekerCharacterController(characterCtx);
            AddDispose(_characterController);
        }
    }

    internal enum LightseekerGameState
    {
        WaitingToStart,
        Playing,
        Finished
    }
}


using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using Code.Games._2048.Scripts.View;
using Code.Games._2048.Scripts.UI;
using Code.Games._2048.Scripts.Input;
using Code.Games._2048.Scripts.Gameplay;
using LightDI.Runtime;

namespace Code.Games._2048.Scripts.Logic
{
    internal class Game2048MainScenePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public Game2048SceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;
        private Game2048GameState _currentState;
        
        private Game2048StartScreenPm _startScreenPm;
        private Game2048InputPm _inputPm;
        private Game2048GameplayPm _gameplayPm;

        public Game2048MainScenePm(Ctx ctx)
        {
            _ctx = ctx;
            
            _currentState = Game2048GameState.WaitingToStart;
            
            StartGame();
        }

        private void ShowStartScreen()
        {
            if (_currentState != Game2048GameState.WaitingToStart)
                return;

            _currentState = Game2048GameState.WaitingToStart;
            Game2048StartScreenPm.Ctx startScreenCtx = new Game2048StartScreenPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                startGameClicked = StartGame,
                cancellationToken = _ctx.cancellationToken
            };
            _startScreenPm = new Game2048StartScreenPm(startScreenCtx);
            AddDispose(_startScreenPm);
        }

        private void StartGame()
        {
            if (_currentState != Game2048GameState.WaitingToStart)
                return;

            _currentState = Game2048GameState.Playing;

            // Dispose start screen
            _startScreenPm?.Dispose();
            _startScreenPm = null;

            // Initialize input system
            InitializeInputSystem();
            
            // Initialize gameplay
            InitializeGameplay();
        }

        private void InitializeInputSystem()
        {
            var inputCtx = new Game2048InputPm.Ctx
            {
                inputAreaView = _ctx.sceneContextView.InputAreaView,
                cancellationToken = _ctx.cancellationToken
            };
            
            _inputPm = new Game2048InputPm(inputCtx);
            AddDispose(_inputPm);
        }

        private void InitializeGameplay()
        {
            var gameplayCtx = new Game2048GameplayPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                inputPm = _inputPm,
                cubePrefab = _ctx.sceneContextView.CubePrefab,
                launchForce = _ctx.sceneContextView.LaunchForce,
                spawnDelay = _ctx.sceneContextView.SpawnDelay,
                cancellationToken = _ctx.cancellationToken
            };
            
            _gameplayPm = new Game2048GameplayPm(gameplayCtx);
            AddDispose(_gameplayPm);
        }

        private void ShowFinishScreen()
        {
            if (_currentState != Game2048GameState.Playing)
                return;

            _currentState = Game2048GameState.Finished;

            // TODO: Cleanup game components and show finish screen
        }
    }

    internal enum Game2048GameState
    {
        WaitingToStart,
        Playing,
        Finished
    }
}

using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using R3;

namespace Code.Games
{
    internal class Game2048MainScenePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public Game2048SceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private Game2048GameState _currentState;
        
        private Game2048StartScreenPm _startScreenPm;
        private Game2048InputPm _inputPm;
        private Game2048PausePm _pausePm;
        private Game2048FinishScreenPm _finishScreenPm;
        private Game2048GameplayPm _gameplayPm;

        public Game2048MainScenePm(Ctx ctx)
        {
            _ctx = ctx;
            
            _currentState = Game2048GameState.WaitingToStart;
            
            InitializeFinishScreen();
            StartGame();
        }
        
        private void InitializeFinishScreen()
        {
            var finishCtx = new Game2048FinishScreenPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                restartGame = _ctx.restartGame,
                cancellationToken = _ctx.cancellationToken
            };
            
            _finishScreenPm = new Game2048FinishScreenPm(finishCtx);
            AddDispose(_finishScreenPm);
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
            // Разрешаем запуск из WaitingToStart или Finished (для рестарта)
            if (_currentState != Game2048GameState.WaitingToStart && _currentState != Game2048GameState.Finished)
                return;

            _currentState = Game2048GameState.Playing;

            // Dispose start screen
            _startScreenPm?.Dispose();
            _startScreenPm = null;
            
            _ctx.sceneContextView.MainUIView.gameObject.SetActive(true);
            // Скрываем экран проигрыша (если был показан)
            _finishScreenPm?.HideFinishScreen();
            
            // Dispose старого геймплея если есть (для рестарта)
            if (_gameplayPm != null)
            {
                _gameplayPm.Dispose();
                _gameplayPm = null;
            }
            
            // Dispose старых систем для рестарта
            _inputPm?.Dispose();
            _pausePm?.Dispose();

            // Initialize input system first
            InitializeInputSystem();
            
            // Initialize pause system (needs input to be initialized)
            InitializePauseSystem();
            
            InitializeGameplay();
        }

        private void InitializePauseSystem()
        {
            var pauseCtx = new Game2048PausePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                isPaused = _ctx.isPaused
            };
            
            _pausePm = new Game2048PausePm(pauseCtx);
            AddDispose(_pausePm);
            
            AddDispose(_ctx.isPaused.Subscribe(isPaused =>
                {
                    // Блокируем/разблокируем инпут при паузе
                    _inputPm.SetInputEnabled(!isPaused);
                }));
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
                cancellationToken = _ctx.cancellationToken,
                isPaused = _ctx.isPaused,
                gameOver = ShowFinishScreen
            };
            
            _gameplayPm = Game2048GameplayPmFactory.CreateGame2048GameplayPm(gameplayCtx);
            AddDispose(_gameplayPm);
        }

        public void ShowFinishScreen()
        {
            if (_currentState != Game2048GameState.Playing)
                return;

            _currentState = Game2048GameState.Finished;
            _ctx.sceneContextView.MainUIView.gameObject.SetActive(false);

            // Останавливаем паузу (если была активна)
            if (_ctx.isPaused.Value)
            {
                _ctx.isPaused.Value = false;
            }
            
            // Останавливаем геймплей
            _gameplayPm?.Dispose();
            _gameplayPm = null;

            // Показываем экран проигрыша
            _finishScreenPm?.ShowFinishScreen();
        }
    }

    internal enum Game2048GameState
    {
        WaitingToStart,
        Playing,
        Finished
    }
}

using System;
using System.Threading;
using Disposable;
using R3;
using Cysharp.Threading.Tasks;

namespace Code.Core.ShortGamesCore.Game2
{
    public class BoxTowerGameScenePm : DisposableBase
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public BoxTowerSceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;
        private GameModel _gameModel;
        private TowerModel _towerModel;
        
        private BoxTowerTowerPm _towerPresenter;
        private BoxTowerUIPm _uiPresenter;
        private BoxTowerCameraPm _cameraPresenter;
        private BoxTowerInputPm _inputPresenter;
        private bool _initialized;
        private bool _subscriptionsSet;

        public BoxTowerGameScenePm(Ctx ctx)
        {
            _ctx = ctx;
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            EnsureModels();

            EnsureTowerPresenter();
            _towerPresenter?.Initialize();

            EnsureUIPresenter();
            _uiPresenter?.Initialize();

            EnsureCameraPresenter();
            _cameraPresenter?.Initialize();

            EnsureInputPresenter();
            _inputPresenter?.Initialize();

            SetupModelSubscriptions();
            _initialized = true;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            EnsureModels();

            // Spread across frames to keep preload smooth.
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            if (_initialized) return;

            EnsureTowerPresenter();
            if (_towerPresenter != null)
            {
                await _towerPresenter.InitializeAsync(cancellationToken);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            if (_initialized) return;

            EnsureUIPresenter();
            if (_uiPresenter != null)
            {
                await _uiPresenter.InitializeAsync(cancellationToken);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            if (_initialized) return;

            EnsureCameraPresenter();
            if (_cameraPresenter != null)
            {
                await _cameraPresenter.InitializeAsync(cancellationToken);
            }

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            if (_initialized) return;

            EnsureInputPresenter();
            if (_inputPresenter != null)
            {
                await _inputPresenter.InitializeAsync(cancellationToken);
            }

            SetupModelSubscriptions();
            _initialized = true;
        }

        public async UniTask PreloadAsync(CancellationToken cancellationToken = default)
        {
            // Prewarm pools for instant first spawn (Instantiate is the expensive part).
            if (_towerPresenter != null)
            {
                await _towerPresenter.PreloadAsync(cancellationToken);
            }
        }

        private void EnsureModels()
        {
            if (_gameModel == null)
            {
                _gameModel = new GameModel();
                _gameModel.Initialize();
            }

            if (_towerModel == null)
            {
                _towerModel = new TowerModel();
                _towerModel.Initialize();
            }
        }

        private void EnsureTowerPresenter()
        {
            if (_towerPresenter != null)
            {
                return;
            }

            BoxTowerTowerPm.Ctx towerCtx = new BoxTowerTowerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                towerModel = _towerModel,
                gameModel = _gameModel,
                cancellationToken = _ctx.cancellationToken
            };
            _towerPresenter =  BoxTowerTowerPmFactory.CreateBoxTowerTowerPm(towerCtx);
            AddDisposable(_towerPresenter);
        }

        private void EnsureUIPresenter()
        {
            if (_uiPresenter != null)
            {
                return;
            }

            BoxTowerUIPm.Ctx uiCtx = new BoxTowerUIPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                gameModel = _gameModel,
                restartGame = _ctx.restartGame,
                cancellationToken = _ctx.cancellationToken
            };
            _uiPresenter = new BoxTowerUIPm(uiCtx);
            AddDisposable(_uiPresenter);
        }

        private void EnsureCameraPresenter()
        {
            if (_cameraPresenter != null)
            {
                return;
            }

            BoxTowerCameraPm.Ctx cameraCtx = new BoxTowerCameraPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                towerModel = _towerModel,
                cancellationToken = _ctx.cancellationToken
            };
            _cameraPresenter =  BoxTowerCameraPmFactory.CreateBoxTowerCameraPm(cameraCtx);
            AddDisposable(_cameraPresenter);
        }

        private void EnsureInputPresenter()
        {
            if (_inputPresenter == null)
            {
                BoxTowerInputPm.Ctx inputCtx = new BoxTowerInputPm.Ctx
                {
                    sceneContextView = _ctx.sceneContextView,
                    gameModel = _gameModel,
                    cancellationToken = _ctx.cancellationToken
                };
                _inputPresenter = new BoxTowerInputPm(inputCtx);
                AddDisposable(_inputPresenter);
            }

            // Set tower presenter reference for input handling
            if (_towerPresenter != null)
            {
                _inputPresenter.SetTowerPresenter(_towerPresenter);
            }
        }

        private void SetupModelSubscriptions()
        {
            if (_subscriptionsSet)
            {
                return;
            }
            _subscriptionsSet = true;

            // Subscribe to game model events
            _gameModel.OnGameRestarted += OnGameRestarted;
            
            // Subscribe to tower model events  
            _towerModel.OnChunkCreated += OnChunkCreated;
        }

        private void OnGameRestarted()
        {
            _towerModel.Initialize();
        }

        private void OnChunkCreated(UnityEngine.Vector3 center, UnityEngine.Vector3 size)
        {
            // Handled by tower presenter
        }

        public void ResetForNewSession()
        {
            _towerPresenter?.ResetForNewSession();
            _gameModel?.RestartGame();
        }

        protected override void OnDispose()
        {
            // Unsubscribe from events
            if (_gameModel != null)
            {
                _gameModel.OnGameRestarted -= OnGameRestarted;
            }
            
            if (_towerModel != null)
            {
                _towerModel.OnChunkCreated -= OnChunkCreated;
            }
            
            base.OnDispose();
        }
    }
}

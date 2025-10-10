using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using R3;

namespace Code.Core.ShortGamesCore.Game2
{
    public class BoxTowerGameScenePm : BaseDisposable
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
        private IDisposable _uiPresenter;
        private IDisposable _cameraPresenter;
        private BoxTowerInputPm _inputPresenter;

        public BoxTowerGameScenePm(Ctx ctx)
        {
            _ctx = ctx;
            
            // Initialize models
            _gameModel = new GameModel();
            _towerModel = new TowerModel();
            
            // Initialize models
            _gameModel.Initialize();
            _towerModel.Initialize();
            
            // Create presenters
            CreateTowerPresenter();
            CreateUIPresenter();
            CreateCameraPresenter();
            CreateInputPresenter();
            
            // Setup model subscriptions
            SetupModelSubscriptions();
        }

        private void CreateTowerPresenter()
        {
            BoxTowerTowerPm.Ctx towerCtx = new BoxTowerTowerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                towerModel = _towerModel,
                gameModel = _gameModel,
                cancellationToken = _ctx.cancellationToken
            };
            _towerPresenter =  BoxTowerTowerPmFactory.CreateBoxTowerTowerPm(towerCtx);
            AddDispose(_towerPresenter);
        }

        private void CreateUIPresenter()
        {
            BoxTowerUIPm.Ctx uiCtx = new BoxTowerUIPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                gameModel = _gameModel,
                restartGame = _ctx.restartGame,
                cancellationToken = _ctx.cancellationToken
            };
            _uiPresenter = new BoxTowerUIPm(uiCtx);
            AddDispose(_uiPresenter);
        }

        private void CreateCameraPresenter()
        {
            BoxTowerCameraPm.Ctx cameraCtx = new BoxTowerCameraPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                towerModel = _towerModel,
                cancellationToken = _ctx.cancellationToken
            };
            _cameraPresenter =  BoxTowerCameraPmFactory.CreateBoxTowerCameraPm(cameraCtx);
            AddDispose(_cameraPresenter);
        }

        private void CreateInputPresenter()
        {
            BoxTowerInputPm.Ctx inputCtx = new BoxTowerInputPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                gameModel = _gameModel,
                cancellationToken = _ctx.cancellationToken
            };
            _inputPresenter = new BoxTowerInputPm(inputCtx);
            
            // Set tower presenter reference for input handling
            _inputPresenter.SetTowerPresenter(_towerPresenter);
            
            AddDispose(_inputPresenter);
        }

        private void SetupModelSubscriptions()
        {
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

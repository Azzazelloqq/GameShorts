using System;
using System.Threading;
using Disposable;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using Cysharp.Threading.Tasks;

namespace Code.Core.ShortGamesCore.Game2
{
    public class BoxTowerCorePm : DisposableBase
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public BoxTowerSceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;
        private BoxTowerGameScenePm _gameScene;
        private readonly IPoolManager _poolManager;
        private IDiContainer _diContainer;

        private bool _initialized;
        private bool _initializingAsync;
        private UniTask _initTask;

        public BoxTowerCorePm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
        }

        public void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            InitializeInternalSync();
            _initialized = true;
        }

        public UniTask EnsureInitializedAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return UniTask.CompletedTask;
            }

            if (_initializingAsync)
            {
                return _initTask;
            }

            _initializingAsync = true;
            _initTask = InitializeInternalAsync(cancellationToken).Preserve();
            return _initTask;
        }

        public async UniTask PreloadAsync(CancellationToken cancellationToken = default)
        {
            await EnsureInitializedAsync(cancellationToken);

            // Prewarm pools/presenters so first gameplay interactions don't hitch.
            // Spread work across frames to avoid spikes.
            if (_gameScene != null)
            {
                await _gameScene.PreloadAsync(cancellationToken);
            }
        }

        public void ResetForNewSession()
        {
            _gameScene?.ResetForNewSession();
        }

        private void InitializeInternalSync()
        {
            if (_diContainer == null)
            {
                _diContainer = DiContainerFactory.CreateContainer();
                AddDisposable(_diContainer);
            }

            if (_gameScene == null)
            {
                BoxTowerGameScenePm.Ctx gameSceneCtx = new BoxTowerGameScenePm.Ctx
                {
                    sceneContextView = _ctx.sceneContextView,
                    cancellationToken = _ctx.cancellationToken,
                    restartGame = _ctx.restartGame
                };

                _gameScene = new BoxTowerGameScenePm(gameSceneCtx);
                AddDisposable(_gameScene);
            }

            _gameScene.Initialize();
        }

        private async UniTask InitializeInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_diContainer == null)
                {
                    _diContainer = DiContainerFactory.CreateContainer();
                    AddDisposable(_diContainer);
                }

                // Spread init across frames so preload window stays smooth.
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                if (_initialized)
                {
                    return;
                }

                if (_gameScene == null)
                {
                    // Create game scene presenter (constructor: only DI & storing ctx)
                    BoxTowerGameScenePm.Ctx gameSceneCtx = new BoxTowerGameScenePm.Ctx
                    {
                        sceneContextView = _ctx.sceneContextView,
                        cancellationToken = _ctx.cancellationToken,
                        restartGame = _ctx.restartGame
                    };

                    _gameScene = new BoxTowerGameScenePm(gameSceneCtx);
                    AddDisposable(_gameScene);
                }

                await _gameScene.InitializeAsync(cancellationToken);
                _initialized = true;
            }
            finally
            {
                _initializingAsync = false;
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            // After all pooled objects were returned by disposables, clear pools.
            _poolManager?.Clear();
        }
    }
}

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Code.Core.Tools.Pool;
using Disposable;
using LightDI.Runtime;
using R3;
using UnityEngine;

namespace Code.Games
{
    internal class Game2048CorePm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public Game2048SceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private IDisposable _scene;
        private readonly IDiContainer _diContainer;
        private readonly IPoolManager _poolManager;
        private bool _prewarmed;

        public Game2048CorePm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _diContainer = DiContainerFactory.CreateLocalContainer();
            
            AddDisposable(_diContainer);
            
            Game2048ScenePm.Ctx sceneCtx = new Game2048ScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame,
                isPaused = _ctx.isPaused
            };
            _scene = new Game2048ScenePm(sceneCtx);
            AddDisposable(_scene);
        }

        public UniTask PreloadAsync(CancellationToken cancellationToken = default)
        {
            if (_prewarmed)
            {
                return UniTask.CompletedTask;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var sceneContextView = _ctx.sceneContextView;
            var cubePrefab = sceneContextView != null ? sceneContextView.CubePrefab : null;
            if (cubePrefab == null || _poolManager == null)
            {
                return UniTask.CompletedTask;
            }

            var spawnPoint = sceneContextView.GameSpawnPoint != null
                ? sceneContextView.GameSpawnPoint.position
                : Vector3.zero;

            // Warm up at least one cube instance so StartGame doesn't hitch on first spawn.
            const int warmCount = 1;
            for (int i = 0; i < warmCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cubeObject = _poolManager.Get(cubePrefab, spawnPoint);
                if (cubeObject == null)
                {
                    continue;
                }

                var cubeView = cubeObject.GetComponent<Game2048CubeView>();
                if (cubeView != null)
                {
                    cubeView.SetCtx(new Game2048CubeView.Ctx
                    {
                        cubeId = Guid.NewGuid(),
                        number = 2,
                        onCollisionWithCube = null
                    });
                    cubeView.ResetVelocity();
                }

                _poolManager.Return(cubePrefab, cubeObject);
            }

            _prewarmed = true;
            return UniTask.CompletedTask;
        }

        protected override void OnDispose()
        {
            // Сначала вызываем base.OnDispose() - это disposal всех AddDispose элементов
            // включая _scene и _diContainer, что приведет к возврату всех кубов в пул
            base.OnDispose();
            
            // ПОСЛЕ того как все кубы вернулись в пул, очищаем его
            _poolManager?.Clear();
        }
    }
}

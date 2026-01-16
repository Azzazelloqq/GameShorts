using System;
using System.Threading;
using Disposable;
using Code.Core.Tools.Pool;
using Cysharp.Threading.Tasks;
using GameShorts.CubeRunner.View;
using R3;

namespace GameShorts.CubeRunner.Core
{
    internal class CubeRunnerCorePm : DisposableBase
    {
        internal struct Ctx
        {
            public CubeRunnerSceneContextView sceneContextView;
            public CancellationToken cancellationToken;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly CubeRunnerScenePm _scene;

        public CubeRunnerCorePm(Ctx ctx)
        {
            CubeRunnerScenePm.Ctx sceneCtx = new CubeRunnerScenePm.Ctx
            {
                sceneContextView = ctx.sceneContextView,
                cancellationToken = ctx.cancellationToken,
                restartGame = ctx.restartGame,
                isPaused = ctx.isPaused
            };

            _scene = new CubeRunnerScenePm(sceneCtx);
            AddDisposable(_scene);
        }

        public async UniTask PreloadAsync(CancellationToken cancellationToken = default)
        {
            if (_scene != null)
            {
                await _scene.PreloadAsync(cancellationToken);
            }
        }
    }
}


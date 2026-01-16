using System;
using System.Threading;
using Disposable;
using Cysharp.Threading.Tasks;
using GameShorts.CubeRunner.Logic;
using GameShorts.CubeRunner.View;
using R3;

namespace GameShorts.CubeRunner.Core
{
    internal class CubeRunnerScenePm : DisposableBase
    {
        internal struct Ctx
        {
            public CubeRunnerSceneContextView sceneContextView;
            public CancellationToken cancellationToken;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly CubeRunnerMainScenePm _mainScenePm;

        public CubeRunnerScenePm(Ctx ctx)
        {
            CubeRunnerMainScenePm.Ctx mainSceneCtx = new CubeRunnerMainScenePm.Ctx
            {
                sceneContextView = ctx.sceneContextView,
                cancellationToken = ctx.cancellationToken,
                restartGame = ctx.restartGame,
                isPaused = ctx.isPaused
            };

            _mainScenePm = new CubeRunnerMainScenePm(mainSceneCtx);
            AddDisposable(_mainScenePm);
        }

        public async UniTask PreloadAsync(CancellationToken cancellationToken = default)
        {
            if (_mainScenePm != null)
            {
                await _mainScenePm.PreloadAsync(cancellationToken);
            }
        }
    }
}


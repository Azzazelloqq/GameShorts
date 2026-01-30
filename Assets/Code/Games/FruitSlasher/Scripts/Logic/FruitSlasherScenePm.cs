using System;
using System.Threading;
using Code.Games.FruitSlasher.Scripts.Input;
using Code.Games.FruitSlasher.Scripts.View;
using Disposable;
using R3;

namespace Code.Games.FruitSlasher.Scripts.Logic
{
    internal class FruitSlasherScenePm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public FruitSlasherSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private readonly FruitManagerPm _manager;
        private readonly InputAreaPm _inputArea;
        private readonly BladePm _blade;

        public FruitSlasherScenePm(Ctx ctx)
        {
            _ctx = ctx;
            
            _blade =  BladePmFactory.CreateBladePm(new BladePm.Ctx()
            {
                sceneContextView = _ctx.sceneContextView,
                isPaused = _ctx.isPaused,
                restartGame = _ctx.restartGame
                
            });
            AddDisposable(_blade);
            
            _manager = FruitManagerPmFactory.CreateFruitManagerPm(new FruitManagerPm.Ctx()
            {
                cancellationToken = _ctx.cancellationToken,
                sceneContextView = _ctx.sceneContextView,
                restartGame = _ctx.restartGame,
                isPaused = _ctx.isPaused,
                blade = _blade,
            });
            AddDisposable(_manager);

            _manager.Start();
        }
        
    }
}
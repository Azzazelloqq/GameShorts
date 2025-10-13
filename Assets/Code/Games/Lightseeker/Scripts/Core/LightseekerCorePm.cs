using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using LightDI.Runtime;
using R3;

namespace Lightseeker
{
    internal class LightseekerCorePm : BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public LightseekerSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private IDisposable _mainScene;
        private readonly IDiContainer _diContainer;
        private readonly InputManager _inputManager;

        public LightseekerCorePm(Ctx ctx)
        {
            _ctx = ctx;
            
            _diContainer = DiContainerFactory.CreateContainer();
            AddDispose(_diContainer);
            _inputManager = new InputManager();
            _diContainer.RegisterAsSingleton<IInputManager>(_inputManager);
            _inputManager?.SetJoystickOptions(AxisOptions.None);
            LightseekerMainScenePm.Ctx mainSceneCtx = new LightseekerMainScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame,
                isPaused = _ctx.isPaused
            };
            _mainScene = LightseekerMainScenePmFactory.CreateLightseekerMainScenePm(mainSceneCtx);
            AddDispose(_mainScene);
        }
        
        protected override void OnDispose()
        {
            _inputManager?.SetJoystickOptions(AxisOptions.None);
            base.OnDispose();
        }
    }
}


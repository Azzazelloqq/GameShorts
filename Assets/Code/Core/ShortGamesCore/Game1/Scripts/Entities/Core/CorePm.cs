using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using LightDI.Runtime;
using R3;

namespace Code.Core.ShortGamesCore.Game1.Scripts.Core
{
    public class CorePm: BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public MainSceneContextView sceneContextView;
        }

        private readonly Ctx _ctx;
        private IDisposable _scene;
        private readonly IDiContainer _diContainer;
        private readonly InputManager.InputManager _inputManager;

        public CorePm(Ctx ctx)
        {
            _ctx = ctx;
            
            _diContainer = DiContainerFactory.CreateContainer();
            AddDispose(_diContainer);
            _inputManager = new InputManager.InputManager();
            _diContainer.RegisterAsSingleton<IInputManager>(_inputManager);
            ScenePm.Ctx sceneCtx = new ScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken
            };
            _scene = new ScenePm(sceneCtx);
        }

        protected override void OnDispose()
        {
            _scene?.Dispose();
            base.OnDispose();
        }
    }
}
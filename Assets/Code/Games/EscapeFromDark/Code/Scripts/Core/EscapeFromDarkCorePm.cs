using System;
using System.Threading;
using Disposable;
using Code.Core.InputManager;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using R3;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Core
{
    internal class EscapeFromDarkCorePm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public EscapeFromDarkSceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;
        private IDisposable _scene;
        private readonly IDiContainer _diContainer;
        private readonly InputManager.InputManager _inputManager;

        public EscapeFromDarkCorePm(Ctx ctx)
        {
            _ctx = ctx;
            _diContainer = DiContainerFactory.CreateContainer();
            AddDisposable(_diContainer);
            _inputManager = new InputManager.InputManager();
            _diContainer.RegisterAsSingleton<IInputManager>(_inputManager);
            _inputManager?.SetJoystickOptions(AxisOptions.None);
            
            EscapeFromDarkScenePm.Ctx sceneCtx = new EscapeFromDarkScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame
            };
            _scene = new EscapeFromDarkScenePm(sceneCtx);
            AddDisposable(_scene);
        }

        protected override void OnDispose()
        {
            _inputManager?.SetJoystickOptions(AxisOptions.None);
            base.OnDispose();
        }
    }
}

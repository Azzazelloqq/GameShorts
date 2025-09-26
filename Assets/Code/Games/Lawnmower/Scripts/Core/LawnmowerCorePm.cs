using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.View;
using Code.Core.Tools.Pool;
using LightDI;
using LightDI.Runtime;
using TickHandler;
using TickHandler.UnityTickHandler;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Core
{
    internal class LawnmowerCorePm: BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public LawnmowerSceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;
        private IDisposable _scene;
        private readonly IDiContainer _diContainer;
        private readonly IInputManager _inputManager;
        private readonly IPoolManager _poolManager;

        public LawnmowerCorePm(Ctx ctx)
        {
            _ctx = ctx;
            _diContainer = DiContainerFactory.CreateContainer();
            AddDispose(_diContainer);
            _inputManager = new InputManager.InputManager();
            _diContainer.RegisterAsSingleton<IInputManager>(_inputManager);
            _inputManager?.SetJoystickOptions(AxisOptions.None);
            
            LawnmowerScenePm.Ctx sceneCtx = new LawnmowerScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame
            };
            _scene = new LawnmowerScenePm(sceneCtx);
        }

        protected override void OnDispose()
        {
            _inputManager?.SetJoystickOptions(AxisOptions.None);
            _scene?.Dispose();
            _poolManager?.Clear();
            base.OnDispose();
        }
    }
}

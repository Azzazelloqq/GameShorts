using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using R3;
using TickHandler;
using TickHandler.UnityTickHandler;
using UnityEngine;

namespace Lightseeker
{
    internal class LightseekerCorePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public LightseekerSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private IDisposable _mainScene;
        private readonly IDiContainer _diContainer;
        private readonly IInputManager _inputManager;
        private readonly IPoolManager _poolManager;
        private readonly ITickHandler _tickHandler;

        public LightseekerCorePm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _diContainer = DiContainerFactory.CreateContainer();
            AddDispose(_diContainer);
            
            // Создаем и регистрируем InputManager
            _inputManager = new InputManager();
            _diContainer.RegisterAsSingleton<IInputManager>(_inputManager);
            // Джойстик будет инициализирован и настроен в LightseekerMainScenePm
            
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
            // Сначала очищаем основную сцену (чтобы отписаться от событий InputManager)
            _mainScene?.Dispose();
            // Затем отключаем джойстик
            _inputManager?.SetJoystickOptions(AxisOptions.None);
            base.OnDispose();
            _poolManager?.Clear();
        }
    }
}

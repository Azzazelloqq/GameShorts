using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using LightDI.Runtime;
using Logic.Entities;
using Logic.Entities.Core;
using TickHandler;
using UnityEngine;

namespace Logic.Scene
{
    internal class BorderControllerPm : BaseDisposable
    {
        internal struct Ctx
        {
            public SceneContextView sceneContextView;
            public BaseModel model;
            public IEntitiesController entitiesController;
        }

        private readonly Ctx _ctx;
        private Camera _camera;
        private Rect _srceenRect;
        private readonly ITickHandler _tickHandler;

        public BorderControllerPm(Ctx ctx, 
            [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            _camera = _ctx.sceneContextView.Camera;
            _tickHandler.PhysicUpdate += (CheckScreenPos);
        }

        protected override void OnDispose()
        {
            base.OnDispose(); 
            _tickHandler.PhysicUpdate -= (CheckScreenPos);
        }
        
        private void CheckScreenPos(float deltaTime)
        {
            var playerPos = _ctx.model.Position.Value;
            Vector3 viewPosition = _camera.WorldToViewportPoint(playerPos);

            if ((viewPosition.x is < -0.5f or > 1.5f) || (viewPosition.y is < -0.5f or > 1.5f))
                _ctx.entitiesController.TryDestroyEntity(_ctx.model.Id);
        }
        
    }
}
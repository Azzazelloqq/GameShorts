using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Logic.Entities;
using Logic.Entities.Core;
using UnityEngine;

namespace Logic.Scene
{
    public class BorderControllerPm : BaseDisposable
    {
        public struct Ctx
        {
            public SceneContextView sceneContextView;
            public BaseModel model;
            public IEntitiesController entitiesController;
        }

        private readonly Ctx _ctx;
        private Camera _camera;
        private Rect _srceenRect;

        public BorderControllerPm(Ctx ctx)
        {
            _ctx = ctx;
            _camera = _ctx.sceneContextView.Camera;
            _ctx.sceneContextView.OnFixedUpdated += CheckScreenPos;
        }

        protected override void OnDispose()
        {
            base.OnDispose(); 
            _ctx.sceneContextView.OnFixedUpdated -= CheckScreenPos;
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
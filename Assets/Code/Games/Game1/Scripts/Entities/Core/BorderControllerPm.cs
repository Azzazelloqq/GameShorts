using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Disposable;
using LightDI.Runtime;
using SceneContext;
using TickHandler;
using UnityEngine;

namespace Logic.Scene
{
internal class BorderControllerPm : DisposableBase
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

	public BorderControllerPm(
		Ctx ctx,
		[Inject] ITickHandler tickHandler)
	{
		_ctx = ctx;
		_tickHandler = tickHandler;
		_camera = _ctx.sceneContextView.Camera;
		_tickHandler.PhysicUpdate += CheckScreenPos;
	}

	protected override void OnDispose()
	{
		_tickHandler.PhysicUpdate -= CheckScreenPos;
		base.OnDispose();
	}

	private void CheckScreenPos(float deltaTime)
	{
		// Проверяем, что камера еще существует
		if (_camera == null || !_camera)
		{
			return;
		}

		var playerPos = _ctx.model.Position.Value;
		var viewPosition = _camera.WorldToViewportPoint(playerPos);

		if (viewPosition.x is < -0.5f or > 1.5f || viewPosition.y is < -0.5f or > 1.5f)
		{
			// Проверяем, что EntitiesController еще существует
			if (_ctx.entitiesController != null)
			{
				_ctx.entitiesController.TryDestroyEntity(_ctx.model.Id);
			}
		}
	}
}
}
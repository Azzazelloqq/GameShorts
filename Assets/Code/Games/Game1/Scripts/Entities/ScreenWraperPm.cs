
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Asteroids.Code.Games.Game1.Scripts.View;
using Disposable;
using LightDI.Runtime;
using TickHandler;
using UnityEngine;

namespace Logic.Entities
{
	internal class ScreenWraperPm : DisposableBase
	{
		internal struct Ctx
		{
			public MainSceneContextView sceneContextView;
			public PlayerModel playerModel;
		}

		private readonly Ctx _ctx;
		private Camera _camera;
		private readonly ITickHandler _tickHandler;

		public ScreenWraperPm(Ctx ctx, 
			[Inject] ITickHandler tickHandler)
		{
			_ctx = ctx;
			_tickHandler = tickHandler;
			_camera = _ctx.sceneContextView.Camera;
			_tickHandler.PhysicUpdate += (CheckScreenPos);
		}

		protected override void OnDispose()
		{
			_tickHandler.PhysicUpdate -= (CheckScreenPos);
			base.OnDispose(); 
		}

		private void CheckScreenPos(float deltaTime)
		{
			// Проверяем, что камера еще существует
			if (_camera == null || !_camera)
			{
				return;
			}
			
			var playerPos = _ctx.playerModel.Position.Value;
			Vector3 viewPosition = _camera.WorldToViewportPoint(playerPos);

			// Телепортация по X оси
			if (viewPosition.x < 0)
			{
				// Игрок вышел за левый край - телепортируем на правый край
				if (_camera != null && _camera)
				{
					Vector3 rightEdge = _camera.ViewportToWorldPoint(new Vector3(1, viewPosition.y, viewPosition.z));
					playerPos.x = rightEdge.x;
				}
			}
			else if (viewPosition.x > 1)
			{
				// Игрок вышел за правый край - телепортируем на левый край
				if (_camera != null && _camera)
				{
					Vector3 leftEdge = _camera.ViewportToWorldPoint(new Vector3(0, viewPosition.y, viewPosition.z));
					playerPos.x = leftEdge.x;
				}
			}

			// Телепортация по Y оси
			if (viewPosition.y < 0)
			{
				// Игрок вышел за нижний край - телепортируем на верхний край
				if (_camera != null && _camera)
				{
					Vector3 topEdge = _camera.ViewportToWorldPoint(new Vector3(viewPosition.x, 1, viewPosition.z));
					playerPos.y = topEdge.y;
				}
			}
			else if (viewPosition.y > 1)
			{
				// Игрок вышел за верхний край - телепортируем на нижний край
				if (_camera != null && _camera)
				{
					Vector3 bottomEdge = _camera.ViewportToWorldPoint(new Vector3(viewPosition.x, 0, viewPosition.z));
					playerPos.y = bottomEdge.y;
				}
			}

			_ctx.playerModel.Position.Value = playerPos;
		}
		
	}
}
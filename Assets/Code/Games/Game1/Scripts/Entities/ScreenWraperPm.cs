
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using UnityEngine;

namespace Logic.Entities
{
	internal class ScreenWraperPm : BaseDisposable
	{
		public struct Ctx
		{
			public MainSceneContextView sceneContextView;
			public PlayerModel playerModel;
		}

		private readonly Ctx _ctx;
		private Camera _camera;

		public ScreenWraperPm(Ctx ctx)
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
			var playerPos = _ctx.playerModel.Position.Value;
			Vector3 viewPosition = _camera.WorldToViewportPoint(playerPos);

			// Телепортация по X оси
			if (viewPosition.x < 0)
			{
				// Игрок вышел за левый край - телепортируем на правый край
				Vector3 rightEdge = _camera.ViewportToWorldPoint(new Vector3(1, viewPosition.y, viewPosition.z));
				playerPos.x = rightEdge.x;
			}
			else if (viewPosition.x > 1)
			{
				// Игрок вышел за правый край - телепортируем на левый край
				Vector3 leftEdge = _camera.ViewportToWorldPoint(new Vector3(0, viewPosition.y, viewPosition.z));
				playerPos.x = leftEdge.x;
			}

			// Телепортация по Y оси
			if (viewPosition.y < 0)
			{
				// Игрок вышел за нижний край - телепортируем на верхний край
				Vector3 topEdge = _camera.ViewportToWorldPoint(new Vector3(viewPosition.x, 1, viewPosition.z));
				playerPos.y = topEdge.y;
			}
			else if (viewPosition.y > 1)
			{
				// Игрок вышел за верхний край - телепортируем на нижний край
				Vector3 bottomEdge = _camera.ViewportToWorldPoint(new Vector3(viewPosition.x, 0, viewPosition.z));
				playerPos.y = bottomEdge.y;
			}

			_ctx.playerModel.Position.Value = playerPos;
		}
		
	}
}
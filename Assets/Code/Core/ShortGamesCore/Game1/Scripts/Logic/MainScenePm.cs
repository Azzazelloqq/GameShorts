using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Logic.Enemy;
using Logic.Player;
using Logic.UI;

namespace Code.Core.ShortGamesCore.Game1.Scripts.Logic
{
	internal class MainScenePm : BaseDisposable
	{
		public struct Ctx
		{
			public CancellationToken cancellationToken;
			public MainSceneContextView sceneContextView;
			public Action restartGame;
		}

		private readonly Ctx _ctx;
		private EntitiesControllerPm _entitiesController;
		private IDisposable _enemyManager;
		private IDisposable _mainScreen;

		public MainScenePm(Ctx ctx)
		{
			_ctx = ctx;
			_entitiesController = new EntitiesControllerPm(new EntitiesControllerPm.Ctx());
			AddDispose(new EntitiesControllerPm(new EntitiesControllerPm.Ctx()));
			
			PlayerSpawnerPm.Ctx playerSpawnerCtx = new PlayerSpawnerPm.Ctx
			{
					sceneContextView = _ctx.sceneContextView,
					entitiesController = _entitiesController,
					playerDead = ShowFinishScreen,
					cancellationToken = _ctx.cancellationToken
			};
			AddDispose( PlayerSpawnerPmFactory.CreatePlayerSpawnerPm(playerSpawnerCtx));

			EnemyManagerPm.Ctx enemyManagerCtx = new EnemyManagerPm.Ctx
			{
				sceneContextView = _ctx.sceneContextView,
				entitiesController = _entitiesController,
				cancellationToken = _ctx.cancellationToken
			};
			_enemyManager = new EnemyManagerPm(enemyManagerCtx);
			AddDispose(_enemyManager);

			MainScreenPm.Ctx mainScreenCtx = new MainScreenPm.Ctx
			{
				entitiesController = _entitiesController,
				mainSceneContextView = _ctx.sceneContextView,
				cancellationToken = _ctx.cancellationToken
			};
			_mainScreen =  MainScreenPmFactory.CreateMainScreenPm(mainScreenCtx);
			AddDispose(_mainScreen);
		}

		private void ShowFinishScreen()
		{
			_enemyManager?.Dispose();
			_mainScreen?.Dispose();
			FinishScreenPm.Ctx FinishScreenCtx = new FinishScreenPm.Ctx
			{
				entitiesController = _entitiesController,
				mainSceneContextView = _ctx.sceneContextView,
				restartGame = _ctx.restartGame,
				cancellationToken = _ctx.cancellationToken
			};
			AddDispose(FinishScreenPmFactory.CreateFinishScreenPm(FinishScreenCtx));
			_entitiesController?.Dispose();
		}
	}
}
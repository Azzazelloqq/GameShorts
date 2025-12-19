using System;
using System.Threading;
using Asteroids.Code.Games.Game1.Scripts.View;
using Code.Core.ShortGamesCore.Game1.Scripts.Logic;
using Disposable;

namespace Code.Core.ShortGamesCore.Game1.Scripts.Core
{
internal class ScenePm : DisposableBase
{
	internal struct Ctx
	{
		public CancellationToken cancellationToken;
		public MainSceneContextView sceneContextView;
		public Action restartGame;
	}

	private readonly Ctx _ctx;

	public ScenePm(Ctx ctx)
	{
		_ctx = ctx;
		var mainSceneCtx = new MainScenePm.Ctx
		{
			sceneContextView = _ctx.sceneContextView,
			cancellationToken = _ctx.cancellationToken,
			restartGame = _ctx.restartGame
		};
		var mainScenePm = MainScenePmFactory.CreateMainScenePm(mainSceneCtx);
		AddDisposable(mainScenePm);
	}
}
}
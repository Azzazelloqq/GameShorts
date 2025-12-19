using System.Threading;
using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Asteroids.Code.Games.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using Code.Generated.Addressables;
using Disposable;
using LightDI.Runtime;
using Logic.Entities;
using Logic.Entities.Core;
using Logic.Scene;
using ResourceLoader;
using TickHandler;
using UnityEngine;

namespace Logic.Enemy.UFO
{
internal class UFOPm : DisposableBase
{
	internal struct Ctx
	{
		public CancellationToken cancellationToken;
		public MainSceneContextView sceneContextView;
		public UFOModel ufoModel;
		public PlayerModel playerModel;
		public IEntitiesController entitiesController;
	}

	private readonly Ctx _ctx;
	private GameObject _pref;
	private UFOView _view;
	private readonly IPoolManager _poolManager;
	private readonly IResourceLoader _resourceLoader;
	private readonly ITickHandler _tickHandler;

	public UFOPm(
		Ctx ctx,
		[Inject] IPoolManager poolManager,
		[Inject] IResourceLoader resourceLoader,
		[Inject] ITickHandler tickHandler)
	{
		_ctx = ctx;
		_poolManager = poolManager;
		_resourceLoader = resourceLoader;
		_tickHandler = tickHandler;
		var baseCtx = new EntityMoverPm.Ctx
		{
			model = _ctx.ufoModel,
			useAcceleration = true
		};

		var UFOMoverCtx = new UFOMoverPm.UFOMoverCtx
		{
			playerModel = _ctx.playerModel
		};
		AddDisposable(UFOMoverPmFactory.CreateUFOMoverPm(UFOMoverCtx, baseCtx));

		var borderCtx = new BorderControllerPm.Ctx
		{
			sceneContextView = _ctx.sceneContextView,
			model = _ctx.ufoModel,
			entitiesController = _ctx.entitiesController
		};
		AddDisposable(BorderControllerPmFactory.CreateBorderControllerPm(borderCtx));

		_resourceLoader.LoadResource<GameObject>(ResourceIdsContainer.GameAsteroids.UFO, pref =>
		{
			_pref = pref;
			var spawnPlayer = _poolManager.Get(pref, _ctx.ufoModel.Position.Value);
			_view = spawnPlayer.GetComponent<UFOView>();
			_view.SetCtx(new BaseView.Ctx
			{
				model = _ctx.ufoModel
			});
			_tickHandler.FrameUpdate += UpdateView;
		}, _ctx.cancellationToken);
	}

	protected override void OnDispose()
	{
		_tickHandler.FrameUpdate -= UpdateView;
		_poolManager.Return(_pref, _view.gameObject);
		base.OnDispose();
	}

	private void UpdateView(float deltaTime)
	{
		_view.transform.position = _ctx.ufoModel.Position.Value;
		_view.transform.rotation = Quaternion.Euler(0, 0, _ctx.ufoModel.CurrentAngle.Value);
	}
}
}
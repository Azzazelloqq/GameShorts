using System;
using System.Threading;
using System.Threading.Tasks;
using Asteroids.Code.Games.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using Code.Generated.Addressables;
using Disposable;
using InGameLogger;
using LightDI.Runtime;
using ResourceLoader;
using TickHandler;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Logic.UI
{
internal class StartScreenPm : DisposableBase
{
	internal struct Ctx
	{
		public CancellationToken cancellationToken;
		public Action startGameClicked;
		public MainSceneContextView mainSceneContextView;
	}

	private readonly Ctx _ctx;
	private StartScreenView _view;
	private readonly IResourceLoader _resourceLoader;
	private readonly IPoolManager _poolManager;
	private readonly IInGameLogger _logger;
	private readonly ITickHandler _tickHandler;

	public StartScreenPm(
		Ctx ctx,
		[Inject] IPoolManager poolManager,
		[Inject] IInGameLogger logger,
		[Inject] IResourceLoader resourceLoader,
		[Inject] ITickHandler tickHandler)
	{
		_ctx = ctx;
		_poolManager = poolManager;
		_resourceLoader = resourceLoader;
		_logger = logger;
		_tickHandler = tickHandler;
		_ = Load();
	}

	private async Task Load()
	{
		try
		{
			await LoadBaseUI();
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to load StartScreen: {ex.Message}");
			throw;
		}
	}

	protected override void OnDispose()
	{
		if (_view != null)
		{
			GameObject.Destroy(_view.gameObject);
		}
	}

	private async Task LoadBaseUI()
	{
		var prefab =
			await _resourceLoader.LoadResourceAsync<GameObject>(ResourceIdsContainer.GameAsteroids.StartScreen,
				_ctx.cancellationToken);
		var objView = Object.Instantiate(prefab, _ctx.mainSceneContextView.UiParent, false);
		_view = objView.GetComponent<StartScreenView>();

		_view.SetCtx(new StartScreenView.Ctx
		{
			startGameClicked = _ctx.startGameClicked
		});

		// Set initial text
		_view.TitleLabel.text = "ASTEROIDS";
		_view.InstructionLabel.text = "TAP TO START";
	}
}
}
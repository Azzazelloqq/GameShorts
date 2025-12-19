using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Asteroids.Code.Games.Game1.Scripts.View;
using Disposable;
using Code.Core.Tools.Pool;
using Code.Generated.Addressables;
using InGameLogger;
using LightDI.Runtime;
using R3;
using ResourceLoader;
using TickHandler;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Logic.UI
{
internal class MainScreenPm : DisposableBase
{
	internal struct Ctx
	{
		public CancellationToken cancellationToken;
		public IEntitiesController entitiesController;
		public MainSceneContextView mainSceneContextView;
	}

	private readonly Ctx _ctx;
	private MainScreenView _view;
	private List<LaserChargeUiView> _battaries;
	private readonly PlayerModel _playerModel;
	private readonly IPoolManager _poolManager;
	private readonly IResourceLoader _resourceLoader;
	private readonly IInGameLogger _logger;
	private readonly ITickHandler _tickHandler;

	public MainScreenPm(
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
		_playerModel = _ctx.entitiesController.GetPlayerModel();

		_ = Load();
	}

	protected override void OnDispose()
	{
		_tickHandler.FrameUpdate -= OnUpdated;
		Object.Destroy(_view);
		foreach (var laserChargeUiView in _battaries)
		{
			Object.Destroy(laserChargeUiView.gameObject);
		}

		_battaries.Clear();

		base.OnDispose();
	}

	private async Task Load()
	{
		try
		{
			await LoadBaseUI();
			await LoadLasers();
			InitSubscribes();
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to load MainScreen: {ex.Message}");
			throw;
		}
	}

	private async Task LoadBaseUI()
	{
		var prefab =
			await _resourceLoader.LoadResourceAsync<GameObject>(ResourceIdsContainer.GameAsteroids.MainScreen,
				_ctx.cancellationToken);
		var objView = Object.Instantiate(prefab, _ctx.mainSceneContextView.UiParent, false);
		_view = objView.GetComponent<MainScreenView>();
	}

	private async Task LoadLasers()
	{
		var prefab =
			await _resourceLoader.LoadResourceAsync<GameObject>(ResourceIdsContainer.GameAsteroids.LaserChargeUI,
				_ctx.cancellationToken);

		_battaries = new List<LaserChargeUiView>(_ctx.mainSceneContextView.laserSettings.CountLaserShots);
		for (var i = 0; i < _ctx.mainSceneContextView.laserSettings.CountLaserShots; i++)
		{
			var objView = Object.Instantiate(prefab, _view.LaserChargeHolder, false);
			var view = objView.GetComponent<LaserChargeUiView>();
			_battaries.Add(view);
		}
	}

	private void InitSubscribes()
	{
		_tickHandler.FrameUpdate += OnUpdated;
		AddDisposable(_playerModel.Position.Subscribe(UpdatePos));
		AddDisposable(_playerModel.Score.Subscribe(ScoreOnChanged));
		AddDisposable(_playerModel.CurrentSpeed.Subscribe(UpdateCurSpeed));
		AddDisposable(_playerModel.CurrentAngle.Subscribe(UpdateCurAngle));
	}

	private void ScoreOnChanged(int score)
	{
		if (IsDisposed || _view == null || _view.Score == null)
		{
			return;
		}

		_view.Score.text = $"Score: {score}";
	}

	private void OnUpdated(float deltaTime)
	{
		// Check if this instance is disposed or objects are null
		if (IsDisposed || _playerModel == null || _battaries == null || _playerModel.Charges == null)
		{
			return;
		}

		if (_playerModel.Charges.Count == 0)
		{
			return;
		}

		for (var i = 0; i < _battaries.Count && i < _playerModel.Charges.Count; i++)
		{
			// Additional null checks for safety
			if (_battaries[i] == null || _playerModel.Charges[i] == null || _playerModel.Charges[i].Charge == null)
			{
				continue;
			}

			var valueCharge = _playerModel.Charges[i].Charge.Value;
			_battaries[i].Slider.value = valueCharge;
			_battaries[i].FillImage.color = valueCharge < 1f ? Color.yellow : Color.green;
		}
	}

	private void UpdateCurAngle(float angle)
	{
		if (IsDisposed || _view == null || _view.Angle == null)
		{
			return;
		}

		_view.Angle.text = $"Angle: {Mathf.Abs(Mathf.Floor(angle))}";
	}

	private void UpdateCurSpeed(float speed)
	{
		if (IsDisposed || _view == null || _view.Speed == null)
		{
			return;
		}

		_view.Speed.text = $"Speed: {Mathf.Floor(speed)}";
	}

	private void UpdatePos(Vector2 position)
	{
		if (IsDisposed || _view == null || _view.PosX == null || _view.PosY == null)
		{
			return;
		}

		_view.PosX.text = $"X: {position.x:N0}";
		_view.PosY.text = $"X: {position.y:N0}";
	}
}
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game2
{
public class BoxTower : BaseMonoBehaviour, IShortGame2D
{
	[SerializeField]
	private Camera _camera;

	[SerializeField]
	private BoxTowerSceneContextView _sceneContext;

	public bool IsPreloaded { get; private set; }

	private bool _isDisposed;
	private IDisposable _root;
	private RenderTexture _renderTexture;
	private BoxTowerCorePm _core;
	private CancellationTokenSource _cancellationTokenSource;

	public ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
	{
		IsPreloaded = true;
		_renderTexture = RenderTextureUtils.GetRenderTexture(_camera);
		return default;
	}

	public RenderTexture GetRenderTexture()
	{
		return _renderTexture;
	}

	public void StartGame()
	{
		CreateRoot();
	}

	public void PauseGame()
	{
	}

	public void UnpauseGame()
	{
	}

	public void ResumeGame()
	{
	}

	public void RestartGame()
	{
		RecreateRoot();
	}

	public void StopGame()
	{
		Dispose();
	}

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		DisposeCore();

		_isDisposed = true;
	}

	private void RecreateRoot()
	{
		DisposeCore();

		CreateRoot();
	}

	private void DisposeCore()
	{
		if (_cancellationTokenSource != null)
		{
			if (!_cancellationTokenSource.IsCancellationRequested)
			{
				_cancellationTokenSource.Cancel();
			}

			_cancellationTokenSource.Dispose();

			_cancellationTokenSource = null;
		}

		_core?.Dispose();
		_core = null;
	}

	private void CreateRoot()
	{
		_cancellationTokenSource = new CancellationTokenSource();
		var rootCtx = new BoxTowerCorePm.Ctx
		{
			sceneContextView = _sceneContext,
			cancellationToken = _cancellationTokenSource.Token,
			restartGame = RestartGame
		};
		_core = BoxTowerCorePmFactory.CreateBoxTowerCorePm(rootCtx);
	}
}
}
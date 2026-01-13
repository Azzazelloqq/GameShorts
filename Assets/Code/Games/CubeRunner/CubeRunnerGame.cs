using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
using Cysharp.Threading.Tasks;
using Disposable;
using GameShorts.CubeRunner.Core;
using GameShorts.CubeRunner.View;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace GameShorts.CubeRunner
{
public class CubeRunnerGame : MonoBehaviourDisposable, IShortGame3D
{
	[SerializeField]
	private CubeRunnerSceneContextView _sceneContextView;

	[SerializeField]
	private Camera _camera;

	[SerializeField]
	private GraphicRaycaster _graphicRaycaster;

	public bool IsPreloaded { get; private set; }

	private IDisposable _core;
	private CancellationTokenSource _cancellationTokenSource;
	private bool _isDisposed;
	private RenderTexture _renderTexture;
	private readonly ReactiveProperty<bool> _isPaused = new();

	public async UniTask PreloadGameAsync(CancellationToken cancellationToken = default)
	{
		if (_isDisposed)
		{
			return;
		}

		if (_renderTexture == null)
		{
			_renderTexture = RenderTextureUtils.GetRenderTextureForShortGame(_camera);
		}

		if (IsPreloaded)
		{
			return;
		}

		if (_core == null)
		{
			CreateRoot(startPaused: true);
		}
		IsPreloaded = true;
	}

	public RenderTexture GetRenderTexture()
	{
		return _renderTexture;
	}

	public void StartGame()
	{
		if (_core == null)
		{
			CreateRoot(startPaused: false);
			return;
		}

		_isPaused.Value = false;
	}

	public void Disable()
	{
		gameObject.SetActive(false);
	}

	public void Enable()
	{
		gameObject.SetActive(true);
	}

	public void RestartGame()
	{
		RecreateRoot();
	}

	public void StopGame()
	{
		Disable();
		DisableInput();
	}

	public void EnableInput()
	{
		if (_graphicRaycaster != null)
		{
			_graphicRaycaster.enabled = true;
		}
	}

	public void DisableInput()
	{
		if (_graphicRaycaster != null)
		{
			_graphicRaycaster.enabled = false;
		}
	}

	public override void Dispose()
	{
        base.Dispose();
		if (_isDisposed)
		{
			return;
		}

		DisposeCore();
		RenderTextureUtils.ReleaseAndDestroy(ref _renderTexture, _camera);
		IsPreloaded = false;
		_isDisposed = true;
		Destroy(gameObject);
	}

	private void RecreateRoot()
	{
		DisposeCore();
		CreateRoot(false);
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

	private void CreateRoot(bool startPaused)
	{
		_isDisposed = false;
		_isPaused.Value = startPaused;
		_cancellationTokenSource = new CancellationTokenSource();
		var rootCtx = new CubeRunnerCorePm.Ctx
		{
			sceneContextView = _sceneContextView,
			cancellationToken = _cancellationTokenSource.Token,
			restartGame = RestartGame,
			isPaused = _isPaused
		};
		_core = new CubeRunnerCorePm(rootCtx);
	}
}
}
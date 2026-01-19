using System;
using System.Threading;
using System.Threading.Tasks;
using Asteroids.Code.Games.Game1.Scripts.View;
using Disposable;
using Code.Core.ShortGamesCore.Game1.Scripts.Core;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Asteroids.Code.Games.Game1
{
public class AsteroidsGame : MonoBehaviourDisposable, IShortGame2D
{
	[SerializeField]
	private MainSceneContextView _sceneContextView;

	[SerializeField]
	private Camera _camera;

	[SerializeField]
	private GraphicRaycaster _graphicRaycaster;

	public int Id => 1;
	public bool IsPreloaded { get; private set; }

	private IDisposable _core;
	private CancellationTokenSource _cancellationTokenSource;
	private bool _isDisposed;
	private RenderTexture _renderTexture;

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

		// Warm up UI/start screen creation so first entry doesn't hitch.
		if (_core == null)
		{
			CreateRoot();
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
			CreateRoot();
		}
	}

	public void Disable()
	{
		gameObject.SetActive(false);
	}

	public void Enable()
	{
		gameObject.SetActive(true);
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
		DisposeCore();
		Disable();
		DisableInput();
	}

	public void EnableInput()
	{
		_graphicRaycaster.enabled = true;
	}

	public void DisableInput()
	{
		_graphicRaycaster.enabled = false;
	}

	public void Dispose()
	{
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
		var rootCtx = new CorePm.Ctx
		{
			sceneContextView = _sceneContextView,
			cancellationToken = _cancellationTokenSource.Token,
			restartGame = RestartGame
		};
		_core = CorePmFactory.CreateCorePm(rootCtx);
	}
}
}
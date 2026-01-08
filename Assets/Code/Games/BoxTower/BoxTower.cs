using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
using Disposable;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.ShortGamesCore.Game2
{
public class BoxTower : MonoBehaviourDisposable, IShortGame2D
{
	[SerializeField]
	private Camera _camera;

	[SerializeField]
	private BoxTowerSceneContextView _sceneContext;

	[SerializeField]
	private GraphicRaycaster _graphicRaycaster;
	
	public bool IsPreloaded { get; private set; }

	private bool _isDisposed;
	private IDisposable _root;
	private RenderTexture _renderTexture;
	private BoxTowerCorePm _core;
	private CancellationTokenSource _cancellationTokenSource;

	public ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
	{
		IsPreloaded = true;
		_renderTexture = RenderTextureUtils.GetRenderTextureForShortGame(_camera);
		return default;
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
		else
		{
			_core.ResetForNewSession();
		}
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
		if (_core == null)
		{
			CreateRoot();
		}
		else
		{
			_core.ResetForNewSession();
		}
	}

	public void StopGame()
	{
		PauseGame();
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
using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Core;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Level;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.View;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
using Cysharp.Threading.Tasks;
using Disposable;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Code.Core.ShortGamesCore.Lawnmower
{
public class LawnmowerGame : MonoBehaviourDisposable, IShortGame2D
{
	[FormerlySerializedAs("sceneContextView")]
	[SerializeField]
	private LawnmowerSceneContextView _sceneContextView;

	[SerializeField]
	private Camera _camera;
	
	[SerializeField]
	private GraphicRaycaster _graphicRaycaster;
	
	private IDisposable _core;
	private CancellationTokenSource _cancellationTokenSource;

	public bool IsPreloaded { get; private set; }

	private bool _isDisposed;
	private RenderTexture _renderTexture;
	private UniTask _preloadTask;
	private bool _isPreloading;

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

		if (_isPreloading)
		{
			await _preloadTask;
			return;
		}

		// Warm up start screen and level manager creation.
		if (_core == null)
		{
			CreateRoot();
		}

		_isPreloading = true;
		_preloadTask = PreloadInternalAsync(cancellationToken).Preserve();
		try
		{
			await _preloadTask;
			if (_isDisposed)
			{
				return;
			}

			IsPreloaded = true;
		}
		finally
		{
			_isPreloading = false;
		}
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
		// TODO: Реализовать возобновление если нужно
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

	private async UniTask PreloadInternalAsync(CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// Allow Unity to run Start/OnEnable to initialize heavy scene components.
		await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

		if (_isDisposed)
		{
			return;
		}

		WarmUpCurrentLevelGrass();
	}

	private void WarmUpCurrentLevelGrass()
	{
		if (_sceneContextView == null)
		{
			return;
		}

		var currentLevel = _sceneContextView.CurrentLevel;
		if (currentLevel == null)
		{
			return;
		}

		var grassFields = currentLevel.GrassFields;
		if (grassFields == null || grassFields.Length == 0)
		{
			grassFields = currentLevel.GetComponentsInChildren<GrassFieldView>(true);
		}

		foreach (var grassField in grassFields)
		{
			if (grassField == null)
			{
				continue;
			}

			var grid = grassField.GrassGrid;
			if (grid == null)
			{
				continue;
			}

			grid.EnsureInitialized();
		}
	}

	private void CreateRoot()
	{
		_cancellationTokenSource = new CancellationTokenSource();

		var rootCtx = new LawnmowerCorePm.Ctx
		{
			sceneContextView = _sceneContextView,
			cancellationToken = _cancellationTokenSource.Token,
			restartGame = RestartGame
		};

		_core = new LawnmowerCorePm(rootCtx);
	}
}
}
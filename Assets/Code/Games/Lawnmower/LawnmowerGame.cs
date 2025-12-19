using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Core;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.View;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
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

	public ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
	{
		_renderTexture = RenderTextureUtils.GetRenderTextureForShortGame(_camera);
		IsPreloaded = true;
		
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
		// TODO: Реализовать паузу если нужно
	}

	public void UnpauseGame()
	{
		throw new NotImplementedException();
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
		Dispose();
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
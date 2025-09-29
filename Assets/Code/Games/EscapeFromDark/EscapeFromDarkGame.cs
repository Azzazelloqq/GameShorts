using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Core;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Code.Core.ShortGamesCore.EscapeFromDark
{
public class EscapeFromDarkGame : BaseMonoBehaviour, IShortGame2D
{
	[FormerlySerializedAs("sceneContextView")]
	[SerializeField]
	private EscapeFromDarkSceneContextView _sceneContextView;

	[SerializeField]
	private Camera _camera; 
	
	[SerializeField]
	private GraphicRaycaster _graphicRaycaster;
	
	private IDisposable _core;
	private CancellationTokenSource _cancellationTokenSource;
	private bool _isDisposed;
	private RenderTexture _renderTexture;

	public bool IsPreloaded { get; private set; }

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
		// TODO: Реализовать паузу игры
	}

	public void UnpauseGame()
	{
	}

	public void ResumeGame()
	{
		// TODO: Реализовать возобновление игры
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
		var rootCtx = new EscapeFromDarkCorePm.Ctx
		{
			sceneContextView = _sceneContextView,
			cancellationToken = _cancellationTokenSource.Token,
			restartGame = RestartGame
		};
		_core = new EscapeFromDarkCorePm(rootCtx);
	}
}
}
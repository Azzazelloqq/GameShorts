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

	private IDisposable _root;
	private RenderTexture _renderTexture;
	public bool IsPreloaded { get; private set; }

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

	public void Dispose()
	{
		_root?.Dispose();
		_root = null;
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

	public void RestartGame()
	{
		CreateRoot();
	}

	public void StopGame()
	{
		_root?.Dispose();
	}

	private void CreateRoot()
	{
	}
}
}
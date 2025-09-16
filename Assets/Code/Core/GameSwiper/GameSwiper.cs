using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.GamesLoader;
using Code.Core.ShortGamesCore.Source.GameCore;
using InGameLogger;
using LightDI.Runtime;
using UnityEngine;

namespace Code.Core.GameSwiper
{
/// <summary>
/// Implementation of ISwiperGame interface for managing game switching
/// </summary>
public class GameSwiper : ISwiperGame
{
	private readonly GameSwiperService _swiperService;
	private readonly IInGameLogger _logger;
	private bool _disposed;

	public event Action<GameSwiperService.TransitionState> OnTransitionStateChanged
	{
		add => _swiperService.OnTransitionStateChanged += value;
		remove => _swiperService.OnTransitionStateChanged -= value;
	}

	public event Action<Type, Type> OnGameChanged
	{
		add => _swiperService.OnGameChanged += value;
		remove => _swiperService.OnGameChanged -= value;
	}

	public bool CanSwipeNext => _swiperService.CanSwipeNext;
	public bool CanSwipePrevious => _swiperService.CanSwipePrevious;
	public bool IsTransitioning => _swiperService.IsTransitioning;

	/// <summary>
	/// GameSwiper constructor
	/// </summary>
	/// <param name="gameProvider">Game provider</param>
	/// <param name="logger">Logger for operation tracking</param>
	public GameSwiper(
		[Inject] IGameProvider gameProvider,
		[Inject] IInGameLogger logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_swiperService = new GameSwiperService(gameProvider, logger);
	}

	public (RenderTexture current, RenderTexture next, RenderTexture previous) GetRenderTextures()
	{
		return _swiperService.GetRenderTextures();
	}

	/// <summary>
	/// Switch to the next game
	/// </summary>
	/// <param name="cancellationToken">Operation cancellation token</param>
	/// <returns>true if switch successful, false otherwise</returns>
	public async Task<bool> SwipeToNextGameAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			_logger.Log("GameSwiper: Switching to next game");
			var success = await _swiperService.SwipeToNextGameAsync(cancellationToken);

			if (success)
			{
				_logger.Log("GameSwiper: Successfully switched to next game");
			}
			else
			{
				_logger.LogWarning("GameSwiper: Failed to switch to next game");
			}

			return success;
		}
		catch (Exception ex)
		{
			_logger.LogError($"GameSwiper: Error switching to next game: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Switch to the previous game
	/// </summary>
	/// <param name="cancellationToken">Operation cancellation token</param>
	/// <returns>true if switch successful, false otherwise</returns>
	public async Task<bool> SwipeToPreviousGameAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			_logger.Log("GameSwiper: Switching to previous game");
			var success = await _swiperService.SwipeToPreviousGameAsync(cancellationToken);

			if (success)
			{
				_logger.Log("GameSwiper: Successfully switched to previous game");
			}
			else
			{
				_logger.LogWarning("GameSwiper: Failed to switch to previous game");
			}

			return success;
		}
		catch (Exception ex)
		{
			_logger.LogError($"GameSwiper: Error switching to previous game: {ex.Message}");
			return false;
		}
	}

	public void PauseAll()
	{
		_swiperService.PauseAll();
	}

	public void ResumeCurrent()
	{
		_swiperService.ResumeCurrent();
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_logger.Log("Disposing GameSwiper");
		_swiperService?.Dispose();
	}
}
}
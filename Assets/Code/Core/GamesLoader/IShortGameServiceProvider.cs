using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using UnityEngine;

namespace Code.Core.GamesLoader
{
/// <summary>
/// Bridge provider between user and game management services (IGameLoader and IGameQueueService)
/// </summary>
public interface IShortGameServiceProvider : IDisposable
{
	/// <summary>
	/// Gets the current game
	/// </summary>
	IShortGame CurrentGame { get; }

	/// <summary>
	/// Gets the next game (preloaded)
	/// </summary>
	IShortGame NextGame { get; }

	/// <summary>
	/// Gets the previous game (preloaded)
	/// </summary>
	IShortGame PreviousGame { get; }

	/// <summary>
	/// Gets the render texture of the current game
	/// </summary>
	RenderTexture CurrentGameRenderTexture { get; }

	/// <summary>
	/// Gets the render texture of the next game
	/// </summary>
	RenderTexture NextGameRenderTexture { get; }

	/// <summary>
	/// Gets the render texture of the previous game
	/// </summary>
	RenderTexture PreviousGameRenderTexture { get; }

	/// <summary>
	/// Checks if there is a current game
	/// </summary>
	bool HasCurrentGame { get; }

	/// <summary>
	/// Checks if there is a next game
	/// </summary>
	bool HasNextGame { get; }

	/// <summary>
	/// Checks if there is a previous game
	/// </summary>
	bool HasPreviousGame { get; }

	/// <summary>
	/// Checks if the next game is ready to be played
	/// </summary>
	bool IsNextGameReady { get; }

	/// <summary>
	/// Checks if the previous game is ready to be played
	/// </summary>
	bool IsPreviousGameReady { get; }

	/// <summary>
	/// Checks if the current game is ready to be played
	/// </summary>
	bool IsCurrentGameReady { get; }

	ValueTask InitializeAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Starts the current game
	/// </summary>
	void StartCurrentGame();

	/// <summary>
	/// Starts the next game
	/// </summary>
	void StartNextGame();

	/// <summary>
	/// Starts the previous game
	/// </summary>
	void StartPreviousGame();

	/// <summary>
	/// Pauses the current game
	/// </summary>
	void PauseCurrentGame();

	/// <summary>
	/// Unpauses the current game
	/// </summary>
	void UnpauseCurrentGame();

	/// <summary>
	/// Pauses the next game
	/// </summary>
	void PauseNextGame();

	/// <summary>
	/// Unpauses the next game
	/// </summary>
	void UnpauseNextGame();

	/// <summary>
	/// Pauses the previous game
	/// </summary>
	void PausePreviousGame();

	/// <summary>
	/// Unpauses the previous game
	/// </summary>
	void UnpausePreviousGame();

	/// <summary>
	/// Pauses all games
	/// </summary>
	void PauseAllGames();

	/// <summary>
	/// Unpauses all games
	/// </summary>
	void UnpauseAllGames();

	/// <summary>
	/// Stops the current game
	/// </summary>
	void StopCurrentGame();

	/// <summary>
	/// Stops the next game
	/// </summary>
	void StopNextGame();

	/// <summary>
	/// Stops the previous game
	/// </summary>
	void StopPreviousGame();

	/// <summary>
	/// Stops all games
	/// </summary>
	void StopAllGames();

	/// <summary>
	/// Enables input for the current game
	/// </summary>
	void EnableCurrentGameInput();

	/// <summary>
	/// Disables input for the current game
	/// </summary>
	void DisableCurrentGameInput();

	/// <summary>
	/// Enables input for the next game
	/// </summary>
	void EnableNextGameInput();

	/// <summary>
	/// Disables input for the next game
	/// </summary>
	void DisableNextGameInput();

	/// <summary>
	/// Enables input for the previous game
	/// </summary>
	void EnablePreviousGameInput();

	/// <summary>
	/// Disables input for the previous game
	/// </summary>
	void DisablePreviousGameInput();

	/// <summary>
	/// Enables input for all games
	/// </summary>
	void EnableAllGamesInput();

	/// <summary>
	/// Disables input for all games
	/// </summary>
	void DisableAllGamesInput();

	/// <summary>
	/// Updates preloaded games based on current queue position
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	ValueTask UpdatePreloadedGamesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Switch to next game in queue
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>true if successful, false otherwise</returns>
	Task<bool> SwipeToNextGameAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Switch to previous game in queue
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>true if successful, false otherwise</returns>
	Task<bool> SwipeToPreviousGameAsync(CancellationToken cancellationToken = default);
}
}
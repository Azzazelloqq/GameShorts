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
    /// Gets the <see cref="Type"/> of the current game, even if it hasn't been instantiated yet.
    /// </summary>
    Type CurrentGameType { get; }

    /// <summary>
    /// Gets the next game (preloaded)
    /// </summary>
    IShortGame NextGame { get; }

    /// <summary>
    /// Gets the <see cref="Type"/> of the next game slot.
    /// </summary>
    Type NextGameType { get; }

    /// <summary>
    /// Gets the previous game (preloaded)
    /// </summary>
    IShortGame PreviousGame { get; }

    /// <summary>
    /// Gets the <see cref="Type"/> of the previous game slot.
    /// </summary>
    Type PreviousGameType { get; }

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

	/// <summary>
	/// Fired when a game finishes preloading.
	/// </summary>
	event Action<Type> OnGamePreloadingCompleted;

    /// <summary>
    /// Readiness timeout used by the loader when waiting for preloaded games.
    /// </summary>
    TimeSpan ReadinessTimeout { get; }

    /// <summary>
    /// Number of fallback attempts to force-preload the current game.
    /// </summary>
    int FallbackLoadAttempts { get; }

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
    /// Disables the current game (best-effort: should just toggle its root GameObject off)
    /// </summary>
    void DisableCurrentGame();

    /// <summary>
    /// Enables the current game (best-effort: should just toggle its root GameObject on)
    /// </summary>
    void EnableCurrentGame();

    /// <summary>
    /// Disables the next game (best-effort: should just toggle its root GameObject off)
    /// </summary>
    void DisableNextGame();

    /// <summary>
    /// Enables the next game (best-effort: should just toggle its root GameObject on)
    /// </summary>
    void EnableNextGame();

    /// <summary>
    /// Disables the previous game (best-effort: should just toggle its root GameObject off)
    /// </summary>
    void DisablePreviousGame();

    /// <summary>
    /// Enables the previous game (best-effort: should just toggle its root GameObject on)
    /// </summary>
    void EnablePreviousGame();

    /// <summary>
    /// Disables all games (best-effort: should just toggle their root GameObjects off)
    /// </summary>
    void DisableAllGames();

    /// <summary>
    /// Enables all games (best-effort: should just toggle their root GameObjects on)
    /// </summary>
    void EnableAllGames();

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
    /// Ensures the current game is preloaded (fallback) if background preload is slow or failed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    ValueTask<bool> EnsureCurrentGamePreloadedAsync(CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Sets which neighbour game is enabled for swipe previews (without changing the active game).
    /// Contract: at any moment, only ONE game should be enabled:
    /// - if enableNext == true: enable Next, disable Current and Previous
    /// - if enablePrevious == true: enable Previous, disable Current and Next
    /// - otherwise: enable Current, disable Next and Previous
    /// </summary>
    void SetNeighbourRenderingEnabled(bool enableNext, bool enablePrevious);
}
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;

namespace Code.Core.GamesLoader
{
/// <summary>
/// Universal interface for game loading strategies
/// </summary>
public interface IGamesLoader : IDisposable
{
	/// <summary>
	/// Event fired when a game starts loading
	/// </summary>
	public event Action<Type> OnGameLoadingStarted;

	/// <summary>
	/// Event fired when a game finishes loading
	/// </summary>
	public event Action<Type, IShortGame> OnGameLoadingCompleted;

	/// <summary>
	/// Event fired when a game loading fails
	/// </summary>
	public event Action<Type, Exception> OnGameLoadingFailed;

	/// <summary>
	/// Gets the currently loaded game
	/// </summary>
	public IShortGame CurrentGame { get; }

	/// <summary>
	/// Gets whether the loader is currently loading a game
	/// </summary>
	public bool IsLoading { get; }

	/// <summary>
	/// Initializes the loader with a collection of game types
	/// </summary>
	/// <param name="gameTypes">Collection of game types available for loading</param>
	/// <param name="cancellationToken">Cancellation token</param>
	public ValueTask InitializeAsync(IReadOnlyList<Type> gameTypes, CancellationToken cancellationToken = default);

	/// <summary>
	/// Loads the next game according to the loader's strategy
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The loaded game or null if no next game available</returns>
	public ValueTask<IShortGame> LoadNextGameAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Loads the previous game according to the loader's strategy
	/// </summary>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The loaded game or null if no previous game available</returns>
	public ValueTask<IShortGame> LoadPreviousGameAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Loads a specific game by type
	/// </summary>
	/// <param name="gameType">Type of the game to load</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The loaded game or null if unable to load</returns>
	public ValueTask<IShortGame> LoadGameAsync(Type gameType, CancellationToken cancellationToken = default);

	/// <summary>
	/// Stops the current game
	/// </summary>
	public void StopCurrentGame();

	/// <summary>
	/// Resets the loader to its initial state
	/// </summary>
	public void Reset();
}
}
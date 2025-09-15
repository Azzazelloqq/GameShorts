using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;

namespace Code.Core.GamesLoader
{
/// <summary>
/// Interface for loading and preloading games
/// </summary>
internal interface IGamesLoader : IDisposable
{
	/// <summary>
	/// Event fired when a game starts loading
	/// </summary>
	event Action<Type> OnGameLoadingStarted;

	/// <summary>
	/// Event fired when a game finishes loading
	/// </summary>
	event Action<Type, IShortGame> OnGameLoadingCompleted;

	/// <summary>
	/// Event fired when a game loading fails
	/// </summary>
	event Action<Type, Exception> OnGameLoadingFailed;

	/// <summary>
	/// Event fired when a game starts preloading
	/// </summary>
	event Action<Type> OnGamePreloadingStarted;

	/// <summary>
	/// Event fired when a game finishes preloading
	/// </summary>
	event Action<Type, IShortGame> OnGamePreloadingCompleted;

	/// <summary>
	/// Event fired when a game preloading fails
	/// </summary>
	event Action<Type, Exception> OnGamePreloadingFailed;

	/// <summary>
	/// Gets whether the loader is currently loading a game
	/// </summary>
	bool IsLoading { get; }

	/// <summary>
	/// Gets the currently loaded games (game type to instance mapping)
	/// </summary>
	IReadOnlyDictionary<Type, IShortGame> LoadedGames { get; }

	/// <summary>
	/// Gets the currently preloaded games (game type to instance mapping)
	/// </summary>
	IReadOnlyDictionary<Type, IShortGame> PreloadedGames { get; }

	/// <summary>
	/// Loads a specific game by type and starts it
	/// </summary>
	/// <param name="gameType">Type of the game to load</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The loaded game instance or null if unable to load</returns>
	ValueTask<IShortGame> LoadGameAsync(Type gameType, CancellationToken cancellationToken = default);

	/// <summary>
	/// Preloads a specific game by type without starting it
	/// </summary>
	/// <param name="gameType">Type of the game to preload</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The preloaded game instance or null if unable to preload</returns>
	ValueTask<IShortGame> PreloadGameAsync(Type gameType, CancellationToken cancellationToken = default);

	/// <summary>
	/// Preloads multiple games
	/// </summary>
	/// <param name="gameTypes">Collection of game types to preload</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>Dictionary of preloaded games</returns>
	ValueTask<IReadOnlyDictionary<Type, IShortGame>> PreloadGamesAsync(
		IEnumerable<Type> gameTypes,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Starts a preloaded game
	/// </summary>
	/// <param name="gameType">Type of the preloaded game to start</param>
	/// <returns>True if the game was started successfully</returns>
	bool StartPreloadedGame(Type gameType);

	/// <summary>
	/// Unloads a specific game
	/// </summary>
	/// <param name="gameType">Type of the game to unload</param>
	void UnloadGame(Type gameType);

	/// <summary>
	/// Unloads all loaded and preloaded games
	/// </summary>
	void UnloadAllGames();

	/// <summary>
	/// Gets a loaded or preloaded game by type
	/// </summary>
	/// <param name="gameType">Type of the game</param>
	/// <returns>Game instance or null if not found</returns>
	IShortGame GetGame(Type gameType);

	/// <summary>
	/// Checks if a game is loaded or preloaded
	/// </summary>
	/// <param name="gameType">Type of the game</param>
	/// <returns>True if the game is loaded or preloaded</returns>
	bool IsGameLoaded(Type gameType);
}
}
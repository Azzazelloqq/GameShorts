using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;

namespace Code.Core.ShortGamesCore.Source.LifeCycleService
{
public interface IShortGameLifeCycleService : IDisposable
{
	/// <summary>
	/// Current active game
	/// </summary>
	IShortGame CurrentGame { get; }
	
	/// <summary>
	/// Preloads a list of games for quick switching
	/// </summary>
	ValueTask PreloadGamesAsync(IEnumerable<Type> gameTypes, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Preloads a single game
	/// </summary>
	ValueTask PreloadGameAsync<T>(CancellationToken cancellationToken = default) where T : class, IShortGame;
	
	/// <summary>
	/// Loads and starts a game. If a game is currently active, it will be stopped and returned to pool
	/// </summary>
	ValueTask<T> LoadGameAsync<T>(CancellationToken cancellationToken = default) where T : class, IShortGame;
	
	/// <summary>
	/// Loads and starts a game by type
	/// </summary>
	ValueTask<IShortGame> LoadGameAsync(Type gameType, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Stops the current game and returns it to pool
	/// </summary>
	void StopCurrentGame();
	
	/// <summary>
	/// Loads the next game from the preloaded list
	/// </summary>
	ValueTask<IShortGame> LoadNextGameAsync(CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Loads the previous game from the preloaded list
	/// </summary>
	ValueTask<IShortGame> LoadPreviousGameAsync(CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Clears all preloaded games from the pool
	/// </summary>
	void ClearPreloadedGames();
}
}
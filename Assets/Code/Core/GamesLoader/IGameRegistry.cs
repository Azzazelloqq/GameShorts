using System;
using System.Collections.Generic;

namespace Code.Core.GamesLoader
{
/// <summary>
/// Registry of all available games in the system
/// </summary>
public interface IGameRegistry : IDisposable
{
	/// <summary>
	/// Event fired when a new game is registered
	/// </summary>
	event Action<Type> OnGameRegistered;

	/// <summary>
	/// Event fired when a game is unregistered
	/// </summary>
	event Action<Type> OnGameUnregistered;

	/// <summary>
	/// Gets all registered game types
	/// </summary>
	IReadOnlyList<Type> RegisteredGames { get; }

	/// <summary>
	/// Gets the count of registered games
	/// </summary>
	int Count { get; }

	/// <summary>
	/// Registers a new game type
	/// </summary>
	/// <param name="gameType">Type of the game to register</param>
	/// <returns>True if registered successfully, false if already registered</returns>
	bool RegisterGame(Type gameType);

	/// <summary>
	/// Registers multiple game types
	/// </summary>
	/// <param name="gameTypes">Collection of game types to register</param>
	void RegisterGames(IEnumerable<Type> gameTypes);

	/// <summary>
	/// Unregisters a game type
	/// </summary>
	/// <param name="gameType">Type of the game to unregister</param>
	/// <returns>True if unregistered successfully</returns>
	bool UnregisterGame(Type gameType);

	/// <summary>
	/// Checks if a game type is registered
	/// </summary>
	/// <param name="gameType">Type of the game to check</param>
	/// <returns>True if the game is registered</returns>
	bool IsGameRegistered(Type gameType);

	/// <summary>
	/// Gets a game type by index
	/// </summary>
	/// <param name="index">Index of the game</param>
	/// <returns>Game type or null if index is invalid</returns>
	Type GetGameTypeByIndex(int index);

	/// <summary>
	/// Gets the index of a game type
	/// </summary>
	/// <param name="gameType">Type of the game</param>
	/// <returns>Index or -1 if not found</returns>
	int GetIndexOfGameType(Type gameType);

}
}
using System;
using System.Collections.Generic;

namespace Code.Core.GamesLoader
{
/// <summary>
/// Service responsible for managing the queue of games
/// </summary>
internal interface IGameQueueService : IDisposable
{
	/// <summary>
	/// Event fired when the game queue is updated
	/// </summary>
	event Action OnQueueUpdated;

	/// <summary>
	/// Gets the current game type
	/// </summary>
	Type CurrentGameType { get; }

	/// <summary>
	/// Gets the next game type in the queue
	/// </summary>
	Type NextGameType { get; }

	/// <summary>
	/// Gets the previous game type in the queue
	/// </summary>
	Type PreviousGameType { get; }

	/// <summary>
	/// Gets the current game index in the queue
	/// </summary>
	int CurrentIndex { get; }

	/// <summary>
	/// Gets the total number of games in the queue
	/// </summary>
	int TotalGamesCount { get; }

	/// <summary>
	/// Checks if there is a next game in the queue
	/// </summary>
	bool HasNext { get; }

	/// <summary>
	/// Checks if there is a previous game in the queue
	/// </summary>
	bool HasPrevious { get; }

	/// <summary>
	/// Initializes the queue with a list of game types
	/// </summary>
	/// <param name="gameTypes">List of game types</param>
	void Initialize(IReadOnlyList<Type> gameTypes);

	/// <summary>
	/// Moves to the next game in the queue
	/// </summary>
	/// <returns>True if moved successfully, false if already at the end</returns>
	bool MoveNext();

	/// <summary>
	/// Moves to the previous game in the queue
	/// </summary>
	/// <returns>True if moved successfully, false if already at the beginning</returns>
	bool MovePrevious();

	/// <summary>
	/// Moves to a specific index in the queue
	/// </summary>
	/// <param name="index">Target index</param>
	/// <returns>True if moved successfully, false if index is invalid</returns>
	bool MoveToIndex(int index);

	/// <summary>
	/// Gets the game type at a specific index
	/// </summary>
	/// <param name="index">Index in the queue</param>
	/// <returns>Game type or null if index is invalid</returns>
	Type GetGameTypeAtIndex(int index);

	/// <summary>
	/// Gets all game types that should be preloaded (current, next, previous)
	/// </summary>
	/// <returns>Collection of game types to preload</returns>
	IEnumerable<Type> GetGamesToPreload();

	/// <summary>
	/// Resets the queue to the beginning
	/// </summary>
	void Reset();

	/// <summary>
	/// Clears the queue
	/// </summary>
	void Clear();
}
}
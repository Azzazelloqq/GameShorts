using System;
using System.Collections.Generic;

namespace Code.Core.GamesLoader
{
/// <summary>
/// Service responsible for managing the queue of games
/// </summary>
public interface IGameQueueService : IDisposable
{
	/// <summary>
	/// Event fired when the game queue is updated together with a snapshot and reason.
	/// </summary>
	event Action<IReadOnlyList<Type>, int, QueueChangeReason> OnQueueUpdated;

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
	/// Gets all game types that should be preloaded around the cursor (current ± radius).
	/// </summary>
	/// <param name="radius">How many neighbours on each side to include.</param>
	/// <returns>Collection of game types to preload</returns>
	IEnumerable<Type> GetGamesToPreload(int radius = 1);

	/// <summary>
	/// Inserts a new game type into the queue while keeping the current cursor stable.
	/// </summary>
	/// <param name="index">Target index.</param>
	/// <param name="gameType">Type to insert.</param>
	void InsertAt(int index, Type gameType);

	/// <summary>
	/// Removes a game type at the provided index.
	/// </summary>
	/// <param name="index">Index to remove.</param>
	/// <returns>True when removal happened.</returns>
	bool RemoveAt(int index);

	/// <summary>
	/// Removes the first occurrence of a specific game type.
	/// </summary>
	/// <param name="gameType">Type to remove.</param>
	/// <returns>True when removal happened.</returns>
	bool Remove(Type gameType);

	/// <summary>
	/// Replaces the entire queue contents with the supplied collection.
	/// </summary>
	void ReplaceAll(IEnumerable<Type> gameTypes);

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
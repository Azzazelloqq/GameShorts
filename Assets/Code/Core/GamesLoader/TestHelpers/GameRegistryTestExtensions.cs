using System;
using InGameLogger;

namespace Code.Core.GamesLoader.TestHelpers
{
	/// <summary>
	/// Extension methods for GameRegistry testing
	/// </summary>
	internal static class GameRegistryTestExtensions
	{
		/// <summary>
		/// Clears all registered games from the registry (for testing purposes)
		/// </summary>
		public static void Clear(this GameRegistry registry)
		{
			// Get logger via reflection if needed for logging
			var loggerField = typeof(GameRegistry).GetField("_logger", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var logger = loggerField?.GetValue(registry) as IInGameLogger;
			
			logger?.Log("Clearing game registry");
			
			// Unregister all games
			var registeredGames = registry.RegisteredGames;
			var gamesToRemove = new Type[registeredGames.Count];
			for (int i = 0; i < registeredGames.Count; i++)
			{
				gamesToRemove[i] = registeredGames[i];
			}
			
			foreach (var gameType in gamesToRemove)
			{
				registry.UnregisterGame(gameType);
			}
		}
	}
}

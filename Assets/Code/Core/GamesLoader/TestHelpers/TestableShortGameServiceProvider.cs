using System;
using Code.Core.ShortGamesCore.Source.Factory;
using InGameLogger;

namespace Code.Core.GamesLoader.TestHelpers
{
	/// <summary>
	/// Testable version of ShortGameServiceProvider that exposes internal services for testing
	/// </summary>
	internal class TestableShortGameServiceProvider : ShortGameServiceProvider
	{
		public TestableShortGameServiceProvider(IInGameLogger logger, IShortGameFactory gameFactory) 
			: base(logger, Array.Empty<Type>(), gameFactory)
		{
		}

		// Expose internal services for testing via reflection
		public IGameRegistry TestGameRegistry 
		{
			get
			{
				var field = typeof(ShortGameServiceProvider).GetField("_gameRegistry", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				return field?.GetValue(this) as IGameRegistry;
			}
		}

		public IGameQueueService TestQueueService 
		{
			get
			{
				var field = typeof(ShortGameServiceProvider).GetField("_queueService", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				return field?.GetValue(this) as IGameQueueService;
			}
		}

		public IGamesLoader TestGamesLoader 
		{
			get
			{
				var field = typeof(ShortGameServiceProvider).GetField("_gamesLoader", 
					System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				return field?.GetValue(this) as IGamesLoader;
			}
		}
	}
}

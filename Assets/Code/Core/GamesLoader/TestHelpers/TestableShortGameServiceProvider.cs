using InGameLogger;

namespace Code.Core.GamesLoader.TestHelpers
{
	/// <summary>
	/// Testable version of ShortGameServiceProvider that exposes internal services for testing
	/// </summary>
internal class TestableShortGameServiceProvider : ShortGameServiceProvider
{
	public TestableShortGameServiceProvider(
		IInGameLogger logger,
		IGameRegistry registry,
		IGameQueueService queueService,
		IGamesLoader gamesLoader,
		ShortGameLoaderSettings settings = null)
		: base(logger, registry, queueService, gamesLoader, settings ?? new ShortGameLoaderSettings())
	{
		TestGameRegistry = registry;
		TestQueueService = queueService;
		TestGamesLoader = gamesLoader;
	}

	public IGameRegistry TestGameRegistry { get; }

	public IGameQueueService TestQueueService { get; }

	public IGamesLoader TestGamesLoader { get; }
}
}

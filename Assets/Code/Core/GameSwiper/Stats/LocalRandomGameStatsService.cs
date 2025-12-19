using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InGameLogger;

namespace Code.Core.GameStats
{
	/// <summary>
	/// Temporary in-memory stats service that seeds random values per game type.
	/// </summary>
	public class LocalRandomGameStatsService : IGameStatsService
	{
		private readonly Dictionary<Type, GameStatsData> _statsCache = new();
		private readonly object _lock = new();
		private readonly Random _random;
		private readonly IInGameLogger _logger;

		public LocalRandomGameStatsService(IInGameLogger logger, int? seed = null)
		{
			_logger = logger;
			_random = seed.HasValue ? new Random(seed.Value) : new Random(Environment.TickCount);
		}

		public ValueTask<GameStatsData> GetStatsAsync(Type gameType, CancellationToken cancellationToken = default)
		{
			if (gameType == null)
			{
				throw new ArgumentNullException(nameof(gameType));
			}

			var result = GetOrCreate(gameType);
			return new ValueTask<GameStatsData>(result);
		}

		public ValueTask<GameStatsData> SubmitVoteAsync(Type gameType, GameVoteType voteType,
			CancellationToken cancellationToken = default)
		{
			if (gameType == null)
			{
				throw new ArgumentNullException(nameof(gameType));
			}

			var updated = UpdateStats(gameType, voteType);
			return new ValueTask<GameStatsData>(updated);
		}

		private GameStatsData GetOrCreate(Type gameType)
		{
			lock (_lock)
			{
				if (_statsCache.TryGetValue(gameType, out var stats))
				{
					return stats;
				}

				var seededStats = new GameStatsData(_random.Next(5, 250), _random.Next(0, 120));
				_statsCache[gameType] = seededStats;
				_logger?.Log($"[Stats] Seeded {gameType.Name} with {seededStats.Likes}/{seededStats.Dislikes}");

				return seededStats;
			}
		}

		private GameStatsData UpdateStats(Type gameType, GameVoteType voteType)
		{
			lock (_lock)
			{
				var current = GetOrCreate(gameType);
				var updated = current.Increment(voteType);
				_statsCache[gameType] = updated;

				return updated;
			}
		}
	}
}













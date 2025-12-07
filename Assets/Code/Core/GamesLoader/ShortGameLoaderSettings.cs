using System;

namespace Code.Core.GamesLoader
{
	/// <summary>
	/// Configuration object that describes how the queue-based games loader should behave.
	/// </summary>
	public sealed class ShortGameLoaderSettings
	{
		private static readonly TimeSpan DefaultReadinessTimeout = TimeSpan.FromSeconds(10);

		/// <summary>
		/// Default settings that mimic the previous behaviour (current + neighbours preloaded, 10s timeout).
		/// </summary>

		public TimeSpan ReadinessTimeout { get; }

		/// <summary>
		/// How many neighbours (on each side) should stay preloaded around the current cursor.
		/// </summary>
		public int PreloadRadius { get; }

		/// <summary>
		/// How many started games the loader is allowed to keep alive at once.
		/// </summary>
		public int MaxLoadedGames { get; }

		/// <summary>
		/// How many synchronous fallback attempts are allowed when a preloaded instance is missing.
		/// </summary>
		public int FallbackLoadAttempts { get; }

		public ShortGameLoaderSettings(
			TimeSpan? readinessTimeout = null,
			int preloadRadius = 1,
			int maxLoadedGames = 3,
			int fallbackLoadAttempts = 1)
		{
			if (preloadRadius < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(preloadRadius), "Preload radius cannot be negative.");
			}

			if (maxLoadedGames < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(maxLoadedGames), "At least one loaded game must be allowed.");
			}

			if (fallbackLoadAttempts < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(fallbackLoadAttempts), "At least one fallback attempt is required.");
			}

			ReadinessTimeout = readinessTimeout ?? DefaultReadinessTimeout;
			PreloadRadius = preloadRadius;
			MaxLoadedGames = maxLoadedGames;
			FallbackLoadAttempts = fallbackLoadAttempts;
		}
	}
}

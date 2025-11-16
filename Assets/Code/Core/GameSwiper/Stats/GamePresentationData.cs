using System;

namespace Code.Core.GameStats
{
public readonly struct GamePresentationData
{
	public Type GameType { get; }
	public string DisplayName { get; }
	public GameStatsData StatsData { get; }
	
	public GamePresentationData(Type gameType, string displayName, GameStatsData statsData)
	{
		GameType = gameType ?? throw new ArgumentNullException(nameof(gameType));
		DisplayName = string.IsNullOrWhiteSpace(displayName) ? gameType.Name : displayName;
		StatsData = statsData;
	}
}
}
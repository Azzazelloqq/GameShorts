using System;

namespace Code.Core.GameStats
{
public readonly struct GameStatsData
{
	public int Likes { get; }
	public int Dislikes { get; }

	public int TotalVotes => Likes + Dislikes;

	public float LikeRatio => TotalVotes == 0 ? 0f : (float)Likes / TotalVotes;

	public GameStatsData(int likes, int dislikes)
	{
		Likes = Math.Max(0, likes);
		Dislikes = Math.Max(0, dislikes);
	}
	
	public GameStatsData Increment(GameVoteType voteType)
	{
		return voteType switch
		{
			GameVoteType.Like => new GameStatsData(Likes + 1, Dislikes),
			GameVoteType.Dislike => new GameStatsData(Likes, Dislikes + 1),
			_ => this
		};
	}
}
}
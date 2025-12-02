using System;
using System.Threading;
using System.Threading.Tasks;

namespace Code.Core.GameStats
{
	public interface IGameStatsService
	{
		ValueTask<GameStatsData> GetStatsAsync(Type gameType, CancellationToken cancellationToken = default);

		ValueTask<GameStatsData> SubmitVoteAsync(Type gameType, GameVoteType voteType,
			CancellationToken cancellationToken = default);
	}
}






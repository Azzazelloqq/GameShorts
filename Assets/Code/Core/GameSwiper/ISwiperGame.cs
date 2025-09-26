using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;

namespace Code.Core.GameSwiper
{
    /// <summary>
    /// Интерфейс для игр, поддерживающих свайп-навигацию между играми
    /// </summary>
    public interface ISwiperGame
    {
        /// <summary>
        /// Переключиться на следующую игру
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Следующая игра или null, если переключение невозможно</returns>
        ValueTask<IShortGame> NextGameAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Переключиться на предыдущую игру
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Предыдущая игра или null, если переключение невозможно</returns>
        ValueTask<IShortGame> PreviousGameAsync(CancellationToken cancellationToken = default);
    }
}

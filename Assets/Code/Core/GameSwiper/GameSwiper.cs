using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Core.ShortGamesCore.Source.LifeCycleService;
using InGameLogger;
using LightDI.Runtime;

namespace Code.Core.GameSwiper
{
    /// <summary>
    /// Реализация интерфейса ISwiperGame для управления переключением между играми
    /// </summary>
    public class GameSwiper : ISwiperGame
    {
        private readonly IShortGameLifeCycleService _lifeCycleService;
        private readonly IInGameLogger _logger;

        /// <summary>
        /// Конструктор GameSwiper
        /// </summary>
        /// <param name="lifeCycleService">Сервис управления жизненным циклом игр</param>
        /// <param name="logger">Логгер для отслеживания операций</param>
        public GameSwiper(
            [Inject] IShortGameLifeCycleService lifeCycleService,
            [Inject] IInGameLogger logger)
        {
            _lifeCycleService = lifeCycleService;
            _logger = logger;
        }

        /// <summary>
        /// Переключиться на следующую игру
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Следующая игра или null, если переключение невозможно</returns>
        public async ValueTask<IShortGame> NextGameAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log("GameSwiper: Switching to next game");
                var nextGame = await _lifeCycleService.LoadNextGameAsync(cancellationToken);
                
                if (nextGame != null)
                {
                    _logger.Log($"GameSwiper: Successfully switched to next game: {nextGame.GetType().Name}");
                }
                else
                {
                    _logger.LogWarning("GameSwiper: Failed to switch to next game - no game returned");
                }
                
                return nextGame;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"GameSwiper: Error switching to next game: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Переключиться на предыдущую игру
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Предыдущая игра или null, если переключение невозможно</returns>
        public async ValueTask<IShortGame> PreviousGameAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.Log("GameSwiper: Switching to previous game");
                var previousGame = await _lifeCycleService.LoadPreviousGameAsync(cancellationToken);
                
                if (previousGame != null)
                {
                    _logger.Log($"GameSwiper: Successfully switched to previous game: {previousGame.GetType().Name}");
                }
                else
                {
                    _logger.LogWarning("GameSwiper: Failed to switch to previous game - no game returned");
                }
                
                return previousGame;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"GameSwiper: Error switching to previous game: {ex.Message}");
                return null;
            }
        }
    }
}

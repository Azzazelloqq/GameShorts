using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using InGameLogger;
using UnityEngine;

namespace Code.Core.GamesLoader
{
    /// <summary>
    /// Simple bridge provider between user and game management services
    /// </summary>
    public class GameProvider : IGameProvider
    {
        private readonly IInGameLogger _logger;
        private IGameRegistry _gameRegistry;
        private IGameQueueService _queueService;
        private IGamesLoader _gamesLoader;
        private bool _disposed;
        
        public IGameRegistry GameRegistry => _gameRegistry;
        public IGamesLoader GamesLoader => _gamesLoader;
        public IGameQueueService QueueService => _queueService;
        
        public IShortGame CurrentGame => GetGameForType(_queueService?.CurrentGameType);
        public IShortGame NextGame => GetGameForType(_queueService?.NextGameType);
        public IShortGame PreviousGame => GetGameForType(_queueService?.PreviousGameType);
        
        public RenderTexture CurrentGameRenderTexture => CurrentGame?.GetRenderTexture();
        public RenderTexture NextGameRenderTexture => NextGame?.GetRenderTexture();
        public RenderTexture PreviousGameRenderTexture => PreviousGame?.GetRenderTexture();
        
        public bool HasCurrentGame => CurrentGame != null;
        public bool HasNextGame => NextGame != null;
        public bool HasPreviousGame => PreviousGame != null;
        
        public bool IsCurrentGameReady => CurrentGame?.IsPreloaded ?? false;
        public bool IsNextGameReady => NextGame?.IsPreloaded ?? false;
        public bool IsPreviousGameReady => PreviousGame?.IsPreloaded ?? false;
        
        public GameProvider(IInGameLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async ValueTask InitializeAsync(
            IGameRegistry gameRegistry,
            IGameQueueService queueService, 
            IGamesLoader gamesLoader, 
            CancellationToken cancellationToken = default)
        {
            if (gameRegistry == null)
            {
                throw new ArgumentNullException(nameof(gameRegistry));
            }
            
            if (queueService == null)
            {
                throw new ArgumentNullException(nameof(queueService));
            }
            
            if (gamesLoader == null)
            {
                throw new ArgumentNullException(nameof(gamesLoader));
            }
            
            _logger.Log("Initializing GameProvider");
            
            _gameRegistry = gameRegistry;
            _queueService = queueService;
            _gamesLoader = gamesLoader;
            
            // Initialize queue with all registered games from registry
            if (_gameRegistry.Count > 0)
            {
                _logger.Log($"Initializing queue with {_gameRegistry.Count} registered games");
                _queueService.Initialize(_gameRegistry.RegisteredGames);
            }
            
            // Only preload games if we're already positioned in the queue
            // Don't preload in the initial state (index = -1)
            if (_queueService.CurrentIndex >= 0)
            {
                await UpdatePreloadedGamesAsync(cancellationToken);
            }
        }
        
        public void StartCurrentGame()
        {
            if (CurrentGame != null)
            {
                _logger.Log("Starting current game");
                CurrentGame.StartGame();
            }
        }
        
        public void StartNextGame()
        {
            if (NextGame != null)
            {
                _logger.Log("Starting next game");
                NextGame.StartGame();
            }
        }
        
        public void StartPreviousGame()
        {
            if (PreviousGame != null)
            {
                _logger.Log("Starting previous game");
                PreviousGame.StartGame();
            }
        }
        
        public void PauseCurrentGame()
        {
            if (CurrentGame != null)
            {
                _logger.Log("Pausing current game");
                CurrentGame.PauseGame();
            }
        }
        
        public void UnpauseCurrentGame()
        {
            if (CurrentGame != null)
            {
                _logger.Log("Unpausing current game");
                CurrentGame.UnpauseGame();
            }
        }
        
        public void PauseNextGame()
        {
            if (NextGame != null)
            {
                _logger.Log("Pausing next game");
                NextGame.PauseGame();
            }
        }
        
        public void UnpauseNextGame()
        {
            if (NextGame != null)
            {
                _logger.Log("Unpausing next game");
                NextGame.UnpauseGame();
            }
        }
        
        public void PausePreviousGame()
        {
            if (PreviousGame != null)
            {
                _logger.Log("Pausing previous game");
                PreviousGame.PauseGame();
            }
        }
        
        public void UnpausePreviousGame()
        {
            if (PreviousGame != null)
            {
                _logger.Log("Unpausing previous game");
                PreviousGame.UnpauseGame();
            }
        }
        
        public void PauseAllGames()
        {
            _logger.Log("Pausing all games");
            PauseCurrentGame();
            PauseNextGame();
            PausePreviousGame();
        }
        
        public void UnpauseAllGames()
        {
            _logger.Log("Unpausing all games");
            UnpauseCurrentGame();
            UnpauseNextGame();
            UnpausePreviousGame();
        }
        
        public void StopCurrentGame()
        {
            if (CurrentGame != null)
            {
                _logger.Log("Stopping current game");
                CurrentGame.StopGame();
            }
        }
        
        public void StopNextGame()
        {
            if (NextGame != null)
            {
                _logger.Log("Stopping next game");
                NextGame.StopGame();
            }
        }
        
        public void StopPreviousGame()
        {
            if (PreviousGame != null)
            {
                _logger.Log("Stopping previous game");
                PreviousGame.StopGame();
            }
        }
        
        public void StopAllGames()
        {
            _logger.Log("Stopping all games");
            StopCurrentGame();
            StopNextGame();
            StopPreviousGame();
        }
        
        public async ValueTask UpdatePreloadedGamesAsync(CancellationToken cancellationToken = default)
        {
            if (_queueService == null || _gamesLoader == null)
            {
                return;
            }
            
            _logger.Log("Updating preloaded games");
            
            var gamesToPreload = _queueService.GetGamesToPreload();
            await _gamesLoader.PreloadGamesAsync(gamesToPreload, cancellationToken);
        }
        
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            
            _logger.Log("Disposing GameProvider");
            _disposed = true;
            
            StopAllGames();
        }
        
        private IShortGame GetGameForType(Type gameType)
        {
            if (gameType == null || _gamesLoader == null)
            {
                return null;
            }
            
            return _gamesLoader.GetGame(gameType);
        }
    }
}
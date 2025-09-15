using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Source.GameCore
{
    public interface IShortGame : IDisposable
    {
        /// <summary>
        /// Indicates whether the game has been preloaded and is ready to start
        /// </summary>
        public bool IsPreloaded { get; }
        
        /// <summary>
        /// Preloads game resources without starting the game
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the preload operation</returns>
        public ValueTask PreloadGameAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Gets the render texture for this game
        /// </summary>
        /// <returns>Render texture or null if not available</returns>
        public RenderTexture GetRenderTexture();
        
        /// <summary>
        /// Starts the game
        /// </summary>
        public void StartGame();
        
        /// <summary>
        /// Pauses the game
        /// </summary>
        public void PauseGame();
        
        /// <summary>
        /// Unpauses the game
        /// </summary>
        public void UnpauseGame();
        
        /// <summary>
        /// Restarts the game
        /// </summary>
        public void RestartGame();
        
        /// <summary>
        /// Stops the game
        /// </summary>
        public void StopGame();
    }
}
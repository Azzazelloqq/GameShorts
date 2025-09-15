using System;

namespace Code.Core.ShortGamesCore.Source.GameCore
{
    public interface IShortGame : IDisposable
    {
        public int Id { get; }
        
        public void StartGame();
        public void PauseGame();
        public void UnpauseGame();
        public void RestartGame();
        public void StopGame();
    }
}
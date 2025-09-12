
using System;

namespace Code.Core.ShortGamesCore.Source.GameCore
{
    public interface IShortGame : IDisposable
    {
        public int Id { get; }
        public void StartGame();
        public void PauseGame();
        public void ResumeGame();
        public void RestartGame();
        public void StopGame();
    }
}
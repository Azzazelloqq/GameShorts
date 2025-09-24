using System;
using Code.Games.Game2.Scripts.Core;
using R3;

namespace Code.Core.ShortGamesCore.Game2
{
    internal class GameModel
    {
        public ReactiveProperty<GameState> CurrentState { get; } = new ReactiveProperty<GameState>(GameState.Ready);
        public ReactiveProperty<int> Score { get; } = new ReactiveProperty<int>(0);
        public ReactiveProperty<int> BestScore { get; } = new ReactiveProperty<int>(0);
        public ReactiveProperty<bool> IsFirstPlay { get; } = new ReactiveProperty<bool>(true);
        
        public event Action OnGameOver;
        public event Action OnBlockPlaced;
        public event Action OnGameRestarted;

        public void IncrementScore()
        {
            Score.Value++;
            OnBlockPlaced?.Invoke();
        }

        public void TriggerGameOver()
        {
            CurrentState.Value = GameState.GameOver;
            
            // Update best score
            if (Score.Value > BestScore.Value)
            {
                BestScore.Value = Score.Value;
                Save.BestScore = Score.Value;
            }
            
            OnGameOver?.Invoke();
        }

        public void StartNewGame()
        {
            CurrentState.Value = GameState.Running;
            Score.Value = 0;
            IsFirstPlay.Value = false;
        }

        public void RestartGame()
        {
            CurrentState.Value = GameState.Ready;
            Score.Value = 0;
            OnGameRestarted?.Invoke();
        }

        public void Initialize()
        {
            BestScore.Value = Save.BestScore;
            CurrentState.Value = GameState.Ready;
            Score.Value = 0;
        }
    }
}

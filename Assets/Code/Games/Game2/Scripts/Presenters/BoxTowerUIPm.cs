using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Games.Game2.Scripts.Core;
using R3;

namespace Code.Core.ShortGamesCore.Game2
{
    internal class BoxTowerUIPm : BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public BoxTowerSceneContextView sceneContextView;
            public GameModel gameModel;
            public Action restartGame;
        }

        private readonly Ctx _ctx;

        public BoxTowerUIPm(Ctx ctx)
        {
            _ctx = ctx;
            
            // Subscribe to model changes
            AddDispose(_ctx.gameModel.Score.Subscribe(OnScoreChanged));
            AddDispose(_ctx.gameModel.BestScore.Subscribe(OnBestScoreChanged));
            AddDispose(_ctx.gameModel.CurrentState.Subscribe(OnGameStateChanged));
            AddDispose(_ctx.gameModel.IsFirstPlay.Subscribe(OnFirstPlayChanged));
            
            // Setup button listeners
            SetupButtonListeners();
            
            // Initialize UI state
            UpdateUI();
        }

        private void SetupButtonListeners()
        {
            if (_ctx.sceneContextView.RestartButton != null)
                _ctx.sceneContextView.RestartButton.onClick.AddListener(OnRestartClicked);
                
                
            if (_ctx.sceneContextView.TapToPlayButton != null)
                _ctx.sceneContextView.TapToPlayButton.onClick.AddListener(OnTapToPlayClicked);
                
            if (_ctx.sceneContextView.PauseButton != null)
                _ctx.sceneContextView.PauseButton.onClick.AddListener(OnPauseClicked);
        }

        private void OnScoreChanged(int score)
        {
            if (_ctx.sceneContextView.ScoreText != null)
                _ctx.sceneContextView.ScoreText.text = $"Score: {score}";
        }

        private void OnBestScoreChanged(int bestScore)
        {
            if (_ctx.sceneContextView.BestScoreText != null)
                _ctx.sceneContextView.BestScoreText.text = $"Best: {bestScore}";
        }

        private void OnGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Ready:
                    ShowReadyState();
                    break;
                case GameState.Running:
                    ShowRunningState();
                    break;
                case GameState.GameOver:
                    ShowGameOverState();
                    break;
            }
        }

        private void OnFirstPlayChanged(bool isFirstPlay)
        {
            if (isFirstPlay && _ctx.gameModel.CurrentState.Value == GameState.Ready)
            {
                ShowTutorial("Tap to place block");
            }
        }

        private void ShowReadyState()
        {
            HideGameOver();
            ShowTapToPlay();
            HideHUD();
        }

        private void ShowRunningState()
        {
            HideTapToPlay();
            HideTutorial();
            HideGameOver();
            ShowHUD();
        }

        private void ShowGameOverState()
        {
            HideHUD();
            ShowGameOver(_ctx.gameModel.Score.Value, _ctx.gameModel.BestScore.Value);
        }

        private void ShowHUD()
        {
            if (_ctx.sceneContextView.ScoreText != null) 
                _ctx.sceneContextView.ScoreText.gameObject.SetActive(true);
            if (_ctx.sceneContextView.BestScoreText != null) 
                _ctx.sceneContextView.BestScoreText.gameObject.SetActive(true);
            if (_ctx.sceneContextView.PauseButton != null) 
                _ctx.sceneContextView.PauseButton.gameObject.SetActive(true);
        }

        private void HideHUD()
        {
            if (_ctx.sceneContextView.ScoreText != null) 
                _ctx.sceneContextView.ScoreText.gameObject.SetActive(false);
            if (_ctx.sceneContextView.BestScoreText != null) 
                _ctx.sceneContextView.BestScoreText.gameObject.SetActive(false);
            if (_ctx.sceneContextView.PauseButton != null) 
                _ctx.sceneContextView.PauseButton.gameObject.SetActive(false);
        }

        private void ShowGameOver(int finalScore, int bestScore)
        {
            if (_ctx.sceneContextView.GameOverPanel != null)
            {
                _ctx.sceneContextView.GameOverPanel.SetActive(true);
                
                if (_ctx.sceneContextView.FinalScoreText != null)
                    _ctx.sceneContextView.FinalScoreText.text = $"Score: {finalScore}";
                    
                if (_ctx.sceneContextView.FinalBestScoreText != null)
                    _ctx.sceneContextView.FinalBestScoreText.text = $"Best: {bestScore}";
            }
        }

        private void HideGameOver()
        {
            if (_ctx.sceneContextView.GameOverPanel != null)
                _ctx.sceneContextView.GameOverPanel.SetActive(false);
        }

        private void ShowTapToPlay()
        {
            if (_ctx.sceneContextView.TapToPlayPanel != null)
                _ctx.sceneContextView.TapToPlayPanel.SetActive(true);
        }

        private void HideTapToPlay()
        {
            if (_ctx.sceneContextView.TapToPlayPanel != null)
                _ctx.sceneContextView.TapToPlayPanel.SetActive(false);
        }

        private void ShowTutorial(string message)
        {
            if (_ctx.sceneContextView.TutorialPanel != null)
            {
                _ctx.sceneContextView.TutorialPanel.SetActive(true);
                if (_ctx.sceneContextView.TutorialText != null)
                    _ctx.sceneContextView.TutorialText.text = message;
            }
        }

        private void HideTutorial()
        {
            if (_ctx.sceneContextView.TutorialPanel != null)
                _ctx.sceneContextView.TutorialPanel.SetActive(false);
        }

        private void UpdateUI()
        {
            OnScoreChanged(_ctx.gameModel.Score.Value);
            OnBestScoreChanged(_ctx.gameModel.BestScore.Value);
            OnGameStateChanged(_ctx.gameModel.CurrentState.Value);
        }

        // Button event handlers
        private void OnRestartClicked()
        {
            // First restart the game model to trigger tower clearing
            _ctx.gameModel.RestartGame();
            
            // Then restart the entire game system
            _ctx.restartGame?.Invoke();
        }

        private void OnTapToPlayClicked()
        {
            _ctx.gameModel.StartNewGame();
        }

        private void OnPauseClicked()
        {
            // Simple pause implementation
            UnityEngine.Time.timeScale = UnityEngine.Time.timeScale == 0f ? 1f : 0f;
        }

        protected override void OnDispose()
        {
            // Cleanup button listeners
            if (_ctx.sceneContextView.RestartButton != null)
                _ctx.sceneContextView.RestartButton.onClick.RemoveListener(OnRestartClicked);
                
            if (_ctx.sceneContextView.TapToPlayButton != null)
                _ctx.sceneContextView.TapToPlayButton.onClick.RemoveListener(OnTapToPlayClicked);
                
            if (_ctx.sceneContextView.PauseButton != null)
                _ctx.sceneContextView.PauseButton.onClick.RemoveListener(OnPauseClicked);
            
            base.OnDispose();
        }
    }
}

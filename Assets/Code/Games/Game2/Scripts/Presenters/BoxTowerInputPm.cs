using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Games.Game2.Scripts.Core;
using R3;

namespace Code.Core.ShortGamesCore.Game2
{
    internal class BoxTowerInputPm : BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public BoxTowerSceneContextView sceneContextView;
            public GameModel gameModel;
        }

        private readonly Ctx _ctx;
        private BoxTowerTowerPm _towerPresenter; // We need reference to tower presenter for placing blocks

        public BoxTowerInputPm(Ctx ctx)
        {
            _ctx = ctx;
            
            // Setup tap input
            if (_ctx.sceneContextView.FullScreenTapButton != null)
            {
                _ctx.sceneContextView.FullScreenTapButton.onClick.AddListener(OnScreenTap);
            }
        }

        // This method should be called by the main scene presenter to inject tower presenter reference
        public void SetTowerPresenter(BoxTowerTowerPm towerPresenter)
        {
            _towerPresenter = towerPresenter;
        }

        private void OnScreenTap()
        {
            switch (_ctx.gameModel.CurrentState.Value)
            {
                case GameState.Ready:
                    _ctx.gameModel.StartNewGame();
                    break;
                    
                case GameState.Running:
                    PlaceCurrentBlock();
                    break;
                    
                case GameState.GameOver:
                    // Do nothing, let UI buttons handle restart
                    break;
            }
        }

        private void PlaceCurrentBlock()
        {
            if (_ctx.gameModel.CurrentState.Value != GameState.Running || _towerPresenter == null) 
                return;

            bool success = _towerPresenter.TryPlaceCurrentBlock();
            
            if (success)
            {
                _ctx.gameModel.IncrementScore();
                
                // Add haptic feedback for successful placement
                // Handheld.Vibrate(); // Uncomment if you want haptic feedback
            }
            else
            {
                // Game Over
                _ctx.gameModel.TriggerGameOver();
                
                // Add stronger haptic feedback for game over
                // Handheld.Vibrate(); // Uncomment if you want haptic feedback
            }
        }

        protected override void OnDispose()
        {
            // Cleanup button listeners
            if (_ctx.sceneContextView.FullScreenTapButton != null)
            {
                _ctx.sceneContextView.FullScreenTapButton.onClick.RemoveListener(OnScreenTap);
            }
            
            base.OnDispose();
        }
    }
}

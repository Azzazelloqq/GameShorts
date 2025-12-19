using System.Threading;
using Disposable;
using R3;
using UnityEngine;
using CompositeDisposable = R3.CompositeDisposable;

namespace Code.Games
{
    internal class Game2048MainUIPm : DisposableBase
    {
        internal struct Ctx
        {
            public Game2048MainUIView mainUIView;
            public ReadOnlyReactiveProperty<int> currentScore;
            public CancellationToken cancellationToken;
        }

        private const string BestScoreKey = "Game2048_BestScore";
        
        private readonly Ctx _ctx;
        private readonly CompositeDisposable _compositeDisposable = new();
        private int _bestScore;

        public int BestScore => _bestScore;

        public Game2048MainUIPm(Ctx ctx)
        {
            _ctx = ctx;
            
            LoadBestScore();
            InitializeView();
            SubscribeToScore();
            
            AddDisposable(_compositeDisposable);
        }

        private void LoadBestScore()
        {
            _bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            Debug.Log($"Game2048MainUIPm: Loaded best score = {_bestScore}");
        }

        private void SaveBestScore()
        {
            PlayerPrefs.SetInt(BestScoreKey, _bestScore);
            PlayerPrefs.Save();
            Debug.Log($"Game2048MainUIPm: Saved best score = {_bestScore}");
        }

        private void InitializeView()
        {
            if (_ctx.mainUIView == null)
            {
                Debug.LogError("Game2048MainUIPm: mainUIView is null!");
                return;
            }

            var viewCtx = new Game2048MainUIView.Ctx
            {
            };

            _ctx.mainUIView.SetCtx(viewCtx);
            _ctx.mainUIView.UpdateBestScore(_bestScore);
            _ctx.mainUIView.UpdateCurrentScore(_ctx.currentScore.CurrentValue);
        }

        private void SubscribeToScore()
        {
            _ctx.currentScore
                .Subscribe(score =>
                {
                    if (_ctx.mainUIView != null)
                    {
                        _ctx.mainUIView.UpdateCurrentScore(score);
                    }

                    if (score > _bestScore)
                    {
                        _bestScore = score;
                        SaveBestScore();
                        
                        if (_ctx.mainUIView != null)
                        {
                            _ctx.mainUIView.UpdateBestScore(_bestScore);
                        }
                    }
                })
                .AddTo(_compositeDisposable);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
        }
    }
}


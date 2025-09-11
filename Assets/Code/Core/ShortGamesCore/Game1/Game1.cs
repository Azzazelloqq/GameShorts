using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using Code.Core.ShortGamesCore.Game1.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.ShortGamesCore.Source.GameCore;
using R3;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game1
{
    public class Game1 : BaseMonoBehaviour, IShortGame
    {
        [SerializeField] private MainSceneContextView sceneContextView;
        private IDisposable _core;
        private CancellationTokenSource _cancellationTokenSource;
        public int Id => 1;

        public void Start()
        {
            Restart();
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Restart()
        {
            Dispose();
            CreateRoot();
        }

        public void Stop()
        {
            Dispose();
        }

        private void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _core?.Dispose();
        }

        private void CreateRoot()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CorePm.Ctx rootCtx = new CorePm.Ctx
            {
                sceneContextView = sceneContextView,
                cancellationToken = _cancellationTokenSource.Token,
                restartGame = Restart
            };
            _core = new CorePm(rootCtx);
        }
    }
}
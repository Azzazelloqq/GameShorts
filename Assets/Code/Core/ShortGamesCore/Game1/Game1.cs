using System;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts;
using Code.Core.ShortGamesCore.Source.GameCore;
using R3;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game1
{
    public class Game1 : BaseMonoBehaviour, IShortGame
    {
        [SerializeField] private Transform _circle;
        private IDisposable _root;
        public int Id => 1;

        public void Start()
        {
            CreateRoot();
        }

        public void Pause()
        {
        }

        public void Resume()
        {
        }

        public void Restart()
        {
            CreateRoot();
        }

        private void CreateRoot()
        {
            _root?.Dispose();
            Root.Ctx rootCtx = new Root.Ctx
            {
            };
            _root = RootFactory.CreateRoot(rootCtx);
        }
    }
}
using System;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Source.GameCore;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game2
{
    public class Game2: BaseMonoBehaviour, IShortGame
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

        public void Stop()
        {
            _root?.Dispose();
        }

        private void CreateRoot()
        {
        }
    }
}
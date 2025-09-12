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

        public void Dispose()
        {
            _root?.Dispose();
            _root = null;
        }
        
        public void StartGame()
        {
            CreateRoot();
        }

        public void PauseGame()
        {
        }

        public void ResumeGame()
        {
        }

        public void RestartGame()
        {
            CreateRoot();
        }

        public void StopGame()
        {
            _root?.Dispose();
        }

        private void CreateRoot()
        {
        }
    }
}
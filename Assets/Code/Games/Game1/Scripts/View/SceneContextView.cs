
using System;
using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game1.Scripts.View
{
    internal class SceneContextView : BaseMonoBehaviour
    {
		
        [SerializeField] private Camera _camera;
        public Camera Camera => _camera;
		
        [SerializeField]
        private Canvas mainCanvas;
        [SerializeField]
        private Transform uiParent;

        public Transform UiParent
            => uiParent;
        public Canvas MainCanvas
            => mainCanvas;
    }
}
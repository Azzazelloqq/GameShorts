
using System;
using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game1.Scripts.View
{
    public class SceneContextView : BaseMonoBehaviour
    {
		
        [SerializeField] private Camera _camera;
        public Camera Camera => _camera;
		
        public event Action<float> OnUpdated;
        public event Action<float>  OnFixedUpdated;
        [SerializeField]
        private Canvas mainCanvas;
        [SerializeField]
        private Transform uiParent;

        public Transform UiParent
            => uiParent;
        public Canvas MainCanvas
            => mainCanvas;
		
		
        private void Update()
        {
            OnUpdated?.Invoke(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            OnFixedUpdated?.Invoke(Time.fixedDeltaTime);
        }
    }
}
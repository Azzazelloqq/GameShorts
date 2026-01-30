
using System;
using Code.Games.FruitSlasher.Scripts.Input;
using Code.Games.FruitSlasher.Scripts.View;
using Disposable;
using R3;
using UnityEngine;

namespace Code.Games.FruitSlasher.Scripts.Logic
{
    internal class BladePm : DisposableBase
    {
        internal struct Ctx
        {
            public FruitSlasherSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        public Vector3 Direction {get; private set;}
        public Vector3 Position => _ctx.sceneContextView.BladeView.transform.position;
        
        private Ctx _ctx;
        private readonly InputAreaPm _inputArea;
        private BladeView _view;
        
        private const float _minSliceVelocity = 0.02f;
        

        public BladePm(Ctx ctx)
        {
            _ctx = ctx;
            
            _inputArea = new InputAreaPm(_ctx.sceneContextView.InputAreaView);
            AddDisposable(_inputArea);

            _view = _ctx.sceneContextView.BladeView;
            StopSlice(Vector2.zero);

            _inputArea.OnPointerDown.Subscribe(StartSlice);
            _inputArea.OnPointerUp.Subscribe(StopSlice);
            _inputArea.OnPointerMove.Subscribe(SwipeSlice);
            AddDisposable(_ctx.isPaused.Subscribe(value => _inputArea.SetInputEnabled(!value)));

        }

        private void StartSlice(Vector2 screenPos)
        {
            var newPosition = GetWorldPos(screenPos);
            newPosition.z = _view.transform.position.z;
            _view.transform.position = newPosition;
            _view.Collider.enabled = true;
            _view.TrailRenderer.Clear();
            _view.TrailRenderer.enabled = true;
        }
        
        private void StopSlice(Vector2 screenPos)
        {
            _view.Collider.enabled = false;
            _view.TrailRenderer.enabled = false;
        }
        
        private void SwipeSlice(Vector2 screenPos)
        {
            var newPosition = GetWorldPos(screenPos);
            newPosition.z = _view.transform.position.z;
            Direction = newPosition - _view.transform.position;
            var velocity = Direction.magnitude /  Time.deltaTime;
            _view.Collider.enabled = velocity > _minSliceVelocity;
            
            _view.transform.position = newPosition;
        }

        private Vector3 GetWorldPos(Vector2 screenPos)
        {
            var cam = _ctx.sceneContextView.MainCamera;
            var depth = Mathf.Abs(_view.transform.position.z - cam.transform.position.z);
            var newPosition = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
            return newPosition;
        }
    }
}


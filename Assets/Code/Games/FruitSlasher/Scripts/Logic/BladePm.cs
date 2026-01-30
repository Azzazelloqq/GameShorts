
using System;
using Code.Games.FruitSlasher.Scripts.Input;
using Code.Games.FruitSlasher.Scripts.View;
using Disposable;
using LightDI.Runtime;
using R3;
using TickHandler;
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
        public object CurrentVelocity => _currentVelocity;

        private Ctx _ctx;
        private readonly InputAreaPm _inputArea;
        private BladeView _view;
        private float _currentVelocity;
        private readonly ITickHandler _tickHandler;
        private float _stationaryTime;
        private bool _isPressed;

        private const float _minSliceVelocity = 2f;
        private const float STATIONARY_DELAY = 0.05f; // сек
        

        public BladePm(Ctx ctx, [Inject] ITickHandler  tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            
            _inputArea = new InputAreaPm(_ctx.sceneContextView.InputAreaView);
            AddDisposable(_inputArea);

            _view = _ctx.sceneContextView.BladeView;
            StopSlice(Vector2.zero);

            _inputArea.OnPointerDown.Subscribe(StartSlice);
            _inputArea.OnPointerUp.Subscribe(StopSlice);
            _inputArea.OnPointerMove.Subscribe(SwipeSlice);
            AddDisposable(_ctx.isPaused.Subscribe(value =>
            {
                _inputArea.SetInputEnabled(!value);
                if (value)
                    _tickHandler.FrameUpdate -= CheckStationary;
                else
                    _tickHandler.FrameUpdate += CheckStationary;
            }));
        }

        protected override void OnDispose()
        {
            _tickHandler.FrameUpdate -= CheckStationary;
        }

        private void CheckStationary(float deltaTime)
        {
            if (!_isPressed)
                return;
            _stationaryTime += Time.deltaTime;
            if (_stationaryTime >= STATIONARY_DELAY)
            {
                _view.Collider.enabled = false;
            }
        }

        private void StartSlice(Vector2 screenPos)
        {
            var newPosition = GetWorldPos(screenPos);
            newPosition.z = _view.transform.position.z;
            _view.transform.position = newPosition;
            _view.Collider.enabled = false;
            _view.TrailRenderer.Clear();
            _view.TrailRenderer.enabled = true;
            _currentVelocity = 0;
            _isPressed = true;
        }
        
        private void StopSlice(Vector2 screenPos)
        {
            _isPressed = false;
            _view.Collider.enabled = false;
            _view.TrailRenderer.enabled = false;
        }
        
        private void SwipeSlice(Vector2 screenPos)
        {
            
            var newPosition = GetWorldPos(screenPos);
            newPosition.z = _view.transform.position.z;
            Direction = newPosition - _view.transform.position;
             _currentVelocity = Direction.magnitude /  Time.deltaTime;
             var isEnableSlice =_currentVelocity > _minSliceVelocity;
            _view.Collider.enabled = isEnableSlice;
            _view.transform.position = newPosition;
            
            if (isEnableSlice)
            {
                _stationaryTime = 0f; // движемся
            }

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


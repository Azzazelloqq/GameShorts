using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Games._2048.Scripts.View;
using Code.Games._2048.Scripts.Input;
using UnityEngine;
using R3;

namespace Code.Games._2048.Scripts.Gameplay
{
    internal class Game2048CubeControllerPm : BaseDisposable
    {
        internal struct Ctx
        {
            public Game2048InputPm inputPm;
            public float launchForce;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private readonly CompositeDisposable _compositeDisposable = new();
        
        private Vector2 _startPointerPosition;
        private Vector2 _lastPointerPosition;
        private bool _isDragging;
        private Game2048CubeView _currentCube;
        
        public readonly Subject<Unit> OnCubeLaunched = new();

        public Game2048CubeControllerPm(Ctx ctx)
        {
            _ctx = ctx;
            
            SubscribeToInput();
            
            AddDispose(_compositeDisposable);
            AddDispose(OnCubeLaunched);
        }

        public void SetCurrentCube(Game2048CubeView cubeView)
        {
            _currentCube = cubeView;
        }

        public void ReleaseCurrentCube()
        {
            _currentCube = null;
            _isDragging = false;
        }

        private void SubscribeToInput()
        {
            _ctx.inputPm.OnPointerDown
                .Subscribe(OnPointerDown)
                .AddTo(_compositeDisposable);
                
            _ctx.inputPm.OnPointerMove
                .Subscribe(OnPointerMove)
                .AddTo(_compositeDisposable);
                
            _ctx.inputPm.OnPointerUp
                .Subscribe(OnPointerUp)
                .AddTo(_compositeDisposable);
        }

        private void OnPointerDown(Vector2 position)
        {
            if (_currentCube == null) return;

            _startPointerPosition = position;
            _lastPointerPosition = position;
            _isDragging = true;
            _currentCube.StartControl();
        }

        private void OnPointerMove(Vector2 position)
        {
            if (!_isDragging || _currentCube == null) return;

            // Определяем направление движения по сравнению с последней позицией
            float deltaX = position.x - _lastPointerPosition.x;
            
            // Минимальный порог для избежания дрожания
            const float threshold = 2f;
            
            if (deltaX > threshold)
            {
                _currentCube.MoveRight();
            }
            else if (deltaX < -threshold)
            {
                _currentCube.MoveLeft();
            }
            
            _lastPointerPosition = position;
        }

        private void OnPointerUp(Vector2 position)
        {
            if (!_isDragging || _currentCube == null) return;

            _isDragging = false;
            _currentCube.LaunchForward(_ctx.launchForce);
            
            // Убираем контроль над кубом
            ReleaseCurrentCube();
            
            OnCubeLaunched.OnNext(Unit.Default);
        }
    }
}

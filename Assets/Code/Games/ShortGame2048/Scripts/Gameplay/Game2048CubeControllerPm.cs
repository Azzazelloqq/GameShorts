using System;
using System.Threading;
using Disposable;
using LightDI.Runtime;
using UnityEngine;
using R3;
using TickHandler;
using CompositeDisposable = R3.CompositeDisposable;

namespace Code.Games
{
    internal class Game2048CubeControllerPm : DisposableBase
    {
        public struct Ctx
        {
            public Game2048InputPm inputPm;
            public float launchForce;
            public float minX;
            public float maxX;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private readonly CompositeDisposable _compositeDisposable = new();
        
        private Vector2 _startPointerPosition;
        private bool _isDragging;
        private CubePm _currentCube;
        private float _targetX;
        private float _cubeStartX;
        
        public readonly Subject<Unit> OnCubeLaunched = new();
        private readonly ITickHandler _tickHandler;

        public CubePm CurrentCube => _currentCube;

        public Game2048CubeControllerPm(Ctx ctx, [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            
            SubscribeToInput();
            
            AddDisposable(_compositeDisposable);
            AddDisposable(OnCubeLaunched);
        }

        protected override void OnDispose()
        {
            _tickHandler.PhysicUpdate -= ControlCube;
            base.OnDispose();
        }

        public void SetCurrentCube(CubePm cube)
        {
            _currentCube = cube;
            
            // Останавливаем физику куба, чтобы он не падал до начала контроля игроком
            if (_currentCube?.View != null)
            {
                _currentCube.View.PrepareForControl();
            }
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

        private void OnPointerDown(Vector2 screenPosition)
        {
            if (_currentCube == null) return;

            _startPointerPosition = screenPosition;
            _cubeStartX = _currentCube.View.GetPositionX();
            _isDragging = true;
            _currentCube.View.StartControl();
            _tickHandler.PhysicUpdate += ControlCube;
        }

        private void ControlCube(float time)
        {
            _currentCube.View.UpdateController(time);
        }

        private void OnPointerMove(Vector2 screenPosition)
        {
            if (!_isDragging || _currentCube == null) return;

            // Вычисляем смещение в экранных координатах
            float deltaScreenX = screenPosition.x - _startPointerPosition.x;
            
            // Преобразуем экранное смещение в мировое с учетом ширины экрана
            // Нормализуем смещение относительно ширины экрана и масштабируем к диапазону движения
            float screenWidth = Screen.width;
            float worldRange = _ctx.maxX - _ctx.minX;
            float deltaWorldX = (deltaScreenX / screenWidth) * worldRange * 2f; // *2 для более чувствительного управления
            
            // Вычисляем целевую позицию куба
            _targetX = _cubeStartX + deltaWorldX;
            
            // Ограничиваем целевую позицию
            _targetX = Mathf.Clamp(_targetX, _ctx.minX+_cubeStartX, _ctx.maxX+_cubeStartX);
            
            // Передаем целевую позицию в View для плавного движения
            _currentCube.View.SetTargetX(_targetX);
        }

        private void OnPointerUp(Vector2 screenPosition)
        {
            if (!_isDragging || _currentCube == null) return;

            // Убираем контроль над кубом
            _tickHandler.PhysicUpdate -= ControlCube;
            _currentCube.View.LaunchForward(_ctx.launchForce);
            ReleaseCurrentCube();
            OnCubeLaunched.OnNext(Unit.Default);
        }
    }
}

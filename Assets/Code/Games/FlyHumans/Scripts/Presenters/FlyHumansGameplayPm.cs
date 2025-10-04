using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.FlyHumans.Presenters;
using GameShorts.FlyHumans.View;
using R3;
using R3.Triggers;
using UnityEngine;

namespace GameShorts.FlyHumans.Gameplay
{
    /// <summary>
    /// Главный презентер геймплея - координирует работу персонажа с камерой и миром
    /// </summary>
    internal class FlyHumansGameplayPm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public FlyHumansSceneContextView sceneContextView;
            public ReactiveProperty<bool> isPaused;
            public CameraPm cameraPm;
            public WorldBlocksPm worldBlocksPm;
        }

        private readonly Ctx _ctx;
        private CharacterView _character;
        private IDisposable _updateSubscription;
        private IDisposable _jumpTrigger;
        private IDisposable _inputSubscription;
        private bool _isGameStarted = false;

        public FlyHumansGameplayPm(Ctx ctx)
        {
            _ctx = ctx;
            Initialize();
        }

        private void Initialize()
        {
            // Получаем персонажа со сцены
            _character = _ctx.sceneContextView.Character;
            
            if (_character != null)
            {
                _character.Initialize();
                _character.RagdollRoot.CollisionEnter = OnRagdollCollision;
                
                // Подписываемся на анимацию старта
                foreach (var obs in _character.Animator.GetBehaviours<ObservableStateMachineTrigger>())
                {
                    _jumpTrigger = obs.OnStateEnterAsObservable()
                        .Subscribe(_ =>
                        {
                            InitGravity();
                            _jumpTrigger?.Dispose();
                        });
                }
                
                // Подписываемся на Update
                _updateSubscription = Observable.EveryUpdate()
                    .Where(_ => !_ctx.isPaused.Value)
                    .Subscribe(_ => OnUpdate());
                
                // Подписываемся на input
                if (_ctx.sceneContextView.MainUIView != null && _ctx.sceneContextView.MainUIView.JumpButton != null)
                {
                    _inputSubscription = _ctx.sceneContextView.MainUIView.JumpButton.OnClickAsObservable()
                        .Where(_ => _character.IsActive && !_ctx.isPaused.Value)
                        .Subscribe(_ => Jump());
                }
                
                AddDispose(_updateSubscription);
                AddDispose(_inputSubscription);
                AddDispose(_jumpTrigger);
            }
        }

        private void OnUpdate()
        {
            if (_character == null) return;
            
            if (!_character.IsActive) return;
            
            // Применяем гравитацию
            _character.VerticalVelocity -= _character.CurrentGravity * Time.deltaTime;
            
            // Обновляем позицию персонажа (только вверх/вниз)
            _character.UpdatePosition(Time.deltaTime);
            
            // Обновляем мир (движение блоков) через презентер
            _ctx.worldBlocksPm?.UpdateWorld(Time.deltaTime);
            
            // Обновляем анимацию персонажа
            _character.UpdateAnimation();
            
            // Обновляем камеру через презентер
            _ctx.cameraPm?.UpdateCameraPosition();
        }

        public void StartGame()
        {
            if (_isGameStarted || _character == null) return;
            
            _isGameStarted = true;
            _character.StartJumpAnimation();
        }

        private void InitGravity()
        {
            _character.CurrentGravity = _character.Gravity;
            _character.CurrentSpeed = _character.ForwardSpeed;
            _character.IsActive = true;
            
            // Запускаем движение мира со скоростью персонажа через презентер
            if (_ctx.worldBlocksPm != null)
            {
                _ctx.worldBlocksPm.SetSpeed(_character.ForwardSpeed);
                _ctx.worldBlocksPm.IsMoving = true;
            }
            
            // Анимируем камеру через презентер
            _ctx.cameraPm?.AnimateCamera();
        }

        private void Jump()
        {
            _character.VerticalVelocity = _character.JumpForce;
        }

        private void OnRagdollCollision()
        {
            Jump();
            if (_character != null)
            {
               // Останавливаем анимацию камеры через презентер
               _ctx.cameraPm?.StopCameraAnimation();
               _character.StopCharacter();
            }
            
            // Останавливаем движение мира через презентер
            if (_ctx.worldBlocksPm != null)
            {
                _ctx.worldBlocksPm.IsMoving = false;
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
        }
    }
}

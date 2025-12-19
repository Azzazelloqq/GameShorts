using System;
using System.Threading;
using Disposable;
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
    internal class FlyHumansGameplayPm : DisposableBase
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
        private IDisposable _resetDelaySubscription;
        private bool _collided;

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
                SubscribeToStartAnimation();
                
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
                
                AddDisposable(_updateSubscription);
                AddDisposable(_inputSubscription);
            }
        }
        
        private void SubscribeToStartAnimation()
        {
            // Отписываемся от предыдущей подписки, если она есть
            _jumpTrigger?.Dispose();
            
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
        }

        private void OnUpdate()
        {
            if (_character == null) return;
            
            _ctx.worldBlocksPm?.UpdateWorld(Time.deltaTime);
            
            // Обновляем персонажа только если он активен
            if (_character.IsActive)
            {
                // Применяем гравитацию
                _character.VerticalVelocity -= _character.CurrentGravity * Time.deltaTime;
                
                // Обновляем позицию персонажа (только вверх/вниз)
                _character.UpdatePosition(Time.deltaTime);
                
                // Обновляем анимацию персонажа
                _character.UpdateAnimation();
                
                // Обновляем камеру через презентер только если персонаж активен
                _ctx.cameraPm?.UpdateCameraPosition();
            }
        }

        public void StartGame()
        {
            _collided = false;
            if (_character == null)
            {
                Debug.LogError("Character is null!");
                return;
            }
            
            
            // Запускаем анимацию старта персонажа
            _character.StartJumpAnimation();
        }

        private void InitGravity()
        {
            _character.CurrentGravity = _character.Gravity;
            _character.IsActive = true;
            
            // Мир уже двигается с момента старта игры
            // Здесь только активируем персонажа и камеру
            
            // Анимируем камеру через презентер
            _ctx.cameraPm?.AnimateCamera();
            
            // Запускаем движение мира с постепенным ускорением
            if (_ctx.worldBlocksPm != null)
            {
                _ctx.worldBlocksPm.IsMoving = true;
            }
            else
            {
                Debug.LogError("WorldBlocksPm is null!");
            }
        }

        private void Jump()
        {
            _character.VerticalVelocity = _character.JumpForce;
        }

        private void OnRagdollCollision()
        {
            if (_collided)
                return;
            _collided = true;
            
            Jump();
            if (_character != null)
            {
               // Останавливаем анимацию камеры через презентер
               _ctx.cameraPm?.StopCameraAnimation();
               _character.StopCharacter();
            }
            
            // Останавливаем только движение блоков (спавн новых и удаление старых)
            // Трафик продолжает работать на существующих блоках
            if (_ctx.worldBlocksPm != null)
            {
                _ctx.worldBlocksPm.IsMoving = false;
                Debug.Log("World movement stopped, but traffic continues on existing blocks");
            }
            
            // Запускаем автоматический ресет через 2 секунды
            _resetDelaySubscription?.Dispose();
            _resetDelaySubscription = Observable.Timer(System.TimeSpan.FromSeconds(2))
                .Subscribe(_ => ResetGame());
        }
        
        private void ResetGame()
        {
            if (_character == null) return;
            
            Debug.Log("Resetting game...");
            
            // Сбрасываем мир (удаляем все блоки кроме стартового)
            _ctx.worldBlocksPm?.ResetWorld();
            
            // Возвращаем персонажа на стартовую позицию с анимацией idle
            _character.ResetToInitialPosition();
            
            // Сбрасываем камеру в начальное положение
            _ctx.cameraPm?.ResetCamera();
            
            // Переподписываемся на анимацию старта
            SubscribeToStartAnimation();
            
            // Запускаем автоматический старт через 2 секунды
            _resetDelaySubscription?.Dispose();
            _resetDelaySubscription = Observable.Timer(System.TimeSpan.FromSeconds(3))
                .Subscribe(_ => StartGame());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _resetDelaySubscription?.Dispose();
            _jumpTrigger?.Dispose();
        }
    }
}

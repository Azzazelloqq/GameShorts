using System;
using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.CubeRunner.Data;
using R3;
using UnityEngine;

namespace GameShorts.CubeRunner.Level
{
    internal class WorldMover : BaseDisposable
    {
        internal struct Ctx
        {
            public Transform worldRoot;
            public CubeRunnerGameSettings gameSettings;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private readonly IDisposable _updateSubscription;

        private float _speedMultiplier = 1f;
        private bool _isActive = true;
        private bool _movementEnabled;

        public float TotalDistance { get; private set; }

        public WorldMover(Ctx ctx)
        {
            _ctx = ctx;
            _updateSubscription = Observable.EveryUpdate()
                .Subscribe(_ => OnUpdate(Time.deltaTime));
            AddDispose(_updateSubscription);

            if (_ctx.isPaused != null)
            {
                AddDispose(_ctx.isPaused.Subscribe(isPaused =>
                {
                    _isActive = !isPaused;
                }));
            }
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0f, multiplier);
        }

        public void ResetDistance()
        {
            TotalDistance = 0f;
        }

        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;
        }

        public void EnableMovement()
        {
            _movementEnabled = true;
        }

        public void DisableMovement()
        {
            _movementEnabled = false;
        }

        private void OnUpdate(float deltaTime)
        {
            if (!_movementEnabled || !_isActive || _ctx.worldRoot == null || _ctx.gameSettings == null)
            {
                return;
            }

            float speed = _ctx.gameSettings.WorldSpeed * _speedMultiplier;
            float delta = speed * deltaTime;
            if (Mathf.Approximately(delta, 0f))
            {
                return;
            }

            Vector3 movement = Vector3.back * delta;
            _ctx.worldRoot.position += movement;
            TotalDistance += delta;
        }

        protected override void OnDispose()
        {
            // subscriptions disposed automatically
        }
    }
}


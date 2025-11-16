using System;
using Code.Core.BaseDMDisposable.Scripts;
using R3;
using UnityEngine;

namespace GameShorts.CubeRunner.Gameplay
{
    internal class CubeRunnerGameState : BaseDisposable
    {
        internal struct Ctx
        {
            public CubeController cubeController;
            public ReactiveProperty<bool> isPaused;
            public Func<Vector2Int, bool> hasTileAtGridPosition;
        }

        private readonly Ctx _ctx;
        private readonly ReactiveProperty<bool> _isAlive = new ReactiveProperty<bool>(true);
        private readonly Subject<Unit> _gameOverStream = new Subject<Unit>();
        private readonly IDisposable _updateSubscription;

        private bool _isPaused;

        public ReactiveProperty<bool> IsAlive => _isAlive;
        public Subject<Unit> GameOver => _gameOverStream;

        public CubeRunnerGameState(Ctx ctx)
        {
            _ctx = ctx;
            _updateSubscription = Observable.EveryUpdate()
                .Subscribe(_ => OnUpdate());
            AddDispose(_updateSubscription);

            if (_ctx.cubeController != null)
            {
                AddDispose(_ctx.cubeController.Movements.Subscribe(OnCubeMoved));
            }

            if (_ctx.isPaused != null)
            {
                AddDispose(_ctx.isPaused.Subscribe(isPaused => _isPaused = isPaused));
            }
        }

        private void OnCubeMoved(CubeMovementEvent movementEvent)
        {
            if (!_isAlive.Value)
            {
                return;
            }

            if (movementEvent.IsFalling)
            {
                TriggerGameOver();
            }
        }

        private void OnUpdate()
        {
            if (_isPaused || !_isAlive.Value || _ctx.cubeController == null)
            {
                return;
            }

            if (_ctx.hasTileAtGridPosition != null)
            {
                Vector2Int gridPosition = _ctx.cubeController.CurrentGridPosition;
                if (!_ctx.hasTileAtGridPosition.Invoke(gridPosition))
                {
                    TriggerGameOver();
                }
            }
        }

        private void TriggerGameOver()
        {
            if (!_isAlive.Value)
            {
                return;
            }

            _isAlive.Value = false;
            _gameOverStream.OnNext(Unit.Default);
        }

        protected override void OnDispose()
        {
            _gameOverStream.OnCompleted();
            _gameOverStream.Dispose();
            _isAlive.Dispose();
        }
    }
}


using System;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using Code.Games._2048.Scripts.View;
using LightDI.Runtime;
using UnityEngine;

namespace Code.Games._2048.Scripts.Gameplay
{
    internal class CubePm : BaseDisposable
    {
        public struct Ctx
        {
            public CubeModel model;
            public Vector3 spawnPoint;
            public GameObject cubePrefab;
            public Action<Guid, Guid> onCubeCollision; // (myId, otherId)
        }

        private readonly Ctx _ctx;
        private readonly IPoolManager _poolManager;
        private Game2048CubeView _view;
        public Guid Id => _ctx.model.id;
        public int Number => _ctx.model.currentNumber;
        public Game2048CubeView View => _view;

        public CubePm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            LoadView();
        }

        private void LoadView()
        {
            GameObject cubeObject = _poolManager.Get(_ctx.cubePrefab, _ctx.spawnPoint);
            _view = cubeObject.GetComponent<Game2048CubeView>();
            
            // Сбрасываем скорость куба сразу после получения из пула
            _view.ResetVelocity();
            
            _view.SetCtx(new Game2048CubeView.Ctx()
            {
                moveSpeed = 4,
                cubeId = _ctx.model.id,
                onCollisionWithCube = OnCubeCollision,
                number = _ctx.model.currentNumber,
            });
        }

        private void OnCubeCollision( Guid otherId)
        {
            // Передаем событие коллизии дальше в менеджер мержа
            _ctx.onCubeCollision?.Invoke(_ctx.model.id, otherId);
        }

        protected override void OnDispose()
        {
            _poolManager.Return(_ctx.cubePrefab, _view.gameObject);
            base.OnDispose();
        }
    }

    internal class CubeModel
    {
        public Guid id;
        public int currentNumber;
    }
}
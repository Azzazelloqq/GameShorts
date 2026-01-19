using System;
using Disposable;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using UnityEngine;

namespace Code.Games
{
    internal class CubePm : DisposableBase
    {
        internal struct Ctx
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
            
            if (cubeObject == null)
            {
                Debug.LogError("CubePm.LoadView: cubeObject is null!");
                return;
            }
            
            _view = cubeObject.GetComponent<Game2048CubeView>();
            
            if (_view == null)
            {
                Debug.LogError("CubePm.LoadView: Game2048CubeView component not found!");
                return;
            }
            
            // Убеждаемся что GameObject активен
            if (!cubeObject.activeSelf)
            {
                Debug.LogWarning("CubePm.LoadView: GameObject is not active, activating...");
                cubeObject.SetActive(true);
            }
            
            // Сбрасываем скорость куба сразу после получения из пула
            _view.ResetVelocity();
            
            _view.SetCtx(new Game2048CubeView.Ctx()
            {
                cubeId = _ctx.model.id,
                onCollisionWithCube = OnCubeCollision,
                number = _ctx.model.currentNumber,
            });
            
            Debug.Log($"CubePm.LoadView: Successfully loaded cube {_ctx.model.id} with number {_ctx.model.currentNumber} at position {cubeObject.transform.position}");
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
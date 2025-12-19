using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Disposable;
using UnityEngine;
using R3;
using CompositeDisposable = R3.CompositeDisposable;

namespace Code.Games
{
    internal class Game2048CubeSpawnerPm : DisposableBase
    {
        internal struct Ctx
        {
            public Transform spawnPoint;
            public GameObject cubePrefab;
            public CancellationToken cancellationToken;
            public Action<Guid, Guid> onCubeCollision; // (cubeId1, cubeId2)
            public Action<Guid, CubePm> onCubeCreated;
            public Action<Guid, CubePm> onMegredCubeCreated;
        }

        private readonly Ctx _ctx;
        private readonly CompositeDisposable _compositeDisposable = new();
        
        private SortedSet<int> _usedNumbers = new();
        private System.Action<Guid, Guid> _onCubeCollision;
        private WeightedPickerUnity.Table _weightTable;

        public SortedSet<int> UsedNumbers => _usedNumbers;

        public Game2048CubeSpawnerPm(Ctx ctx)
        {
            _ctx = ctx;
            _onCubeCollision = ctx.onCubeCollision;
            AddDisposable(_compositeDisposable);
        }

        public CubePm CreateCube()
        {
            var number = _usedNumbers.Count > 0 
                ? WeightedPickerUnity.Pick(ref _weightTable) : 2;
            return CreateCubeAtPosition(_ctx.spawnPoint.position, number, false);
        }

        public CubePm CreateCubeAtPosition(Vector3 position, int number, bool fromMerge = true)
        {
            if (_ctx.cubePrefab == null)
            {
                Debug.LogError("Game2048CubeSpawnerPm: CubePrefab is null!");
                return null;
            }

            var cubeModel = new CubeModel()
            {
                currentNumber = number,
                id = Guid.NewGuid()
            };

            var cube = CubePmFactory.CreateCubePm(new CubePm.Ctx()
            {
                model = cubeModel,
                cubePrefab = _ctx.cubePrefab,
                spawnPoint = position,
                onCubeCollision = _onCubeCollision
            });
            if (number <= 64)
                if (_usedNumbers.Add(number))
                    _weightTable = WeightedPickerUnity.BuildTable(_usedNumbers);
            
            // Устанавливаем видимость Line (если он есть)
            if (cube.View != null && cube.View.Line != null)
            {
                cube.View.Line.gameObject.SetActive(!fromMerge);
            }
            
            Debug.Log($"Game2048CubeSpawnerPm: Created cube {cube.Id} with number {number} at position {position}, fromMerge={fromMerge}");
            if (fromMerge)
                _ctx.onMegredCubeCreated.Invoke(cube.Id, cube);
            else 
               _ctx.onCubeCreated.Invoke(cube.Id, cube);
   
            return cube;
        }

        public void SetCollisionHandler(System.Action<Guid, Guid> onCubeCollision)
        {
            _onCubeCollision = onCubeCollision;
        }

        protected override void OnDispose()
        {
           
            _usedNumbers.Clear();
            _weightTable = default;
            base.OnDispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Games._2048.Scripts.Logic;
using Code.Games._2048.Scripts.View;
using Code.Games._2048.Scripts.Models;
using Code.Games._2048.Scripts.Presenters;
using UnityEngine;
using R3;

namespace Code.Games._2048.Scripts.Gameplay
{
    internal class Game2048CubeSpawnerPm : BaseDisposable
    {
        internal struct Ctx
        {
            public Transform spawnPoint;
            public GameObject cubePrefab;
            public CancellationToken cancellationToken;
            public System.Action<Guid, Guid> onCubeCollision; // (cubeId1, cubeId2)
        }

        private readonly Ctx _ctx;
        private readonly CompositeDisposable _compositeDisposable = new();
        
        public readonly Subject<Game2048CubeView> OnCubeSpawned = new();
        private Dictionary<Guid, CubePm> _cubes = new();
        private SortedSet<int> _usedNumbers = new();
        private System.Action<Guid, Guid> _onCubeCollision;
        private WeightedPickerUnity.Table _weightTable;

        public Dictionary<Guid, CubePm> Cubes => _cubes;

        public Game2048CubeSpawnerPm(Ctx ctx)
        {
            _ctx = ctx;
            _onCubeCollision = ctx.onCubeCollision;
            AddDispose(_compositeDisposable);
            AddDispose(OnCubeSpawned);
        }

        public Game2048CubeView CreateCube()
        {
            var number = _usedNumbers.Count > 0 
                ? WeightedPickerUnity.Pick(ref _weightTable) : 2;
            return CreateCubeAtPosition(_ctx.spawnPoint.position, number, false);
        }

        public Game2048CubeView CreateCubeAtPosition(Vector3 position, int number, bool fromMerge = true)
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
            
            cube.View.Line.gameObject.SetActive(!fromMerge);
            _cubes.Add(cube.Id, cube);
            AddDispose(cube);
            
            OnCubeSpawned.OnNext(cube.View);
            return cube.View;
        }

        public void RemoveCube(Guid cubeId)
        {
            if (_cubes.TryGetValue(cubeId, out var cube))
            {
                _cubes.Remove(cubeId);
                cube?.Dispose();
            }
        }

        public IEnumerable<Game2048CubeView> GetAllActiveCubeViews()
        {
            return _cubes.Values.Select(cube => cube.View);
        }

        public void SetCollisionHandler(System.Action<Guid, Guid> onCubeCollision)
        {
            _onCubeCollision = onCubeCollision;
        }

        protected override void OnDispose()
        {
            foreach (var cube in _cubes.Values)
            {
                cube?.Dispose();
            }
            _cubes.Clear();
            base.OnDispose();
        }

        public void DisposeCube(Guid cube1)
        {
            if (_cubes.TryGetValue(cube1, out var cube))
            {
                cube.Dispose();
                _cubes.Remove(cube1);
            }
        }
    }
}

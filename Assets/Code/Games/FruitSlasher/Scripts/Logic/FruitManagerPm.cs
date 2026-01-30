using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Code.Core.Tools;
using Code.Core.Tools.Pool;
using Code.Games.FruitSlasher.Scripts.View;
using Disposable;
using LightDI.Runtime;
using R3;
using TickHandler;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code.Games.FruitSlasher.Scripts.Logic
{
    internal class FruitManagerPm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public FruitSlasherSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
            public BladePm blade;
        }

        private readonly Ctx _ctx;
        private readonly ITickHandler _tickHandler;
        private readonly IPoolManager _pool;
        private Dictionary<Guid, FruitSpawnInfo> _fruitSpawners;
        private readonly FruitSpawnerView _viewSpawner;
        private float _spawnDelay;
        private ReactiveProperty<int> _score;

        struct FruitSpawnInfo
        {
            public GameObject prefab;
            public FruitPm logic;
            public int points;
        }

        public FruitManagerPm(Ctx ctx, [Inject] ITickHandler tickHandler, [Inject] IPoolManager pool)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            _pool = pool;
            _viewSpawner = _ctx.sceneContextView.FruitSpawnerView;
            _fruitSpawners = new Dictionary<Guid, FruitSpawnInfo>();
            _score = new ReactiveProperty<int>(0);
            AddDisposable(_score.Subscribe(value =>
                _ctx.sceneContextView.Score.text = value.ToString()));
        }

        protected override void OnDispose()
        {
            _tickHandler.FrameUpdate -= UpdateLogic;
            var keys = _fruitSpawners.Keys.ToList();
            foreach (var key in keys)
            {
                if (_fruitSpawners.ContainsKey(key))
                    Remove(key);
            }

            base.OnDispose();
        }

        public void Start()
        {
            _tickHandler.FrameUpdate += UpdateLogic;
        }

        public void Pause()
        {
            _tickHandler.FrameUpdate -= UpdateLogic;
        }

        private void UpdateLogic(float deltaTime)
        {
            SpawnFruit(deltaTime);
        }

        private void SpawnFruit(float deltaTime)
        {
            if (_spawnDelay > 0f)
            {
                _spawnDelay -= deltaTime;
                return;
            }

            _spawnDelay = Random.Range(_viewSpawner.MinDelay, _viewSpawner.MaxDelay);
            var prefab = _viewSpawner.FruitsPrefabs[Random.Range(0, _viewSpawner.FruitsPrefabs.Length)];

            var position = new Vector3();
            position.x = Random.Range(_viewSpawner.Spawner.bounds.min.x, _viewSpawner.Spawner.bounds.max.x);
            position.y = Random.Range(_viewSpawner.Spawner.bounds.min.y, _viewSpawner.Spawner.bounds.max.y);
            position.z = Random.Range(_viewSpawner.Spawner.bounds.min.z, _viewSpawner.Spawner.bounds.max.z);

            var rotation = Quaternion.Euler(0, 0, Random.Range(_viewSpawner.MinAngle, _viewSpawner.MaxAngle));

            var fruitObj = _pool.Get(prefab.FruitPrefab, position, rotation);
            var fruitView = fruitObj.GetComponent<FruitView>();

            var removeTrigger = new ReactiveTrigger<Guid>();
            var slicedTrigger = new ReactiveTrigger<Guid>();
            var newGuid = Guid.NewGuid();
            var fruitCtx = new FruitPm.Ctx
            {
                _lifeTime = _viewSpawner.LifeTime,
                fruitView = fruitView,
                remove = removeTrigger,
                id = newGuid,
                startForce = Random.Range(_viewSpawner.MinForce, _viewSpawner.MaxForce),
                blade = _ctx.blade,
                sliced = slicedTrigger
            };
            var fruit = FruitPmFactory.CreateFruitPm(fruitCtx);
            AddDisposable(removeTrigger.SubscribeOnce(Remove));
            AddDisposable(slicedTrigger.SubscribeOnce(id =>
            {
                if (_fruitSpawners.TryGetValue(id, out var fruitInfo))
                {
                        _score.Value += fruitInfo.points;
                }
            }));

            _fruitSpawners.Add(newGuid, new FruitSpawnInfo()
            {
                logic = fruit,
                prefab = prefab.FruitPrefab,
                points = prefab.FruitPoint
            });
        }

        private void Remove(Guid id)
        {
            if (_fruitSpawners.TryGetValue(id, out var fruitInfo))
            {
                if (!fruitInfo.logic.IsSliced)
                    _score.Value -= fruitInfo.points;
                _pool.Return(fruitInfo.prefab, fruitInfo.logic.FruitView.gameObject);
                fruitInfo.logic.Dispose();
                _fruitSpawners.Remove(id);
            }
        }
    }
}
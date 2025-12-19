using System.Collections.Generic;
using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Disposable;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities;
using UnityEngine;

namespace Asteroids.Code.Games.Game1.Scripts.Logic
{
    internal class EntitiesControllerPm : DisposableBase, IEntitiesController
    {
        internal struct Ctx
        {
        }

        private readonly Ctx _ctx;
        public IReadOnlyDictionary<int, EntityInfo> AllEntities => _entities;
        public PlayerModel _playerModel;
        
        private readonly Dictionary<int, EntityInfo> _entities;
        private int _indexator;
        
        public EntitiesControllerPm(Ctx ctx)
        {
            _ctx = ctx;
            _entities = new Dictionary<int, EntityInfo>();
        }
        public bool TryGetEntityInfo(int id, out EntityInfo entityInfo)
        {
            return _entities.TryGetValue(id, out entityInfo);
        }
        
        public bool TryDestroyEntity(int id, int? killer = null)
        {
            // Check if this controller is disposed
            if (IsDisposed)
            {
                return false;
            }
            
            if (!TryGetEntityInfo(id, out var entityInfo))
            {
                Debug.LogError($"Dont find entity with id = {id}");
                return false;
            }
            if (killer != null & killer == _playerModel.Id)
                _playerModel.Score.Value += entityInfo.Model.Reward;
            
            entityInfo?.Model.Destroy(killer);
            entityInfo?.Logic?.Dispose();
            return _entities.Remove(id);
        }
        public PlayerModel GetPlayerModel()
        {
            return _playerModel;
        }

        public void Clear()
        {
            foreach (var entity in _entities)
                entity.Value.Logic?.Dispose();
        }

        public int GenerateId()
        {
            return _indexator++;
        }
        public void AddEntity(int Id, EntityInfo entityInfo)
        {
            _entities.Add(Id, entityInfo);
            if (entityInfo.Model.EntityType is EntityType.PlayerShip)
                _playerModel = (PlayerModel) entityInfo.Model;
        }
        
        protected override void OnDispose()
        {
            Clear();
            base.OnDispose();
        }

    }
}
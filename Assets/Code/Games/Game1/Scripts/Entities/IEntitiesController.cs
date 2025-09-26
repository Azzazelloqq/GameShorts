using System.Collections.Generic;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities;

namespace Asteroids.Code.Games.Game1.Scripts.Entities
{
    internal interface IEntitiesController
    {
        public int GenerateId();
        public void AddEntity(int Id, EntityInfo entityInfo);
        public IReadOnlyDictionary<int, EntityInfo> AllEntities { get; }

        public bool TryGetEntityInfo(int id, out EntityInfo entityInfo);

        public bool TryDestroyEntity(int id, int? killer = null);

        public PlayerModel GetPlayerModel();

        void Clear();
    }
}
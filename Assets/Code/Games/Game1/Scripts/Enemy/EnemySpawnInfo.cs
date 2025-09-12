using Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core;
using Logic.Entities.Core;
using UnityEngine;

namespace Logic.Enemy
{
    public struct EnemySpawnInfo
    {
        public EntityType entityType;
        public Vector2 SpawnPosition;
        public float Angle;
    }
}
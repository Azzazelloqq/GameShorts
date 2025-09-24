using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using UnityEngine;

namespace Asteroids.Code.Games.Game1.Scripts.Enemy
{
    internal struct EnemySpawnInfo
    {
        public EntityType entityType;
        public Vector2 SpawnPosition;
        public float Angle;
    }
}
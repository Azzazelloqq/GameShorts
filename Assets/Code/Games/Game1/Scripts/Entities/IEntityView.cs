using System;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Logic.Entities.Core;

namespace Logic.Entities
{
	internal interface IEntityView
	{
		public event Action<CollidedInfo> Collided;
		public BaseModel Model { get; }
	}
}
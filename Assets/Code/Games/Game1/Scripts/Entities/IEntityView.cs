using System;
using Logic.Entities.Core;

namespace Logic.Entities
{
	public interface IEntityView
	{
		public event Action<CollidedInfo> Collided;
		public BaseModel Model { get; }
	}
}
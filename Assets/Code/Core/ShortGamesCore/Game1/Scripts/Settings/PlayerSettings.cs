using UnityEngine;
using UnityEngine.Serialization;

namespace Logic.Settings
{
	[CreateAssetMenu(fileName = "PlayerSettings", menuName = "MyAsteroids/Settings/Create player settings")]
	public class PlayerSettings : ScriptableObject
	{
		public Vector2 StartPosition;

		[Space]
		public float MaxSpeed;
		public float Acceleration;
		public float Deceleration;

		[Space]
		public float MaxRotationSpeed;
		public float RotationAcceleration;
		public float RotationDeceleration;

	}
}	
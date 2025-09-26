using UnityEngine;
using UnityEngine.Serialization;

namespace Logic.Settings
{
	[CreateAssetMenu(fileName = "PlayerSettings", menuName = "MyAsteroids/Settings/Create player settings")]
	internal class PlayerSettings : ScriptableObject
	{

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
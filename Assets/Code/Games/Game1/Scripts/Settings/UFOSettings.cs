using UnityEngine;

namespace Logic.Settings
{
    [CreateAssetMenu(fileName = "UFOSettings", menuName = "MyAsteroids/Settings/Create UFO settings")]
    public class UFOSettings  : ScriptableObject
    {
        public float MaxSpeed;
        public float Acceleration;
        public float MinSpeed;
        public float MaxRotateSpeed;
        public float RotationAcceleration;
        public float MinRotateSpeed;
    }
}
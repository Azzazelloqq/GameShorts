using UnityEngine;

namespace Logic.Settings
{
    [CreateAssetMenu(fileName = "AsteroidSettings", menuName = "MyAsteroids/Settings/Create asteroid settings")]
    internal class AsteroidSettings  : ScriptableObject
    {
        [Header("Big asteroid")]
        public float MaxSpeed;
        public float MinSpeed;
        public float MaxRotateSpeed;
        public float MinRotateSpeed;
        
        
        [Header("Small asteroid")]
        public float MaxSpeedSmall;
        public float MinSpeedSmall;
        public float MaxRotateSpeedSmall;
        public float MinRotateSpeedSmall;
    }
}
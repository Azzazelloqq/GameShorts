using UnityEngine;

namespace Logic.Settings
{
    [CreateAssetMenu(fileName = "LaserSettings", menuName = "MyAsteroids/Settings/Create laser settings")]
    internal class LaserSettings : ScriptableObject
    {
        [Header( "Laser Settings" )]
        public float LaserLength;
        public float LaserThickness;
        public float LaserCooldown;
        
        [Range(1,5)]
        public int CountLaserShots;
        public float LaserShotDuration;
        
        [Header("Rotation Settings")]
        public float LaserRotationSpeed = 90f; // degrees per second

    }
}
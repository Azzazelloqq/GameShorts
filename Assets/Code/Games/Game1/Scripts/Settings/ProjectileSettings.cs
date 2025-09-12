using UnityEngine;

namespace Logic.Settings
{
    [CreateAssetMenu(fileName = "ProjectileSettings", menuName = "MyAsteroids/Settings/Create projectile settings")]
    public class ProjectileSettings : ScriptableObject
    {
        public float ProjectileMaxSpeed;
        public float ProjectileRate;

    }
}
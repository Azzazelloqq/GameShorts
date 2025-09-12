using Logic.Settings;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game1.Scripts.View
{
    public class MainSceneContextView: SceneContextView
    {
        public PlayerSettings PlayerSettings;
        public GameSettings GameSettings;
        public AsteroidSettings AsteroidSettings;
        public UFOSettings UFOSettings;
        public ProjectileSettings ProjectileSettings;
        public LaserSettings laserSettings;
        public RewardSettings RewardSettings;
        
        [SerializeField] DynamicJoystick _joystick;

        public DynamicJoystick Joystick => _joystick;
    }
}
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Logic.Settings;
using UnityEngine;

namespace Asteroids.Code.Games.Game1.Scripts.View
{
    internal class MainSceneContextView: SceneContextView
    {
        public PlayerSettings PlayerSettings;
        public GameSettings GameSettings;
        public AsteroidSettings AsteroidSettings;
        public UFOSettings UFOSettings;
        public ProjectileSettings ProjectileSettings;
        public LaserSettings laserSettings;
        public RewardSettings RewardSettings;
        
        [SerializeField] Joystick _joystick;

        public Joystick Joystick => _joystick;
    }
}
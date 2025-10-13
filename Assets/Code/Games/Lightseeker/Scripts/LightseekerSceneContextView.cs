using Code.Core.ShortGamesCore.Game1.Scripts.View;
using UnityEngine;

namespace Lightseeker
{
    internal class LightseekerSceneContextView : SceneContextView
    {
        
        [SerializeField]
        private FloatingJoystick _joystick;
        
        public FloatingJoystick Joystick => _joystick;
    }
}


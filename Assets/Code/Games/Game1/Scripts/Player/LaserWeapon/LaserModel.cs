using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Logic.Entities.Core;
using R3;

namespace Logic.Player.LaserWeapon
{
    internal class LaserModel : BaseModel
    {
        public int OwnerId;
        public ReactiveProperty<float> Duration;
        public ReactiveProperty<float> Length;
        public ReactiveProperty<float> RotationSpeed;

        public LaserModel()
        {
            Duration = new ReactiveProperty<float>();
            Length = new ReactiveProperty<float>();
            RotationSpeed = new ReactiveProperty<float>();
        }
    }
}
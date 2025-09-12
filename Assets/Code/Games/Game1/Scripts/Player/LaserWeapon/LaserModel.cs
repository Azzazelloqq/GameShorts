using Logic.Entities.Core;
using R3;

namespace Logic.Player.LaserWeapon
{
    public class LaserModel : BaseModel
    {
        public int OwnerId;
        public ReactiveProperty<float> Duration;
        public ReactiveProperty<float> Length;

        public LaserModel()
        {
            Duration = new ReactiveProperty<float>();
            Length = new ReactiveProperty<float>();
        }
    }
}
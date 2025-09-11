using Logic.Entities.Core;
using R3;

namespace Logic.Player.ProjectileWeapon
{
    internal class ProjectileModel : BaseModel
    {
        public int OwnerId;
        public ReactiveProperty<float> Speed;
    }
}
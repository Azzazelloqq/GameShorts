using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
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
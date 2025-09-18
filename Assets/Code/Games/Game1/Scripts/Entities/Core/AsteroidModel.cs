
using Logic.Entities.Core;
using R3;

namespace Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core
{
    internal class AsteroidModel : BaseModel
    {
        public ReactiveProperty<bool> CanCollapse;

        public AsteroidModel() : base()
        {
            CanCollapse = new ReactiveProperty<bool>(true);
        }

    }
}
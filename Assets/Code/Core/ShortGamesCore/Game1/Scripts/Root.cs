using Code.Core.BaseDMDisposable.Scripts;
using LightDI.Runtime;
using TickHandler;

namespace Code.Core.ShortGamesCore.Game1.Scripts
{
    public class Root : BaseDisposable
    {
        public struct Ctx
        {
        }

        private Ctx _ctx;
        private readonly ITickHandler _tickHandler;

        public Root(Ctx ctx, [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
        }
    }
}
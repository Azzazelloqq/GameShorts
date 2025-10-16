using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using R3;

namespace Lightseeker
{
    internal class LightseekerMainUIPm : BaseDisposable
    {
        internal struct Ctx
        {
            public LightseekerMainUIView view;
            public LightseekerGameModel gameModel;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;

        public LightseekerMainUIPm(Ctx ctx)
        {
            _ctx = ctx;
            
            SubscribeToModelChanges();
            UpdateUI();
        }

        private void SubscribeToModelChanges()
        {
            AddDispose(_ctx.gameModel.CurrentLevel.Subscribe(_ => UpdateUI()));
            AddDispose(_ctx.gameModel.CollectedStars.Subscribe(_ => UpdateUI()));
        }

        private void UpdateUI()
        {
            if (_ctx.view == null) return;
            
            _ctx.view.UpdateLevel(_ctx.gameModel.CurrentLevel.Value, LightseekerGameModel.MaxLevel);
            _ctx.view.UpdateStars(_ctx.gameModel.CollectedStars.Value, LightseekerGameModel.StarsPerLevel);
        }
    }
}


using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Player;
using R3;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.UI
{
    /// <summary>
    /// Presenter для управления UI контейнера травы
    /// </summary>
    internal class GrassContainerPm : BaseDisposable
    {
        internal struct Ctx
        {
            public LawnmowerPlayerModel playerModel;
            public GrassContainerView view;
        }

        private readonly Ctx _ctx;
        private CompositeDisposable _disposables = new CompositeDisposable();

        public GrassContainerPm(Ctx ctx)
        {
            _ctx = ctx;
            
            // Инициализируем View
            _ctx.view.SetCtx(new GrassContainerView.Ctx());
            
            // Подписываемся на изменения модели
            _disposables.Add(_ctx.playerModel.GrassContainerCurrentAmount.Subscribe(OnContainerAmountChanged));
            _disposables.Add(_ctx.playerModel.GrassContainerMaxCapacity.Subscribe(OnContainerCapacityChanged));
            
            // Обновляем UI с текущими значениями
            UpdateUI();
        }

        protected override void OnDispose()
        {
            _disposables?.Dispose();
            base.OnDispose();
        }

        private void OnContainerAmountChanged(float newAmount)
        {
            UpdateUI();
        }

        private void OnContainerCapacityChanged(float newCapacity)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            float currentAmount = _ctx.playerModel.GrassContainerCurrentAmount.Value;
            float maxCapacity = _ctx.playerModel.GrassContainerMaxCapacity.Value;
            
            _ctx.view.UpdateContainer(currentAmount, maxCapacity);
        }
    }
}

using UnityEngine;
using R3;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Player;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.UI
{
    /// <summary>
    /// Presenter для управления UI фермера, который следует за игроком
    /// </summary>
    internal class FarmerUIPm : BaseDisposable
    {
        internal struct Ctx
        {
            public LawnmowerPlayerModel playerModel;
            public LawnmowerPlayerSettings settings;
            public Canvas targetCanvas;
            public UnityEngine.Camera worldCamera;
        }

        private readonly Ctx _ctx;
        private FarmerUIView _farmerUIView;
        private GrassContainerPm _containerPm;
        private CompositeDisposable _disposables = new CompositeDisposable();

        public FarmerUIPm(Ctx ctx)
        {
            _ctx = ctx;
            
            CreateFarmerUI();
            InitializeContainerPm();
            
            // Подписываемся на изменения позиции игрока
            _disposables.Add(_ctx.playerModel.Position.Subscribe(OnPlayerPositionChanged));
        }

        protected override void OnDispose()
        {
            _disposables?.Dispose();
            _containerPm?.Dispose();
            
            if (_farmerUIView != null)
            {
                Object.DestroyImmediate(_farmerUIView.gameObject);
            }
            
            base.OnDispose();
        }

        private void CreateFarmerUI()
        {
            // Используем префаб из настроек, если он есть
            GameObject prefabToUse = _ctx.settings.FarmerUIPrefab;
            Vector2 offsetToUse = _ctx.settings.FarmerUIOffset != Vector2.zero ? _ctx.settings.FarmerUIOffset : new Vector2(0, 50);

            if (prefabToUse == null)
            {
                Debug.LogWarning("FarmerUIPm: No farmer UI prefab assigned!");
                return;
            }

            if (_ctx.targetCanvas == null)
            {
                Debug.LogError("FarmerUIPm: No target canvas found!");
                return;
            }

            // Создаем экземпляр UI фермера
            GameObject farmerUIInstance = Object.Instantiate(prefabToUse, _ctx.targetCanvas.transform);
            _farmerUIView = farmerUIInstance.GetComponent<FarmerUIView>();

            if (_farmerUIView == null)
            {
                Debug.LogError("FarmerUIPm: Farmer UI prefab must have FarmerUIView component!");
                Object.DestroyImmediate(farmerUIInstance);
                return;
            }

            // Инициализируем View
            _farmerUIView.SetCtx(new FarmerUIView.Ctx
            {
                targetCanvas = _ctx.targetCanvas,
                worldCamera = _ctx.worldCamera,
                offset = offsetToUse
            });

            Debug.Log($"Farmer UI created with offset: {offsetToUse}");
        }

        private void InitializeContainerPm()
        {
            if (_farmerUIView?.ContainerView == null)
            {
                Debug.LogWarning("FarmerUIPm: No container view found in farmer UI");
                return;
            }

            var containerCtx = new GrassContainerPm.Ctx
            {
                playerModel = _ctx.playerModel,
                view = _farmerUIView.ContainerView
            };

            _containerPm = new GrassContainerPm(containerCtx);
        }

        private void OnPlayerPositionChanged(Vector2 playerWorldPosition)
        {
            _farmerUIView?.UpdatePosition(playerWorldPosition);
        }
    }
}

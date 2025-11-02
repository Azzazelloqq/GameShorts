using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.Gardener.Data;
using GameShorts.Gardener.UI;
using UnityEngine;

namespace GameShorts.Gardener.View
{
    internal class GardenerSceneContextView : BaseMonoBehaviour
    {
        [Header("Garden")]
        [SerializeField] private Transform _basePlatform; 
        [SerializeField] private Transform _gardenGrid; // Сетка для грядок
        [SerializeField] private Transform _plotUiPlacer; 
        [SerializeField] private GardenBounds _gardenBounds; // Границы огорода
        [SerializeField] private PlatformRotationView _platformRotationView; // View для вращения платформы
        
        [Header("Camera")]
        [SerializeField] private Camera _mainCamera;
        
        [Header("UI")]
        [SerializeField] private Canvas _uiCanvas;
        [SerializeField] private GardenerMainUIView _mainUIView;
        [SerializeField] private GardenerShopUIView _shopUIView;
        [SerializeField] private PlaceableItemsPanel _placeableItemsPanel;
        [SerializeField] private GameObject _plotUIBarPrefab;
        [SerializeField] private HarvestProgressBar _harvestProgressBar;
        
        [Header("Game Settings")]
        [SerializeField] private GardenerGameSettings _gameSettings;
        [SerializeField] private PlantSettings[] _availablePlants; // Доступные растения для игры
        
        // Properties
        public Transform GardenGrid => _gardenGrid;
        public GardenBounds GardenBounds => _gardenBounds;
        public PlatformRotationView PlatformRotationView => _platformRotationView;
        public Camera MainCamera => _mainCamera;
        public Canvas UiCanvas => _uiCanvas;
        public GardenerMainUIView MainUIView => _mainUIView;
        public GardenerShopUIView ShopUIView => _shopUIView;
        public PlaceableItemsPanel PlaceableItemsPanel => _placeableItemsPanel;
        public GameObject PlotUIBarPrefab => _plotUIBarPrefab;
        public HarvestProgressBar HarvestProgressBar => _harvestProgressBar;
        public GardenerGameSettings GameSettings => _gameSettings;
        public PlantSettings[] AvailablePlants => _availablePlants;

        public Transform BasePlatform => _basePlatform;

        public Transform PlotUiPlacer => _plotUiPlacer;
    }
}
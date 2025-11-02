using System;
using System.Collections.Generic;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using GameShorts.Gardener.Core;
using GameShorts.Gardener.Data;
using GameShorts.Gardener.View;
using GameShorts.Gardener.Gameplay.Modes;
using GameShorts.Gardener.UI;
using LightDI.Runtime;
using R3;
using UnityEngine;

namespace GameShorts.Gardener.Gameplay
{
    internal class GardenerGameplayPm : BaseDisposable
    {
        internal struct Ctx
        {
            public GardenerSceneContextView sceneContextView;
            public CancellationToken cancellationToken;
            public ReactiveProperty<bool> isPaused;
            public PlantSettings[] availablePlants;
            public GardenerGameSettings gameSettings;
        }

        private readonly Ctx _ctx;
        private readonly List<PlotPm> _plots = new List<PlotPm>();
        private readonly ReactiveProperty<int> _money;
        private readonly IPoolManager _poolManager;
        private readonly GardenerModeManager _modeManager;
        private readonly PlotUIBarManager _plotUIBarManager;
        private readonly GardenerInputHandler _inputHandler;
        private readonly InventoryManager _inventoryManager;

        public ReactiveProperty<int> Money => _money;
        public GardenerModeManager ModeManager => _modeManager;
        public IReadOnlyList<PlotPm> Plots => _plots;

        public InventoryManager InventoryManager => _inventoryManager;

        // Доступные растения для покупки
        public PlantSettings[] AvailablePlants => _ctx.availablePlants;

        public GardenerGameplayPm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;

            // Инициализируем деньги из настроек
            _money = new ReactiveProperty<int>(_ctx.gameSettings.StartingCapital);

            // Создаем менеджер инвентаря
            _inventoryManager = new InventoryManager();
            AddDispose(_inventoryManager);

            // Создаем менеджер UI баров
            _plotUIBarManager = PlotUIBarManagerFactory.CreatePlotUIBarManager(new PlotUIBarManager.Ctx
            {
                uiCanvas = _ctx.sceneContextView.UiCanvas,
                barPrefab = _ctx.sceneContextView.PlotUIBarPrefab,
                camera = _ctx.sceneContextView.MainCamera,
                holderUI = _ctx.sceneContextView.PlotUiPlacer
            });
            AddDispose(_plotUIBarManager);

            // Создаем менеджер режимов
            _modeManager = new GardenerModeManager();
            AddDispose(_modeManager);

            // Регистрируем режимы
            RegisterModes();

            // Создаем обработчик ввода
            _inputHandler = GardenerInputHandlerFactory.CreateGardenerInputHandler(new GardenerInputHandler.Ctx
            {
                mainCamera = _ctx.sceneContextView.MainCamera,
                modeManager = _modeManager,
                findPlotAtPosition = (pos) => FindPlotAtPosition(pos)
            });
            AddDispose(_inputHandler);

            // Создаем обработчик вращения платформы
            if (_ctx.sceneContextView.PlatformRotationView != null)
            {
                var rotationPm = new PlatformRotationPm(new PlatformRotationPm.Ctx
                {
                    view = _ctx.sceneContextView.PlatformRotationView,
                    platformTransform = _ctx.sceneContextView.BasePlatform,
                    gameSettings = _ctx.gameSettings,
                    camera = _ctx.sceneContextView.MainCamera
                });
                AddDispose(rotationPm);
            }
        }

        protected override void OnDispose()
        {
            foreach (var plot in _plots)
            {
                plot.Dispose();
            }

            _plots.Clear();
        }

        private void RegisterModes()
        {
            // Проверяем настройки грядок
            if (_ctx.gameSettings == null)
            {
                Debug.LogError("GardenerGameSettings is null!");
                return;
            }

            if (_ctx.gameSettings.PlotSettings == null)
            {
                Debug.LogError("PlotSettings is null! Please assign PlotSettings in GardenerGameSettings.");
                return;
            }

            if (_ctx.gameSettings.PlotSettings.PlotPrefab == null)
            {
                Debug.LogError("PlotPrefab is null! Please assign a prefab in PlotSettings.");
                return;
            }

            // Создаем элементы для размещения (грядки)
            var placeableItems = new PlaceableItem[]
            {
                new PlaceableItem
                {
                    ItemName = "Грядка",
                    Icon = _ctx.gameSettings.PlotSettings.Icon,
                    Prefab = _ctx.gameSettings.PlotSettings.PlotPrefab
                }
            };

            Debug.Log($"Registering Harvey mode with placeable items: {placeableItems.Length}");

            // Регистрируем режим Harvey
            var harveyMode = HarveyModeFactory.CreateHarveyMode(new HarveyMode.Ctx
            {
                placeableItems = placeableItems,
                harvestProgressBar = _ctx.sceneContextView.HarvestProgressBar,
                gameSettings = _ctx.gameSettings,
                onPlotInteraction = OnPlotInteraction,
                onItemPlaced = OnItemPlaced,
                onPlotRemoved = RemovePlot,
                camera = _ctx.sceneContextView.MainCamera
            });
            _modeManager.RegisterMode(harveyMode);

            // Регистрируем режим полива
            var wateringMode = new WateringMode(new WateringMode.Ctx
            {
                onPlotInteraction = OnPlotInteraction
            });
            _modeManager.RegisterMode(wateringMode);

            // Регистрируем режим инвентаря
            var inventoryMode = new InventoryMode(new InventoryMode.Ctx
            {
                inventoryManager = _inventoryManager,
                findPlotAtPosition = pos => FindPlotAtPosition(pos),
                plotUIBarManager = _plotUIBarManager
            });
            _modeManager.RegisterMode(inventoryMode);

            Debug.Log("Modes registered successfully");
        }

        /// <summary>
        /// Создает грядку в указанной позиции
        /// </summary>
        public void CreatePlot(Vector3 worldPosition)
        {
            if (_ctx.sceneContextView.GardenGrid == null || _ctx.gameSettings.PlotSettings?.PlotPrefab == null)
            {
                Debug.LogError("Garden grid or plot prefab is missing!");
                return;
            }

            // Проверяем, нет ли уже грядки в этой позиции
            float minDistance = .5f; // Минимальное расстояние между грядками
            if (IsPositionOccupied(worldPosition, minDistance))
            {
                Debug.LogWarning($"Cannot place plot at {worldPosition} - position is too close to another plot!");
                return;
            }

            var plotObject =
                _poolManager.Get(_ctx.gameSettings.PlotSettings.PlotPrefab, _ctx.sceneContextView.GardenGrid);
            plotObject.transform.position = worldPosition;
            var plotView = plotObject.GetComponent<PlotView>();

            if (plotView == null)
            {
                Debug.LogError("PlotView component is missing on plot prefab!");
                return;
            }

            var plotPm = PlotPmFactory.CreatePlotPm(new PlotPm.Ctx
            {
                plotView = plotView,
                cancellationToken = _ctx.cancellationToken,
                onPlantHarvested = OnPlantHarvested,
                preparationTime = _ctx.gameSettings.PlotPreparationTime
            });

            _plots.Add(plotPm);

            // Создаем UI бар 
           // _plotUIBarManager.CreateBarForPlot(plotPm, plotView.transform);
        }

        /// <summary>
        /// Проверяет, занята ли позиция другой грядкой
        /// </summary>
        private bool IsPositionOccupied(Vector3 position, float minDistance)
        {
            foreach (var plot in _plots)
            {
                float distance = Vector3.Distance(plot.WorldPosition, position);
                if (distance < minDistance)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnItemPlaced(PlaceableItem item, Vector3 worldPosition)
        {
            Debug.Log($"OnItemPlaced called: {item.ItemName} at {worldPosition}");

            // Размещаем грядку
            CreatePlot(worldPosition);
        }

        private void OnPlotInteraction(PlotPm plot, Vector3 worldPosition)
        {
            // Можно использовать для будущих взаимодействий
        }

        private void OnPlantHarvested(int reward)
        {
            _money.Value += reward;
        }

        /// <summary>
        /// Покупает семена и добавляет в инвентарь
        /// </summary>
        public bool TryBuySeeds(PlantSettings plantSettings)
        {
            if (_money.Value >= plantSettings.SeedPrice)
            {
                _money.Value -= plantSettings.SeedPrice;
                _inventoryManager.AddSeeds(plantSettings, 1);
                Debug.Log($"Bought {plantSettings.PlantName} for {plantSettings.SeedPrice}");
                return true;
            }
            else
            {
                Debug.LogWarning($"Not enough money! Have: {_money.Value}, Need: {plantSettings.SeedPrice}");
                return false;
            }
        }


        /// <summary>
        /// Покупает семена и добавляет в инвентарь
        /// </summary>
        public void BuyPlant(PlantSettings plant)
        {
            TryBuySeeds(plant);
        }

        /// <summary>
        /// Находит грядку по точке в мире (raycast)
        /// </summary>
        public PlotPm FindPlotAtPosition(Vector3 worldPosition, float radius = 0.5f)
        {
            foreach (var plot in _plots)
            {
                if (Vector3.Distance(plot.WorldPosition, worldPosition) < radius)
                {
                    return plot;
                }
            }

            return null;
        }

        /// <summary>
        /// Удаляет грядку
        /// </summary>
        public void RemovePlot(PlotPm plot)
        {
            if (_plots.Contains(plot))
            {
                _plotUIBarManager.RemoveBar(plot);
                _plots.Remove(plot);
                plot.Dispose();

                // Возвращаем объект в пул
                _poolManager.Return(_ctx.gameSettings.PlotSettings.PlotPrefab, plot.GameObject);
            }
        }
    }
}
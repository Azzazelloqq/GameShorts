using System.Collections.Generic;
using Disposable;
using Code.Core.Tools.Pool;
using GameShorts.Gardener.Gameplay;
using LightDI.Runtime;
using R3;
using TickHandler;
using UnityEngine;

namespace GameShorts.Gardener.UI
{
    /// <summary>
    /// Менеджер UI баров для грядок
    /// Создает, обновляет и удаляет бары над грядками
    /// </summary>
    internal class PlotUIBarManager : DisposableBase
    {
        internal struct Ctx
        {
            public Canvas uiCanvas;
            public GameObject barPrefab;
            public Camera camera;
            public Transform holderUI;
        }
        
        private readonly Ctx _ctx;
        private readonly Dictionary<PlotPm, PlotUIBar> _plotBars = new Dictionary<PlotPm, PlotUIBar>();
        private readonly ITickHandler _tickHandler;
        private readonly IPoolManager _poolManager;

        public PlotUIBarManager(Ctx ctx, [Inject] ITickHandler tickHandler, [Inject] IPoolManager  poolManager) 
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _tickHandler = tickHandler;
            
            // Подписываемся на обновление позиций каждый кадр
            _tickHandler.FrameUpdate += OnUpdate;
        }
        
        /// <summary>
        /// Создает UI бар для грядки
        /// </summary>
        public void CreateBarForPlot(PlotPm plot, Transform plotTransform)
        {
            if (_plotBars.ContainsKey(plot))
            {
                Debug.LogWarning("PlotUIBar already exists for this plot!");
                return;
            }
            
            var barInstance = _poolManager.Get(_ctx.barPrefab, _ctx.holderUI);
            barInstance.transform.localScale = Vector3.one;
            var barView =  barInstance.GetComponent<PlotUIBar>();
            barView.Initialize(_ctx.camera, plotTransform);
            
            _plotBars[plot] = barView;
            
            // Подписываемся на изменения в грядке
            AddDisposable(plot.GrowthProgressObservable?.Subscribe(progress => barView.SetGrowthProgress(progress)));
            AddDisposable(plot.WaterLevelObservable?.Subscribe(level => barView.SetWaterLevel(level)));
            
            // Подписываемся на изменение состояния, чтобы скрывать бар для пустых грядок, созревших и гнилых растений
            AddDisposable(plot.CurrentStateObservable?.Subscribe(state =>
            {
                // Скрываем бар для:
                // - Пустых грядок (Empty)
                // - Созревших растений (Fruit/Flowering) - рост завершен
                // - Гнилых растений (Rotten) - рост прекращен
                bool shouldBeVisible = state != Data.PlantState.Empty && 
                                       state != Data.PlantState.Fruit && 
                                       state != Data.PlantState.Flowering &&
                                       state != Data.PlantState.Rotten;
                barView.SetVisible(shouldBeVisible);
            }));
        }
        
        /// <summary>
        /// Удаляет UI бар для грядки
        /// </summary>
        public void RemoveBar(PlotPm plot)
        {
            if (_plotBars.TryGetValue(plot, out var bar))
            {
                if (bar != null)
                {
                    _poolManager.Return(_ctx.barPrefab, bar.gameObject);
                }
                _plotBars.Remove(plot);
            }
        }
        
        /// <summary>
        /// Обновляет позиции всех баров
        /// </summary>
        private void OnUpdate(float deltaTime)
        {
            foreach (var bar in _plotBars.Values)
            {
                if (bar != null)
                {
                    bar.UpdatePosition();
                }
            }
        }
        
        protected override void OnDispose()
        {
            _tickHandler.FrameUpdate -= OnUpdate;
            
            // Удаляем все бары
            foreach (var bar in _plotBars.Values)
            {
                if (bar != null)
                {
                    _poolManager.Return(_ctx.barPrefab, bar.gameObject);
                }
            }
            
            _plotBars.Clear();
        }
    }
}


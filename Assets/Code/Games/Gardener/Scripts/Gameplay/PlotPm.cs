using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.Gardener.Core;
using GameShorts.Gardener.Data;
using GameShorts.Gardener.View;
using LightDI.Runtime;
using R3;
using TickHandler;
using UnityEngine;

namespace GameShorts.Gardener.Gameplay
{
    internal class PlotPm : BaseDisposable
    {
        internal struct Ctx
        {
            public PlotView plotView;
            public CancellationToken cancellationToken;
            public Action<int> onPlantHarvested;
            public float preparationTime;
        }

        private readonly Ctx _ctx;
        private PlantSettings _currentPlantSettings;
        private ReactiveProperty<PlantState> _currentState = new ReactiveProperty<PlantState>(PlantState.Empty);
        private ReactiveProperty<float> _waterLevel = new ReactiveProperty<float>(1f);
        private ReactiveProperty<float> _growthProgressProperty = new ReactiveProperty<float>(0f);
        private ReactiveProperty<bool> _isPreparationComplete = new ReactiveProperty<bool>(false);
        private float _growthProgress;
        private float _timeSinceLastWatering;
        private float _preparationTimer;
        private IDisposable _updateSubscription;
        private readonly ITickHandler _tickHandler;

        public PlantState CurrentState => _currentState.Value;
        public float WaterLevel => _waterLevel.Value;
        public float GrowthProgress => _growthProgressProperty.Value;
        public bool IsPreparationComplete => _isPreparationComplete.Value;
        public Vector3 WorldPosition => _ctx.plotView.transform.position;
        public GameObject GameObject => _ctx.plotView.gameObject;
        
        // Observable свойства для UI баров
        public ReadOnlyReactiveProperty<float> GrowthProgressObservable => _growthProgressProperty;
        public ReadOnlyReactiveProperty<float> WaterLevelObservable => _waterLevel;
        public ReadOnlyReactiveProperty<bool> IsPreparationCompleteObservable => _isPreparationComplete;
        public ReadOnlyReactiveProperty<PlantState> CurrentStateObservable => _currentState;

        public PlotPm(Ctx ctx, [Inject] ITickHandler tickHandler) 
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            _preparationTimer = 0f;

            _tickHandler.FrameUpdate += UpdatePlant;
                
            // Подписываемся на изменения состояния растения
            AddDispose(_currentState.Subscribe(state => _ctx.plotView.UpdateState(state, _currentPlantSettings)));
            
            // Начинаем с неподготовленной грядки
            _isPreparationComplete.Value = false;
        }
        
        /// <summary>
        /// Инициализирует грядку как готовую к использованию (для уже существующих грядок)
        /// </summary>
        public void SetPrepared()
        {
            _isPreparationComplete.Value = true;
            _preparationTimer = _ctx.preparationTime;
        }

        private void UpdatePlant(float deltaTime)
        {
            // Обновляем подготовку грядки
            if (!_isPreparationComplete.Value)
            {
                _preparationTimer += deltaTime;
                if (_preparationTimer >= _ctx.preparationTime)
                {
                    _isPreparationComplete.Value = true;
                }
                return;
            }
            
            if (_currentState.Value is PlantState.Empty or PlantState.Rotten)
                return;

            if (_currentPlantSettings == null)
                return;

            // Если растение уже зрелое (Fruit или Flowering), оно не растет и не требует воды
            // Просто ждем когда игрок его соберет
            if (IsPlantMature())
            {
                // Зрелое растение не меняется, не требует воды и не может сгнить
                return;
            }

            // Обновляем время с последнего полива
            _timeSinceLastWatering += deltaTime;
            
            // Постепенно уменьшаем уровень воды
            _waterLevel.Value -= deltaTime / _currentPlantSettings.WateringInterval;
            _waterLevel.Value = Mathf.Max(0f, _waterLevel.Value);
            
            // Если растение не поливали долго, оно гниет
            if (_waterLevel.Value <= 0f)
            {
                _currentState.Value = PlantState.Rotten;
                return;
            }

            // Обновляем рост растения (растет только если достаточно воды)
            if (_waterLevel.Value > 0.3f)
            {
                _growthProgress += deltaTime / _currentPlantSettings.GrowthTime;
                _growthProgressProperty.Value = Mathf.Clamp01(_growthProgress);
                UpdateGrowthState();
            }
        }

        private void UpdateGrowthState()
        {
            var newState = _currentState.Value;

            if (_growthProgress >= 1f)
            {
                newState = _currentPlantSettings.HasFruits ? PlantState.Fruit : PlantState.Flowering;
                // Фиксируем прогресс на 100% когда растение созрело
                _growthProgress = 1f;
                _growthProgressProperty.Value = 1f;
            }
            else if (_growthProgress >= 0.75f)
            {
                newState = PlantState.Flowering;
            }
            else if (_growthProgress >= 0.5f)
            {
                newState = PlantState.Bush;
            }
            else if (_growthProgress >= 0.25f)
            {
                newState = PlantState.Sprout;
            }

            if (newState != _currentState.Value)
            {
                _currentState.Value = newState;
            }
        }

        public void PlantSeed(PlantSettings plantSettings)
        {
            if (_currentState.Value != PlantState.Empty)
                return;
                
            // Нельзя сажать, если грядка не подготовлена
            if (!_isPreparationComplete.Value)
                return;

            _currentPlantSettings = plantSettings;
            _currentState.Value = PlantState.Seed;
            _waterLevel.Value = 1f;
            _growthProgress = 0f;
            _growthProgressProperty.Value = 0f;
            _timeSinceLastWatering = 0f;
        }

        public void Water()
        {
            if (_currentState.Value == PlantState.Empty || _currentState.Value == PlantState.Rotten)
                return;

            _waterLevel.Value = 1f;
            _timeSinceLastWatering = 0f;
        }

        public void Harvest()
        {
            if (_currentState.Value != PlantState.Fruit && _currentState.Value != PlantState.Flowering)
                return;

            _ctx.onPlantHarvested?.Invoke(_currentPlantSettings.HarvestPrice);
            Clear();
        }

        public void Clear()
        {
            _currentState.Value = PlantState.Empty;
            _currentPlantSettings = null;
            _waterLevel.Value = 1f;
            _growthProgress = 0f;
            _growthProgressProperty.Value = 0f;
            _timeSinceLastWatering = 0f;
        }
        
        /// <summary>
        /// Проверяет, является ли растение созревшим
        /// </summary>
        public bool IsPlantMature()
        {
            return _currentState.Value == PlantState.Fruit || _currentState.Value == PlantState.Flowering;
        }
        
        /// <summary>
        /// Проверяет, является ли растение сгнившим
        /// </summary>
        public bool IsPlantRotten()
        {
            return _currentState.Value == PlantState.Rotten;
        }
        
        protected override void OnDispose()
        {
            _tickHandler.FrameUpdate -= UpdatePlant;
            base.OnDispose();
        }
    }
}
using System;
using UnityEngine;
using R3;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Level;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Player
{
    /// <summary>
    /// Менеджер контейнера для травы - отвечает за заполнение и опустошение
    /// </summary>
    internal class GrassContainerManager : BaseDisposable
    {
        public struct Ctx
        {
            public LawnmowerPlayerModel playerModel;
            public LawnmowerPlayerSettings settings;
            public EmptyingZonePm emptyingZonePm;
        }

        private readonly Ctx _ctx;
        private float _emptyingStartTime;
        private bool _wasEmptyingLastFrame;

        // Events
        public event Action<int> OnGrassAdded; // количество добавленной травы
        public event Action<float> OnContainerEmptied; // количество опустошенной травы

        public GrassContainerManager(Ctx ctx)
        {
            _ctx = ctx;
            
            // Инициализируем настройки контейнера
            _ctx.playerModel.GrassContainerMaxCapacity.Value = _ctx.settings.ContainerMaxCapacity;
            
            // Подписываемся на зону опустошения
            if (_ctx.emptyingZonePm != null)
            {
                _ctx.emptyingZonePm.OnPlayerEntered += OnPlayerEnteredEmptyingZone;
                _ctx.emptyingZonePm.OnPlayerExited += OnPlayerExitedEmptyingZone;
            }
            
            // Подписываемся на изменения состояния зоны для обновления опустошения
            AddDispose(_ctx.playerModel.IsInEmptyingZone.Subscribe(OnEmptyingZoneStateChanged));
        }

        protected override void OnDispose()
        {
            // Отписываемся от событий зоны опустошения
            if (_ctx.emptyingZonePm != null)
            {
                _ctx.emptyingZonePm.OnPlayerEntered -= OnPlayerEnteredEmptyingZone;
                _ctx.emptyingZonePm.OnPlayerExited -= OnPlayerExitedEmptyingZone;
            }
            
            base.OnDispose();
        }

        /// <summary>
        /// Добавить траву в контейнер
        /// </summary>
        public void AddGrass(int grassTilesCount)
        {
            if (grassTilesCount <= 0) return;
            
            float grassAmount = grassTilesCount * _ctx.settings.GrassPerTile;
            float currentAmount = _ctx.playerModel.GrassContainerCurrentAmount.Value;
            float maxCapacity = _ctx.playerModel.GrassContainerMaxCapacity.Value;
            
            // Проверяем, не превышаем ли максимальную вместимость
            float newAmount = Mathf.Min(currentAmount + grassAmount, maxCapacity);
            float actuallyAdded = newAmount - currentAmount;
            
            if (actuallyAdded > 0)
            {
                _ctx.playerModel.GrassContainerCurrentAmount.Value = newAmount;
                OnGrassAdded?.Invoke(grassTilesCount);
                
                Debug.Log($"Added {actuallyAdded} grass to container. Current: {newAmount}/{maxCapacity}");
            }
        }

        /// <summary>
        /// Обновление системы опустошения (вызывается каждый кадр)
        /// </summary>
        public void UpdateEmptying(float deltaTime)
        {
            // Отладочная информация
            bool isInZone = _ctx.playerModel.IsInEmptyingZone.Value;
            float currentAmount = _ctx.playerModel.GrassContainerCurrentAmount.Value;
            
            if (!isInZone) return;
            if (currentAmount <= 0) return;
            
            // Рассчитываем скорость опустошения
            float emptyingSpeed = _ctx.playerModel.GrassContainerMaxCapacity.Value / _ctx.settings.EmptyingTime;
            float emptyAmount = emptyingSpeed * deltaTime;
            
            float newAmount = Mathf.Max(0, currentAmount - emptyAmount);
            float actuallyEmptied = currentAmount - newAmount;
            
            _ctx.playerModel.GrassContainerCurrentAmount.Value = newAmount;
            
            // Обновляем прогресс опустошения
            UpdateEmptyingProgress();
            
            if (actuallyEmptied > 0)
            {
                OnContainerEmptied?.Invoke(actuallyEmptied);
                Debug.Log($"Container emptying: {actuallyEmptied:F2} grass removed. Current: {newAmount:F2}");
            }
        }

        /// <summary>
        /// Получить процент заполнения контейнера (0-1)
        /// </summary>
        public float GetFillPercentage()
        {
            float maxCapacity = _ctx.playerModel.GrassContainerMaxCapacity.Value;
            if (maxCapacity <= 0) return 0f;
            
            return _ctx.playerModel.GrassContainerCurrentAmount.Value / maxCapacity;
        }

        /// <summary>
        /// Проверить, заполнен ли контейнер
        /// </summary>
        public bool IsContainerFull()
        {
            return GetFillPercentage() >= 1f;
        }

        /// <summary>
        /// Проверить, пуст ли контейнер
        /// </summary>
        public bool IsContainerEmpty()
        {
            return _ctx.playerModel.GrassContainerCurrentAmount.Value <= 0;
        }

        private void OnPlayerEnteredEmptyingZone(GameObject player)
        {
            _ctx.playerModel.IsInEmptyingZone.Value = true;
            _emptyingStartTime = Time.time;
            float currentAmount = _ctx.playerModel.GrassContainerCurrentAmount.Value;
            Debug.Log($"Player entered emptying zone - starting container emptying. Current amount: {currentAmount}");
        }

        private void OnPlayerExitedEmptyingZone(GameObject player)
        {
            _ctx.playerModel.IsInEmptyingZone.Value = false;
            _ctx.playerModel.EmptyingProgress.Value = 0f; // Сбрасываем прогресс
            float currentAmount = _ctx.playerModel.GrassContainerCurrentAmount.Value;
            Debug.Log($"Player exited emptying zone - stopping container emptying. Current amount: {currentAmount}");
        }

        private void OnEmptyingZoneStateChanged(bool isInZone)
        {
            if (isInZone && !_wasEmptyingLastFrame)
            {
                _emptyingStartTime = Time.time;
            }
            
            _wasEmptyingLastFrame = isInZone;
        }

        private void UpdateEmptyingProgress()
        {
            if (!_ctx.playerModel.IsInEmptyingZone.Value)
            {
                _ctx.playerModel.EmptyingProgress.Value = 0f;
                return;
            }

            float elapsedTime = Time.time - _emptyingStartTime;
            float progress = Mathf.Clamp01(elapsedTime / _ctx.settings.EmptyingTime);
            _ctx.playerModel.EmptyingProgress.Value = progress;
        }
    }
}

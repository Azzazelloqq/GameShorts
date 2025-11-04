using System;
using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.Gardener.Gameplay.Modes;
using LightDI.Runtime;
using R3;
using TickHandler;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameShorts.Gardener.Gameplay
{
    /// <summary>
    /// Обработчик ввода для игры Gardener
    /// Обрабатывает клики/тапы на грядки и передает события в текущий режим
    /// </summary>
    internal class GardenerInputHandler : BaseDisposable
    {
        public struct Ctx
        {
            public Camera mainCamera;
            public GardenerModeManager modeManager;
            public Func<Vector3, PlotPm> findPlotAtPosition;
            public ReactiveProperty<bool> isPaused;
        }
        
        private readonly Ctx _ctx;
        private readonly ITickHandler _tickHandler;
        
        private bool _isHolding;
        private float _holdTime;
        private PlotPm _currentPlot;
        private Vector3 _holdWorldPosition;
        private Vector2 _holdScreenPosition; // Позиция курсора на экране
        private bool _isInputEnabled = true;
        
        public GardenerInputHandler(Ctx ctx, [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            _tickHandler.FrameUpdate += OnUpdate;
            
            // Подписываемся на паузу
            if (_ctx.isPaused != null)
            {
                AddDispose(_ctx.isPaused.Subscribe(isPaused =>
                {
                    _isInputEnabled = !isPaused;
                }));
            }
        }
        
        private void OnUpdate(float deltaTime)
        {
            // Проверяем ввод
            HandleInput(deltaTime);
        }
        
        private void HandleInput(float deltaTime)
        {
            // Если игра на паузе, не обрабатываем ввод
            if (!_isInputEnabled)
                return;
            
            // Поддержка сенсорного ввода для мобильных устройств
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        OnPointerDown(touch.position);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (_isHolding)
                        {
                            OnPointerHold(deltaTime);
                        }
                        break;
                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (_isHolding)
                        {
                            OnPointerUp();
                        }
                        break;
                }
            }
            // Поддержка мыши для редактора и десктопа
            else if (Input.GetMouseButtonDown(0))
            {
                OnPointerDown(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && _isHolding)
            {
                OnPointerHold(deltaTime);
            }
            else if (Input.GetMouseButtonUp(0) && _isHolding)
            {
                OnPointerUp();
            }
        }
        
        private void OnPointerDown(Vector3 screenPosition)
        {
            // Проверяем, не кликнули ли мы на UI
            if (IsPointerOverUI(screenPosition))
                return;
            
            Ray ray = _ctx.mainCamera.ScreenPointToRay(screenPosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector3 worldPosition = hit.point;
                PlotPm plot = _ctx.findPlotAtPosition(worldPosition);
                
                if (plot != null)
                {
                    _isHolding = true;
                    _holdTime = 0f;
                    _currentPlot = plot;
                    _holdWorldPosition = worldPosition;
                    _holdScreenPosition = screenPosition; // Сохраняем позицию курсора
                    
                    // Передаем событие в текущий режим с экранной позицией
                    _ctx.modeManager.CurrentMode?.OnPlotPressed(plot, worldPosition, _holdScreenPosition);
                }
            }
        }
        
        private void OnPointerHold(float deltaTime)
        {
            if (_currentPlot == null)
            {
                _isHolding = false;
                return;
            }
            
            _holdTime += deltaTime;
            
            // Передаем событие удержания в текущий режим
            _ctx.modeManager.CurrentMode?.OnPlotHeld(_currentPlot, _holdWorldPosition, _holdTime);
        }
        
        private void OnPointerUp()
        {
            if (_currentPlot != null)
            {
                // Передаем событие отпускания в текущий режим
                _ctx.modeManager.CurrentMode?.OnPlotReleased(_currentPlot);
            }
            
            _isHolding = false;
            _holdTime = 0f;
            _currentPlot = null;
        }
        
        private bool IsPointerOverUI(Vector3 screenPosition)
        {
            // Проверяем, находится ли курсор/палец над UI элементом
            if (EventSystem.current == null)
                return false;
            
            // Для мобильных устройств - проверяем касание
            if (Input.touchCount > 0)
            {
                int touchId = Input.GetTouch(0).fingerId;
                return EventSystem.current.IsPointerOverGameObject(touchId);
            }
            
            // Для мыши
            return EventSystem.current.IsPointerOverGameObject();
        }
        
        protected override void OnDispose()
        {
            _tickHandler.FrameUpdate -= OnUpdate;
            _isHolding = false;
            _currentPlot = null;
        }
    }
}


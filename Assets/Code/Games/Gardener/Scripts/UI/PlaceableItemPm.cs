using System;
using Disposable;
using GameShorts.Gardener.Gameplay;
using GameShorts.Gardener.Gameplay.Modes;
using GameShorts.Gardener.View;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameShorts.Gardener.UI
{
    /// <summary>
    /// Presenter для PlaceableItem - содержит всю логику drag-and-drop
    /// </summary>
    internal class PlaceableItemPm : DisposableBase
    {
        public struct Ctx
        {
            public PlaceableItem item;
            public PlaceableItemView view;
            public Canvas canvas;
            public Camera worldCamera;
            public GardenBounds gardenBounds;
            public Action<PlaceableItem, Vector3> onItemPlaced;
            public Func<Vector3, PlotPm> findPlotAtPosition;
        }

        private readonly Ctx _ctx;
        private bool _isDragging;

        public PlaceableItemPm(Ctx ctx)
        {
            _ctx = ctx;
            
            // Настраиваем View
            _ctx.view.SetData(_ctx.item);
            
            // Подписываемся на события View
            _ctx.view.OnBeginDragEvent += HandleBeginDrag;
            _ctx.view.OnDragEvent += HandleDrag;
            _ctx.view.OnEndDragEvent += HandleEndDrag;
        }

        private void HandleBeginDrag(PointerEventData eventData)
        {
            if (_ctx.item == null)
            {
                Debug.LogWarning("Cannot begin drag - item is null");
                return;
            }

            Debug.Log($"Begin drag: {_ctx.item.ItemName}");
            _isDragging = true;

            // Если есть префаб, создаем 3D превью, иначе UI превью
            if (_ctx.item.Prefab != null)
            {
                // Получаем начальную позицию для 3D превью
                if (TryGetWorldPosition(eventData, out Vector3 worldPosition))
                {
                    _ctx.view.Create3DPreview(_ctx.item.Prefab, worldPosition);
                }
            }
            else
            {
                // Создаем UI превью (картинку под курсором) для элементов без префаба
                _ctx.view.CreateUIPreview(_ctx.item.Icon, _ctx.canvas);
            }
        }

        private void HandleDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;

            // Если есть префаб, обновляем 3D превью
            if (_ctx.item.Prefab != null)
            {
                if (TryGetWorldPosition(eventData, out Vector3 worldPosition))
                {
                    // Проверяем, есть ли грядка под курсором
                    Vector3 targetPosition = worldPosition;
                    
                    // Прилипание к грядке работает только для семян (элементов с PlantSettings)
                    // Грядки не должны прилипать к другим грядкам
                    if (_ctx.item.PlantSettings != null && _ctx.findPlotAtPosition != null)
                    {
                        var plot = _ctx.findPlotAtPosition(worldPosition);
                        
                        // Если грядка найдена, прилепляем превью к центру грядки
                        if (plot != null)
                        {
                            targetPosition = plot.WorldPosition;
                        }
                    }
                    
                    _ctx.view.Update3DPreviewPosition(targetPosition);
                }
            }
            else
            {
                // Обновляем UI превью (картинки)
                _ctx.view.UpdateUIPreviewPosition(eventData.position);
            }
        }

        private void HandleEndDrag(PointerEventData eventData)
        {
            if (!_isDragging)
                return;

            Debug.Log($"End drag: {_ctx.item.ItemName}");
            _isDragging = false;

            // Проверяем, можем ли разместить объект
            if (TryGetWorldPosition(eventData, out Vector3 worldPosition))
            {
                if (_ctx.gardenBounds != null && _ctx.gardenBounds.IsWithinBounds(worldPosition))
                {
                    Debug.Log($"Position is within bounds, placing item at {worldPosition}");
                    _ctx.onItemPlaced?.Invoke(_ctx.item, worldPosition);
                    
                }
                else
                {
                    Debug.Log($"Position {worldPosition} is outside garden bounds");
                }
            }

            // Удаляем превью
            DestroyPreviews();
        }

        private bool TryGetWorldPosition(PointerEventData eventData, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            if (_ctx.worldCamera == null)
                return false;

            Ray ray = _ctx.worldCamera.ScreenPointToRay(eventData.position);

            // Создаем плоскость на уровне земли (y = 0)
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                worldPosition = ray.GetPoint(distance);
                return true;
            }

            return false;
        }

        private void DestroyPreviews()
        {
            // Удаляем все превью (UI и 3D)
            _ctx.view.DestroyAllPreviews();
        }

        protected override void OnDispose()
        {
            // Отписываемся от событий View
            if (_ctx.view != null)
            {
                _ctx.view.OnBeginDragEvent -= HandleBeginDrag;
                _ctx.view.OnDragEvent -= HandleDrag;
                _ctx.view.OnEndDragEvent -= HandleEndDrag;
            }

            DestroyPreviews();
        }
    }
}


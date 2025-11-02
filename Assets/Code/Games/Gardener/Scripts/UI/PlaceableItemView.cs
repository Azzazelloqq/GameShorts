using System;
using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.Gardener.Gameplay.Modes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace GameShorts.Gardener.UI
{
    /// <summary>
    /// View для отображения элемента drag-and-drop
    /// Только отображение и передача событий в Presenter
    /// </summary>
    public class PlaceableItemView : BaseMonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _countLabel;
        
        private RectTransform _dragPreviewUI;
        
        // События для Presenter
        public event Action<PointerEventData> OnBeginDragEvent;
        public event Action<PointerEventData> OnDragEvent;
        public event Action<PointerEventData> OnEndDragEvent;
        
        /// <summary>
        /// Устанавливает данные для отображения
        /// </summary>
        public void SetData(PlaceableItem item)
        {
            if (item == null)
                return;
            
            if (_icon != null)
            {
                _icon.sprite = item.Icon;
            }
            
            if (_nameText != null)
            {
                _nameText.text = item.ItemName;
            }
            
            // Показываем счетчик только если count > 1
            if (_countLabel != null)
            {
                if (item.Count > 1)
                {
                    _countLabel.gameObject.SetActive(true);
                    _countLabel.text = item.Count.ToString();
                }
                else
                {
                    _countLabel.gameObject.SetActive(false);
                }
            }
        }
        
        // Обработчики событий Unity - просто передаем в Presenter
        public void OnBeginDrag(PointerEventData eventData)
        {
            OnBeginDragEvent?.Invoke(eventData);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            OnDragEvent?.Invoke(eventData);
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragEvent?.Invoke(eventData);
        }
        
        /// <summary>
        /// Создает UI превью (вызывается Presenter'ом)
        /// </summary>
        public void CreateUIPreview(Sprite icon, Canvas canvas)
        {
            _dragPreviewUI = new GameObject("DragPreviewUI").AddComponent<RectTransform>();
            _dragPreviewUI.SetParent(canvas.transform, false);
            
            var previewImage = _dragPreviewUI.gameObject.AddComponent<Image>();
            previewImage.sprite = icon;
            previewImage.raycastTarget = false;
            
            _dragPreviewUI.sizeDelta = new Vector2(100, 100);
        }
        
        /// <summary>
        /// Обновляет позицию UI превью (вызывается Presenter'ом)
        /// </summary>
        public void UpdateUIPreviewPosition(Vector2 screenPosition)
        {
            if (_dragPreviewUI != null)
            {
                _dragPreviewUI.position = screenPosition;
            }
        }
        
        /// <summary>
        /// Удаляет UI превью (вызывается Presenter'ом)
        /// </summary>
        public void DestroyUIPreview()
        {
            if (_dragPreviewUI != null)
            {
                Destroy(_dragPreviewUI.gameObject);
                _dragPreviewUI = null;
            }
        }
    }
}


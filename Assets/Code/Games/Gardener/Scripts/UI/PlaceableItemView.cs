using System;
using Disposable;
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
    public class PlaceableItemView : MonoBehaviourDisposable, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _countLabel;
        
        private RectTransform _dragPreviewUI;
        private GameObject _dragPreview3D;
        private Material[] _originalMaterials;
        private Material[] _transparentMaterials;
        
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
            
            // Устанавливаем слой UI для превью объекта
            _dragPreviewUI.gameObject.layer = LayerMask.NameToLayer("UI");
            
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
        
        /// <summary>
        /// Создает 3D превью из префаба с 50% прозрачностью
        /// </summary>
        public void Create3DPreview(GameObject prefab, Vector3 worldPosition)
        {
            if (prefab == null)
            {
                Debug.LogWarning("Cannot create 3D preview - prefab is null");
                return;
            }
            
            // Создаем экземпляр префаба
            _dragPreview3D = Instantiate(prefab);
            _dragPreview3D.name = "DragPreview3D";
            _dragPreview3D.transform.position = worldPosition;
            
            // Отключаем коллайдеры у превью
            var colliders = _dragPreview3D.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
            
            // Устанавливаем прозрачность 50% для всех материалов
            SetPreviewTransparency(0.5f);
        }
        
        /// <summary>
        /// Обновляет позицию 3D превью
        /// </summary>
        public void Update3DPreviewPosition(Vector3 worldPosition)
        {
            if (_dragPreview3D != null)
            {
                _dragPreview3D.transform.position = worldPosition;
            }
        }
        
        /// <summary>
        /// Устанавливает прозрачность для всех материалов превью
        /// </summary>
        private void SetPreviewTransparency(float alpha)
        {
            if (_dragPreview3D == null)
                return;
            
            var renderers = _dragPreview3D.GetComponentsInChildren<Renderer>();
            
            foreach (var renderer in renderers)
            {
                // Создаем копии материалов для изменения прозрачности
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    
                    // Включаем режим прозрачности
                    material.SetFloat("_Mode", 3); // Transparent mode
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = 3000;
                    
                    // Устанавливаем альфа-канал
                    var color = material.color;
                    color.a = alpha;
                    material.color = color;
                }
                
                renderer.materials = materials;
            }
        }
        
        /// <summary>
        /// Удаляет 3D превью
        /// </summary>
        public void Destroy3DPreview()
        {
            if (_dragPreview3D != null)
            {
                Destroy(_dragPreview3D);
                _dragPreview3D = null;
            }
        }
        
        /// <summary>
        /// Удаляет все превью (UI и 3D)
        /// </summary>
        public void DestroyAllPreviews()
        {
            DestroyUIPreview();
            Destroy3DPreview();
        }
    }
}


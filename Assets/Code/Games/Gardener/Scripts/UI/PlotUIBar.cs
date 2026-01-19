using Disposable;
using UnityEngine;
using UnityEngine.UI;

namespace GameShorts.Gardener.UI
{
/// <summary>
/// UI бар, отображающий прогресс роста и уровень воды над грядкой
/// Позиционируется в screen-space через WorldToScreenPoint
/// </summary>
internal class PlotUIBar : MonoBehaviourDisposable
{
	[SerializeField]
	private Slider _growthProgressBar;

	[SerializeField]
	private Slider _waterLevelBar;

	[SerializeField]
	private Vector2 _screenOffset = new(0, 50);

	[SerializeField]
	private CanvasGroup _canvasGroup;

	private RectTransform _rectTransform;
	private Camera _worldCamera;
	private Transform _targetPlot;
	private bool _isInitialized;
	private Canvas _parentCanvas;
	private bool _shouldBeVisible = true; // Флаг желаемой видимости (контролируется извне)

	private void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
		if (_canvasGroup == null)
		{
			_canvasGroup = GetComponent<CanvasGroup>();
		}
	}

	/// <summary>
	/// Инициализирует бар для конкретной грядки
	/// </summary>
	public void Initialize(Camera camera, Transform plotTransform)
	{
		_worldCamera = camera;
		_targetPlot = plotTransform;
		_isInitialized = true;

		// Получаем родительский Canvas
		_parentCanvas = GetComponentInParent<Canvas>();

		UpdatePosition();
	}

	/// <summary>
	/// Обновляет позицию бара относительно грядки
	/// </summary>
	public void UpdatePosition()
	{
		if (!_isInitialized || _targetPlot == null || _worldCamera == null || _rectTransform == null)
		{
			return;
		}

		var worldPosition = _targetPlot.position;

		// Преобразуем мировую позицию в экранную
		var screenPosition = _worldCamera.WorldToScreenPoint(worldPosition);

		// Если объект за камерой, скрываем UI независимо от флага
		if (screenPosition.z < 0)
		{
			ApplyVisibility(false);
			return;
		}

		// Применяем желаемую видимость (установленную через SetVisible)
		ApplyVisibility(_shouldBeVisible);

		// Для Screen Space - Overlay Canvas используем прямую экранную позицию
		if (_parentCanvas != null && _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
		{
			screenPosition.x += _screenOffset.x;
			screenPosition.y += _screenOffset.y;
			screenPosition.z = 0;

			_rectTransform.position = screenPosition;
		}
		// Для Screen Space - Camera или World Space
		else if (_parentCanvas != null)
		{
			Vector2 localPoint;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				_parentCanvas.transform as RectTransform,
				screenPosition,
				_parentCanvas.worldCamera ?? _worldCamera,
				out localPoint
			);

			localPoint.x += _screenOffset.x;
			localPoint.y += _screenOffset.y;

			_rectTransform.anchoredPosition = localPoint;
		}
	}

	/// <summary>
	/// Устанавливает прогресс роста (0-1)
	/// </summary>
	public void SetGrowthProgress(float progress)
	{
		if (_growthProgressBar != null)
		{
			_growthProgressBar.value = Mathf.Clamp01(progress);
		}
	}

	/// <summary>
	/// Устанавливает уровень воды (0-1)
	/// </summary>
	public void SetWaterLevel(float level)
	{
		if (_waterLevelBar != null)
		{
			_waterLevelBar.value = Mathf.Clamp01(level);
		}
	}

	/// <summary>
	/// Показывает или скрывает бар (устанавливает желаемое состояние видимости)
	/// </summary>
	public void SetVisible(bool visible)
	{
		_shouldBeVisible = visible;
		ApplyVisibility(visible);
	}

	/// <summary>
	/// Применяет фактическую видимость к UI элементу
	/// </summary>
	private void ApplyVisibility(bool visible)
	{
		if (_canvasGroup != null)
		{
			_canvasGroup.alpha = visible ? 1 : 0;
			// Также отключаем взаимодействие и блокируем raycast когда скрыт
			_canvasGroup.interactable = visible;
			_canvasGroup.blocksRaycasts = visible;
		}
		else
		{
			gameObject.SetActive(visible);
		}
	}
}
}
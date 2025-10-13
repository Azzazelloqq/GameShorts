using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Code.Games.AngryHumans
{
internal class LaunchController : MonoBehaviour
{
	[Header("Swipe Throw Settings")]
	[SerializeField]
	[Tooltip("Множитель силы броска от скорости свайпа")]
	private float _swipeForceMultiplier = 0.05f;

	[SerializeField]
	[Tooltip("Максимальная сила броска")]
	private float _maxLaunchForce = 30f;

	[SerializeField]
	[Tooltip("Минимальная скорость экранного свайпа для броска (пиксели/секунду)")]
	private float _minSwipeSpeed = 100f;

	[SerializeField]
	[Range(0f, 1f)]
	[Tooltip("Баланс между высотой и глубиной: 0 = чисто вперед, 1 = чисто вверх")]
	private float _verticalToForwardRatio = 0.5f;

	[SerializeField]
	[Tooltip("Базовая сила броска вперед (независимо от свайпа)")]
	private float _baseForwardForce = 15f;

	[Header("Swipe Tracking")]
	[SerializeField]
	[Tooltip("Количество последних позиций для расчета скорости свайпа")]
	private int _velocitySamples = 5;

	[SerializeField]
	[Tooltip("Время в секундах для расчета средней скорости свайпа")]
	private float _velocityTimeWindow = 0.1f;

	[SerializeField]
	private Camera _gameCamera;

	[Header("References")]
	[SerializeField]
	private LaunchPlatform _launchPlatform;

	private Human _currentHuman;
	private Transform _grabbedPoint;
	private Rigidbody _grabbedRigidbody;
	private Vector3 _targetDragPosition;
	private bool _isDragging = false;
	
	// Для отслеживания свайпа
	private List<Vector3> _swipePositions = new List<Vector3>();
	private List<float> _swipeTimes = new List<float>();
	private Vector3 _lastSwipeVelocity;
	
	// Для отслеживания экранного свайпа
	private List<Vector2> _screenSwipePositions = new List<Vector2>();
	private List<float> _screenSwipeTimes = new List<float>();
	
	[Header("Drag Physics")]
	[SerializeField]
	[Tooltip("Сила притяжения при перетаскивании")]
	private float _dragForce = 500f;

	[SerializeField]
	[Tooltip("Демпфирование при перетаскивании")]
	private float _dragDamping = 10f;

	private void Awake()
	{
		if (_gameCamera == null)
		{
			_gameCamera = Camera.main;
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (_launchPlatform == null)
		{
			return;
		}

		_currentHuman = _launchPlatform.GetCurrentHuman();

		if (_currentHuman == null || _currentHuman.IsLaunched)
		{
			return;
		}

		var worldPosition = GetWorldPosition(eventData.position);
		_grabbedPoint = _currentHuman.GetNearestGrabPoint(worldPosition);
		
		// Ищем Rigidbody на точке хватания или в родителях
		_grabbedRigidbody = _grabbedPoint.GetComponent<Rigidbody>();
		if (_grabbedRigidbody == null)
		{
			_grabbedRigidbody = _grabbedPoint.GetComponentInParent<Rigidbody>();
		}

		if (_grabbedRigidbody != null)
		{
			Debug.Log($"Grabbed rigidbody: {_grabbedRigidbody.name}, mass: {_grabbedRigidbody.mass}, isKinematic: {_grabbedRigidbody.isKinematic}");
		}
		else
		{
			Debug.LogWarning($"No Rigidbody found on grabbed point: {_grabbedPoint.name}");
		}

		_currentHuman.OnGrabbed(_grabbedPoint);

		_isDragging = true;
		_targetDragPosition = worldPosition;
		
		// Инициализируем отслеживание свайпа (мировые координаты для перетаскивания)
		_swipePositions.Clear();
		_swipeTimes.Clear();
		_swipePositions.Add(worldPosition);
		_swipeTimes.Add(Time.time);
		
		// Инициализируем отслеживание экранного свайпа (для расчета направления броска)
		_screenSwipePositions.Clear();
		_screenSwipeTimes.Clear();
		_screenSwipePositions.Add(eventData.position);
		_screenSwipeTimes.Add(Time.time);
		
		_lastSwipeVelocity = Vector3.zero;
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!_isDragging || _currentHuman == null || _grabbedPoint == null)
		{
			return;
		}

		var worldPosition = GetWorldPosition(eventData.position);
		var currentTime = Time.time;
		
		// Обновляем целевую позицию для физического перетаскивания
		_targetDragPosition = worldPosition;
		
		// Добавляем новую позицию в историю свайпа (мировые координаты)
		_swipePositions.Add(worldPosition);
		_swipeTimes.Add(currentTime);
		
		// Ограничиваем количество сохраненных позиций
		while (_swipePositions.Count > _velocitySamples)
		{
			_swipePositions.RemoveAt(0);
			_swipeTimes.RemoveAt(0);
		}
		
		// Добавляем экранную позицию для расчета направления броска
		_screenSwipePositions.Add(eventData.position);
		_screenSwipeTimes.Add(currentTime);
		
		while (_screenSwipePositions.Count > _velocitySamples)
		{
			_screenSwipePositions.RemoveAt(0);
			_screenSwipeTimes.RemoveAt(0);
		}
		
		// Визуализация направления для отладки
		if (_swipePositions.Count > 1)
		{
			var startPos = _swipePositions[0];
			var endPos = _swipePositions[_swipePositions.Count - 1];
			Debug.DrawLine(startPos, endPos, Color.yellow, 0.1f);
		}
	}

	private void FixedUpdate()
	{
		if (!_isDragging || _grabbedRigidbody == null)
		{
			return;
		}

		// Тянем точку хватания к целевой позиции через физику
		var currentPosition = _grabbedRigidbody.position;
		var direction = _targetDragPosition - currentPosition;
		var distance = direction.magnitude;

		if (distance > 0.01f)
		{
			// Применяем силу как пружину
			var force = direction.normalized * (_dragForce * distance);
			_grabbedRigidbody.AddForce(force, ForceMode.Force);

			// Добавляем демпфирование (только во время перетаскивания, не влияет на бросок)
			_grabbedRigidbody.linearVelocity *= (1f - _dragDamping * Time.fixedDeltaTime);
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (!_isDragging || _currentHuman == null || _grabbedPoint == null)
		{
			return;
		}

		_isDragging = false;

		// Проверяем минимальную дельту свайпа в экранных координатах
		if (_screenSwipePositions.Count < 2)
		{
			Debug.Log("No swipe detected");
			_grabbedPoint = null;
			_grabbedRigidbody = null;
			_currentHuman = null;
			return;
		}
		
		var startScreenPos = _screenSwipePositions[0];
		var endScreenPos = _screenSwipePositions[_screenSwipePositions.Count - 1];
		var screenDelta = endScreenPos - startScreenPos;
		var deltaTime = _screenSwipeTimes[_screenSwipeTimes.Count - 1] - _screenSwipeTimes[0];
		var screenSpeed = screenDelta.magnitude / Mathf.Max(deltaTime, 0.001f);
		
		// Проверяем минимальную скорость свайпа (в пикселях в секунду)
		if (screenSpeed < _minSwipeSpeed)
		{
			Debug.Log($"Swipe too slow ({screenSpeed:F0}px/s), not launching. Minimum: {_minSwipeSpeed}px/s");
			_grabbedPoint = null;
			_grabbedRigidbody = null;
			_currentHuman = null;
			return;
		}

		// Вычисляем финальную скорость свайпа из экранных координат
		var swipeVelocity = CalculateSwipeVelocityFromScreen();

		// Обнуляем скорость перед броском, чтобы не было остаточной скорости от перетаскивания
		if (_grabbedRigidbody != null)
		{
			_grabbedRigidbody.linearVelocity = Vector3.zero;
			_grabbedRigidbody.angularVelocity = Vector3.zero;
		}

		var launchVelocity = CalculateLaunchVelocity(swipeVelocity);
		
		// Отладочная информация
		Debug.Log($"=== LAUNCH ===");
		Debug.Log($"Screen: {screenDelta} ({screenSpeed:F0}px/s)");
		Debug.Log($"Launch: speed={launchVelocity.magnitude:F1}, dir={launchVelocity.normalized}");
		Debug.Log($"  X(right)={launchVelocity.x:F1}, Y(up)={launchVelocity.y:F1}, Z(forward)={launchVelocity.z:F1}");
		
		// Визуализация направления броска
		var launchStartPos = _grabbedPoint.position;
		Debug.DrawRay(launchStartPos, launchVelocity * 0.5f, Color.red, 5f);          // Полный вектор броска (красный)
		Debug.DrawRay(launchStartPos, Vector3.up * launchVelocity.y * 0.5f, Color.green, 5f);  // Вертикальная компонента (зеленый)
		Debug.DrawRay(launchStartPos, new Vector3(launchVelocity.x, 0, launchVelocity.z) * 0.5f, Color.blue, 5f);  // Горизонтальная компонента (синий)
		
		_currentHuman.Launch(_grabbedPoint, launchVelocity);

		_grabbedPoint = null;
		_grabbedRigidbody = null;
		_currentHuman = null;
	}

	/// <summary>
	/// Вычисляет скорость свайпа на основе последних позиций
	/// </summary>
	private Vector3 CalculateSwipeVelocity()
	{
		if (_swipePositions.Count < 2)
		{
			return Vector3.zero;
		}
		
		// Берем позиции только в пределах временного окна
		var currentTime = Time.time;
		var startIndex = 0;
		
		for (var i = _swipeTimes.Count - 1; i >= 0; i--)
		{
			if (currentTime - _swipeTimes[i] <= _velocityTimeWindow)
			{
				startIndex = i;
			}
			else
			{
				break;
			}
		}
		
		if (startIndex >= _swipePositions.Count - 1)
		{
			return Vector3.zero;
		}
		
		// Вычисляем среднюю скорость
		var startPosition = _swipePositions[startIndex];
		var endPosition = _swipePositions[_swipePositions.Count - 1];
		var deltaTime = currentTime - _swipeTimes[startIndex];
		
		if (deltaTime < 0.001f)
		{
			return Vector3.zero;
		}
		
		var velocity = (endPosition - startPosition) / deltaTime;
		return velocity;
	}

	/// <summary>
	/// Вычисляет скорость свайпа из экранных координат и преобразует в мировое направление
	/// </summary>
	private Vector3 CalculateSwipeVelocityFromScreen()
	{
		if (_screenSwipePositions.Count < 2)
		{
			return Vector3.zero;
		}
		
		// Берем первую и последнюю позицию свайпа
		var startScreenPos = _screenSwipePositions[0];
		var endScreenPos = _screenSwipePositions[_screenSwipePositions.Count - 1];
		var screenDelta = endScreenPos - startScreenPos;
		
		var startTime = _screenSwipeTimes[0];
		var endTime = _screenSwipeTimes[_screenSwipeTimes.Count - 1];
		var deltaTime = endTime - startTime;
		
		if (deltaTime < 0.001f || screenDelta.magnitude < 1f)
		{
			return Vector3.zero;
		}
		
		// Скорость свайпа в пикселях в секунду
		var screenVelocity = screenDelta / deltaTime;
		
		// Получаем векторы камеры
		var cameraRight = _gameCamera.transform.right;
		var cameraUp = _gameCamera.transform.up;
		var cameraForward = _gameCamera.transform.forward;
		
		// Горизонтальное направление камеры (без наклона)
		var horizontalForward = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
		
		// Нормализуем направление свайпа, сохраняя его величину
		var swipeMagnitude = screenVelocity.magnitude;
		var swipeDirection = screenVelocity.normalized;
		
		// Преобразуем нормализованное направление в мировые координаты
		// X экрана → влево/вправо (camera.right)
		var rightComponent = cameraRight * swipeDirection.x;
		
		// Y экрана → комбинация вверх и вперед в зависимости от _verticalToForwardRatio
		var verticalInput = swipeDirection.y;
		
		// Вертикальная компонента (вверх в мире)
		var upComponent = Vector3.up * verticalInput * _verticalToForwardRatio;
		
		// Компонента вперед (горизонтальное направление камеры)
		var forwardComponent = horizontalForward * verticalInput * (1f - _verticalToForwardRatio);
		
		// Суммируем компоненты направления и умножаем на величину свайпа
		var worldDirection = (rightComponent + upComponent + forwardComponent).normalized;
		var worldVelocity = worldDirection * swipeMagnitude;
		
		// Добавляем базовую силу вперед (независимо от направления свайпа)
		worldVelocity += horizontalForward * _baseForwardForce;
		
		Debug.Log($"Screen swipe: {screenDelta} = X:{screenVelocity.x:F0} Y:{screenVelocity.y:F0} px/s");
		Debug.Log($"World velocity: {worldVelocity}, mag: {worldVelocity.magnitude:F1}");
		Debug.Log($"Ratio: vertical={_verticalToForwardRatio:F2}, forward={1f - _verticalToForwardRatio:F2}");
		
		return worldVelocity;
	}

	/// <summary>
	/// Преобразует скорость свайпа в скорость броска
	/// </summary>
	private Vector3 CalculateLaunchVelocity(Vector3 swipeVelocity)
	{
		// Просто применяем множитель - никаких ограничений!
		var velocity = swipeVelocity * _swipeForceMultiplier;
		
		// Ограничиваем только максимальную силу
		if (velocity.magnitude > _maxLaunchForce)
		{
			velocity = velocity.normalized * _maxLaunchForce;
		}
		
		return velocity;
	}

	private Vector3 GetWorldPosition(Vector2 screenPosition)
	{
		// Для корректного определения направления свайпа используем фиксированное расстояние от камеры
		// Это гарантирует, что движение пальца по экрану напрямую соответствует направлению в 3D пространстве
		var platformPosition = _launchPlatform != null ? _launchPlatform.GetSpawnPosition() : Vector3.zero;
		
		// Вычисляем расстояние от камеры до платформы
		var distanceFromCamera = Vector3.Distance(_gameCamera.transform.position, platformPosition);
		
		// Преобразуем экранную позицию в мировую на том же расстоянии от камеры
		var screenPoint = new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera);
		var worldPosition = _gameCamera.ScreenToWorldPoint(screenPoint);
		
		return worldPosition;
	}

	public void Reset()
	{
		_isDragging = false;
		_grabbedPoint = null;
		_grabbedRigidbody = null;
		_currentHuman = null;
		
		_swipePositions.Clear();
		_swipeTimes.Clear();
		_screenSwipePositions.Clear();
		_screenSwipeTimes.Clear();
		_lastSwipeVelocity = Vector3.zero;
	}
}
}
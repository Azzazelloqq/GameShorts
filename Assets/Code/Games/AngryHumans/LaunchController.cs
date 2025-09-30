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
	private float _swipeForceMultiplier = 2f;

	[SerializeField]
	[Tooltip("Максимальная сила броска")]
	private float _maxLaunchForce = 30f;

	[SerializeField]
	[Tooltip("Минимальная скорость свайпа для броска (в мировых единицах/секунду)")]
	private float _minSwipeSpeed = 1f;

	[SerializeField]
	[Tooltip("Максимальный угол запуска в градусах (0 = горизонтально, 90 = вертикально)")]
	private float _maxLaunchAngle = 60f;

	[SerializeField]
	[Tooltip("Максимальное отклонение влево/вправо в градусах")]
	private float _maxHorizontalDeviation = 35f;

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
		
		// Инициализируем отслеживание свайпа
		_swipePositions.Clear();
		_swipeTimes.Clear();
		_swipePositions.Add(worldPosition);
		_swipeTimes.Add(Time.time);
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
		
		// Добавляем новую позицию в историю свайпа
		_swipePositions.Add(worldPosition);
		_swipeTimes.Add(currentTime);
		
		// Ограничиваем количество сохраненных позиций
		while (_swipePositions.Count > _velocitySamples)
		{
			_swipePositions.RemoveAt(0);
			_swipeTimes.RemoveAt(0);
		}
		
		// Вычисляем текущую скорость свайпа
		_lastSwipeVelocity = CalculateSwipeVelocity();
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

		// Вычисляем финальную скорость свайпа
		var swipeVelocity = CalculateSwipeVelocity();
		
		// Проверяем минимальную скорость свайпа
		if (swipeVelocity.magnitude < _minSwipeSpeed)
		{
			Debug.Log($"Swipe too slow ({swipeVelocity.magnitude:F2}), not launching. Minimum: {_minSwipeSpeed}");
			
			// Просто отпускаем человечка - он остается с текущей физикой
			_grabbedPoint = null;
			_grabbedRigidbody = null;
			_currentHuman = null;
			return;
		}

		// Обнуляем скорость перед броском, чтобы не было остаточной скорости от перетаскивания
		if (_grabbedRigidbody != null)
		{
			_grabbedRigidbody.linearVelocity = Vector3.zero;
			_grabbedRigidbody.angularVelocity = Vector3.zero;
		}

		var launchVelocity = CalculateLaunchVelocity(swipeVelocity);
		Debug.Log($"Swipe velocity: {swipeVelocity.magnitude:F1} -> Launch velocity: {launchVelocity.magnitude:F1}, direction: {launchVelocity.normalized}");
		
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
	/// Преобразует скорость свайпа в скорость броска с учетом ограничений
	/// </summary>
	private Vector3 CalculateLaunchVelocity(Vector3 swipeVelocity)
	{
		// Применяем множитель силы
		var velocity = swipeVelocity * _swipeForceMultiplier;
		
		// Ограничиваем максимальную силу броска
		if (velocity.magnitude > _maxLaunchForce)
		{
			velocity = velocity.normalized * _maxLaunchForce;
		}
		
		// Горизонтальная проекция скорости (на плоскость XZ)
		var horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
		var horizontalSpeed = horizontalVelocity.magnitude;
		
		if (horizontalSpeed > 0.01f)
		{
			// 1. Ограничиваем вертикальный угол
			var verticalAngle = Mathf.Atan2(velocity.y, horizontalSpeed) * Mathf.Rad2Deg;
			
			if (verticalAngle > _maxLaunchAngle)
			{
				var angleRad = _maxLaunchAngle * Mathf.Deg2Rad;
				var totalSpeed = velocity.magnitude;
				var newHorizontalSpeed = totalSpeed * Mathf.Cos(angleRad);
				var newVerticalSpeed = totalSpeed * Mathf.Sin(angleRad);
				
				// Масштабируем горизонтальные компоненты
				var scale = newHorizontalSpeed / horizontalSpeed;
				velocity.x *= scale;
				velocity.z *= scale;
				velocity.y = newVerticalSpeed;
				
				horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
				horizontalSpeed = newHorizontalSpeed;
			}
			
			// 2. Ограничиваем горизонтальное отклонение
			// Базовое направление "вперед" - это направление от камеры в горизонтальной плоскости
			var cameraForward = _gameCamera.transform.forward;
			var forwardHorizontal = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
			
			if (forwardHorizontal.magnitude > 0.1f)
			{
				var currentHorizontalDir = horizontalVelocity.normalized;
				var angleFromForward = Vector3.SignedAngle(forwardHorizontal, currentHorizontalDir, Vector3.up);
				
				// Ограничиваем угол
				if (Mathf.Abs(angleFromForward) > _maxHorizontalDeviation)
				{
					var clampedAngle = Mathf.Clamp(angleFromForward, -_maxHorizontalDeviation, _maxHorizontalDeviation);
					var limitedDir = Quaternion.AngleAxis(clampedAngle, Vector3.up) * forwardHorizontal;
					
					velocity.x = limitedDir.x * horizontalSpeed;
					velocity.z = limitedDir.z * horizontalSpeed;
				}
			}
		}
		
		return velocity;
	}

	private Vector3 GetWorldPosition(Vector2 screenPosition)
	{
		// Создаем плоскость, параллельную виду камеры, проходящую через позицию платформы
		var platformPosition = _launchPlatform != null ? _launchPlatform.GetSpawnPosition() : Vector3.zero;
		var plane = new Plane(-_gameCamera.transform.forward, platformPosition);
		var ray = _gameCamera.ScreenPointToRay(screenPosition);
		
		if (plane.Raycast(ray, out var distance))
		{
			return ray.GetPoint(distance);
		}
		
		// Fallback if raycast fails
		var worldPosition = _gameCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
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
		_lastSwipeVelocity = Vector3.zero;
	}
}
}
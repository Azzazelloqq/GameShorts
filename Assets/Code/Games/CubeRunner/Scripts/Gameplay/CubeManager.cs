using Code.Core.Tools.Pool;
using Disposable;
using GameShorts.CubeRunner.Data;
using GameShorts.CubeRunner.View;
using LightDI.Runtime;
using TickHandler;
using UnityEngine;

namespace GameShorts.CubeRunner.Gameplay
{
internal class CubeManager : DisposableBase
{
	private readonly Ctx _ctx;
	private readonly CubeRunnerGameSettings _settings;
	private readonly IPoolManager _poolManager;
	private CubeView _spawnedCubeView;
	private readonly ITickHandler _tickHandler;
	private Vector2Int _currentGridPosition;
	private bool _isRotating;
	private Vector3 _startPos;
	private Quaternion _preRotation;
	private Quaternion _postRotation;
	private float _rotationTime;
	private Vector3 _scale;
	private float _radius;
	private float _startAngleRad;
	private float _duration = 0.3f;
	private Vector2Int _currentDirection;
	private bool _isEnable = true;
	private Vector3 _spawnPosition;

	public CubeView CurrentCubeView => _spawnedCubeView;

	public bool IsRotating => _isRotating;

	public struct Ctx
	{
		public CubeRunnerSceneContextView sceneContextView;
	}

	public CubeManager(Ctx ctx, [Inject] IPoolManager poolManager, [Inject] ITickHandler tickHandler)
	{
		_ctx = ctx;
		_settings = _ctx.sceneContextView.GameSettings;
		_poolManager = poolManager;
		_tickHandler = tickHandler;
	}

	private void OnPhysicUpdate(float deltaTime)
	{
		if (_isRotating)
		{
			_rotationTime += deltaTime;
			var ratio = Mathf.Lerp(0, 1, _rotationTime / _duration);

			var rotAng = Mathf.Lerp(0, Mathf.PI / 2f, ratio);
			var distanceX = _currentDirection.x * _radius *
							(Mathf.Cos(_startAngleRad) - Mathf.Cos(_startAngleRad + rotAng));
			var distanceY = _radius * (Mathf.Sin(_startAngleRad + rotAng) - Mathf.Sin(_startAngleRad));
			var distanceZ = _currentDirection.y * _radius *
							(Mathf.Cos(_startAngleRad) - Mathf.Cos(_startAngleRad + rotAng));
			_spawnedCubeView.VisualRoot.position = new Vector3(_startPos.x + distanceX, _startPos.y + distanceY,
				_startPos.z + distanceZ);

			_spawnedCubeView.VisualRoot.rotation = Quaternion.Lerp(_preRotation, _postRotation, ratio);

			if (Mathf.Approximately(ratio, 1))
			{
				_currentDirection = Vector2Int.zero;
				_isRotating = false;
				_rotationTime = 0;
			}
		}
	}

	protected override void OnDispose()
	{
		_tickHandler.PhysicUpdate -= OnPhysicUpdate;
		if (_spawnedCubeView != null)
		{
			_poolManager.Return(_settings.CubePrefab, _spawnedCubeView.gameObject);
		}

		_spawnedCubeView = null;
		base.OnDispose();
	}

	public CubeView SpawnCube(Vector3 scale, Vector3 position)
	{
		CubeView cubeView = null;
		var parent = _ctx.sceneContextView.WorldRoot != null
			? _ctx.sceneContextView.WorldRoot
			: _ctx.sceneContextView.transform;

		var cubePrefabObject = _settings?.CubePrefab != null
			? _settings.CubePrefab.gameObject
			: null;

		if (cubePrefabObject != null)
		{
			var spawnPos = Vector3.zero;
			spawnPos.y += _settings.SpawnHeight;
			spawnPos.x = 0.5f * (scale.x - 1);
			spawnPos.z = 0.5f * (scale.z - 1);
			var cubeObject = _poolManager.Get(cubePrefabObject, spawnPos, parent, Quaternion.identity);
			if (cubeObject != null)
			{
				cubeView = cubeObject.GetComponent<CubeView>();
				if (cubeView == null)
				{
					cubeView = cubeObject.AddComponent<CubeView>();
				}

				cubeView.SetCtx(new CubeView.Ctx
				{
					scale = scale
				});
				cubeView.Rigidbody.freezeRotation = true;
				cubeView.Collider.isTrigger = false;

				cubeView.Rigidbody.linearVelocity = new Vector3(0, -10, 0);
				cubeObject.SetActive(true);
			}
		}

		if (cubeView == null)
		{
			Debug.LogError("CubeRunnerGameplayPm: Cube prefab is missing or pool did not provide an instance.");
			return null;
		}

		cubeView.SetCubeDimensions(scale);

		_spawnPosition = position;
		_scale = scale; //cubeView.VisualRoot.lossyScale;
		_spawnedCubeView = cubeView;
		_isEnable = true;
		_tickHandler.PhysicUpdate += OnPhysicUpdate;
		return cubeView;
	}

	public void TryMove(Vector2Int direction)
	{
		if (!_isEnable)
		{
			return;
		}

		if (direction.sqrMagnitude > 0 && !_isRotating && _spawnedCubeView.IsGrounded)
		{
			_startPos = _spawnedCubeView.VisualRoot.position;
			_preRotation = _spawnedCubeView.VisualRoot.rotation;
			_spawnedCubeView.VisualRoot.Rotate(direction.y * 90, 0, -direction.x * 90, Space.World);
			_postRotation = _spawnedCubeView.VisualRoot.rotation;
			_spawnedCubeView.VisualRoot.rotation = _preRotation;
			SetRadius(direction);
			_rotationTime = 0;
			_isRotating = true;
			_currentDirection = direction;
		}
	}

	private void SetRadius(Vector2Int direction)
	{
		const float threshold = 0.99f;

		var moveDir = direction.x != 0
			? Vector3.right
			: direction.y != 0
				? Vector3.forward
				: Vector3.zero;

		if (moveDir == Vector3.zero)
		{
			return;
		}

		var cashTransform = _spawnedCubeView.VisualRoot;

		(Vector3 axisMove, Vector3 axisUp, float a, float b)?[] rules =
		{
			// движение по X
			(cashTransform.right, cashTransform.up, _scale.x, _scale.y),
			(cashTransform.right, cashTransform.forward, _scale.x, _scale.z),

			// движение по Y
			(cashTransform.up, cashTransform.right, _scale.y, _scale.x),
			(cashTransform.up, cashTransform.forward, _scale.y, _scale.z),

			// движение по Z
			(cashTransform.forward, cashTransform.right, _scale.z, _scale.x),
			(cashTransform.forward, cashTransform.up, _scale.z, _scale.y)
		};

		foreach (var r in rules)
		{
			if (Mathf.Abs(Vector3.Dot(r.Value.axisMove, moveDir)) > threshold &&
				Mathf.Abs(Vector3.Dot(r.Value.axisUp, Vector3.up)) > threshold)
			{
				var a = r.Value.a * 0.5f;
				var b = r.Value.b * 0.5f;

				_radius = Mathf.Sqrt(a * a + b * b);
				_startAngleRad = Mathf.Atan2(r.Value.b, r.Value.a);
				return;
			}
		}
	}

	public void DisableControl()
	{
		_isEnable = false;
	}

	public void ClearCube()
	{
		_tickHandler.PhysicUpdate -= OnPhysicUpdate;

		if (_spawnedCubeView != null)
		{
			_spawnedCubeView.Rigidbody.angularVelocity = Vector3.zero;
			_spawnedCubeView.Rigidbody.linearVelocity = Vector3.zero;
			_poolManager.Return(_settings.CubePrefab, _spawnedCubeView.gameObject);
		}

		_spawnedCubeView = null;
	}

	public void RespawnCube()
	{
		ClearCube();
		SpawnCube(_scale, _spawnPosition);
	}
}
}
using UnityEngine;

namespace Code.Games.AngryHumans
{
/// <summary>
/// Handles input via raycast and forwards it to LaunchController
/// Can be enabled/disabled to control input
/// </summary>
internal class InputHandler : MonoBehaviour
{
	[SerializeField]
	private LaunchController _launchController;

	[SerializeField]
	private Camera _camera;

	private bool _isPointerDown;

	private void Awake()
	{
		if (_camera == null)
		{
			_camera = Camera.main;
		}
	}

	private void Update()
	{
		if (_launchController == null)
		{
			return;
		}

		var inputPosition = GetInputPosition(out var hasInput);

		if (!hasInput)
		{
			if (_isPointerDown)
			{
				_isPointerDown = false;
				_launchController.OnPointerUp(inputPosition);
			}

			return;
		}

		if (IsPointerDown())
		{
			if (!_isPointerDown)
			{
				_isPointerDown = true;
				_launchController.OnPointerDown(inputPosition);
			}
			else
			{
				_launchController.OnDrag(inputPosition);
			}
		}
		else if (_isPointerDown)
		{
			_isPointerDown = false;
			_launchController.OnPointerUp(inputPosition);
		}
	}

	private Vector2 GetInputPosition(out bool hasInput)
	{
#if UNITY_EDITOR || UNITY_STANDALONE
		hasInput = true;
		return Input.mousePosition;
#else
		if (Input.touchCount > 0)
		{
			hasInput = true;
			return Input.GetTouch(0).position;
		}

		hasInput = false;
		return Vector2.zero;
#endif
	}

	private bool IsPointerDown()
	{
#if UNITY_EDITOR || UNITY_STANDALONE
		return Input.GetMouseButton(0);
#else
		if (Input.touchCount > 0)
		{
			var phase = Input.GetTouch(0).phase;
			return phase == TouchPhase.Began || phase == TouchPhase.Moved || phase == TouchPhase.Stationary;
		}

		return false;
#endif
	}

	private void OnDisable()
	{
		if (_isPointerDown && _launchController != null)
		{
			_isPointerDown = false;
			_launchController.OnPointerUp(Vector2.zero);
		}
	}
}
}

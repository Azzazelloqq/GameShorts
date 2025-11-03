using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.GameSwiper.InputHandlers
{
/// <summary>
/// Creates a gooey/metaball effect between two circles (joystick circles).
/// The circles appear to be connected by a viscous liquid when moved.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class GooeyJoystickEffect : MonoBehaviour
{
	[Header("Circle References")]
	[SerializeField]
	[Tooltip("The outer (larger) circle of the joystick")]
	private RectTransform _outerCircle;

	[SerializeField]
	[Tooltip("The inner (smaller, draggable) circle of the joystick")]
	private RectTransform _innerCircle;

	[Header("Effect Settings")]
	[SerializeField]
	[Tooltip("How thick the gooey connection is (lower = thicker)")]
	[Range(0.1f, 1f)]
	private float _threshold = 0.5f;

	[SerializeField]
	[Tooltip("How smooth the edges are")]
	[Range(0f, 0.5f)]
	private float _smoothness = 0.1f;

	[SerializeField]
	[Tooltip("Color of the gooey effect")]
	private Color _effectColor = Color.white;

	[SerializeField]
	[Tooltip("Update in real-time (disable for performance)")]
	private bool _updateInRealtime = true;

	private RawImage _rawImage;
	private Material _material;
	private RectTransform _rectTransform;

	// Shader property IDs for performance
	private static readonly int Circle1PosID = Shader.PropertyToID("_Circle1Pos");
	private static readonly int Circle2PosID = Shader.PropertyToID("_Circle2Pos");
	private static readonly int Circle1RadiusID = Shader.PropertyToID("_Circle1Radius");
	private static readonly int Circle2RadiusID = Shader.PropertyToID("_Circle2Radius");
	private static readonly int ThresholdID = Shader.PropertyToID("_Threshold");
	private static readonly int SmoothnessID = Shader.PropertyToID("_Smoothness");
	private static readonly int ColorID = Shader.PropertyToID("_Color");

	private void Awake()
	{
		_rawImage = GetComponent<RawImage>();
		_rectTransform = GetComponent<RectTransform>();

		// Create a unique material instance for this effect
		if (_rawImage.material != null)
		{
			_material = new Material(_rawImage.material);
			_rawImage.material = _material;
		}
		else
		{
			Debug.LogError("GooeyJoystickEffect: RawImage needs a material with GooeyJoystick shader!");
		}
	}

	private void Start()
	{
		// Initial update
		UpdateEffect();
	}

	private void Update()
	{
		if (_updateInRealtime)
		{
			UpdateEffect();
		}
	}

	/// <summary>
	/// Manually update the effect (call this if _updateInRealtime is false)
	/// </summary>
	public void UpdateEffect()
	{
		if (_material == null || _outerCircle == null || _innerCircle == null)
		{
			return;
		}

		// Get the positions in normalized UV space (0-1)
		Vector2 outerPosUV = GetNormalizedPosition(_outerCircle);
		Vector2 innerPosUV = GetNormalizedPosition(_innerCircle);

		// Get radii in normalized space
		float outerRadiusUV = GetNormalizedRadius(_outerCircle);
		float innerRadiusUV = GetNormalizedRadius(_innerCircle);

		// Update shader parameters
		_material.SetVector(Circle1PosID, outerPosUV);
		_material.SetVector(Circle2PosID, innerPosUV);
		_material.SetFloat(Circle1RadiusID, outerRadiusUV);
		_material.SetFloat(Circle2RadiusID, innerRadiusUV);
		_material.SetFloat(ThresholdID, _threshold);
		_material.SetFloat(SmoothnessID, _smoothness);
		_material.SetColor(ColorID, _effectColor);
	}

	/// <summary>
	/// Get position of a RectTransform in normalized UV coordinates (0-1) relative to this effect's rect
	/// </summary>
	private Vector2 GetNormalizedPosition(RectTransform target)
	{
		if (_rectTransform == null || target == null)
		{
			return new Vector2(0.5f, 0.5f);
		}

		// Get world position of the target
		Vector3 worldPos = target.position;

		// Convert to local position relative to this rect
		Vector2 localPos;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_rectTransform,
			RectTransformUtility.WorldToScreenPoint(null, worldPos),
			null,
			out localPos);

		// Get rect dimensions
		Rect rect = _rectTransform.rect;

		// Normalize to 0-1 UV space
		float u = (localPos.x - rect.xMin) / rect.width;
		float v = (localPos.y - rect.yMin) / rect.height;

		return new Vector2(u, v);
	}

	/// <summary>
	/// Get radius of a circle in normalized UV space
	/// </summary>
	private float GetNormalizedRadius(RectTransform circle)
	{
		if (_rectTransform == null || circle == null)
		{
			return 0.1f;
		}

		// Assume circle is square, use width as diameter
		float circleRadius = circle.rect.width * 0.5f;

		// Normalize to UV space (use width of effect rect as reference)
		float normalizedRadius = circleRadius / _rectTransform.rect.width;

		return normalizedRadius;
	}

	/// <summary>
	/// Set the threshold for the gooey effect
	/// </summary>
	public void SetThreshold(float threshold)
	{
		_threshold = Mathf.Clamp01(threshold);
		if (_material != null)
		{
			_material.SetFloat(ThresholdID, _threshold);
		}
	}

	/// <summary>
	/// Set the smoothness of the effect edges
	/// </summary>
	public void SetSmoothness(float smoothness)
	{
		_smoothness = Mathf.Clamp(smoothness, 0f, 0.5f);
		if (_material != null)
		{
			_material.SetFloat(SmoothnessID, _smoothness);
		}
	}

	/// <summary>
	/// Set the color of the effect
	/// </summary>
	public void SetColor(Color color)
	{
		_effectColor = color;
		if (_material != null)
		{
			_material.SetColor(ColorID, color);
		}
	}

	/// <summary>
	/// Enable or disable real-time updates
	/// </summary>
	public void SetRealtimeUpdate(bool enabled)
	{
		_updateInRealtime = enabled;
	}

	private void OnDestroy()
	{
		// Clean up the material instance
		if (_material != null)
		{
			Destroy(_material);
		}
	}

	private void OnValidate()
	{
		// Validate ranges in editor
		_threshold = Mathf.Clamp01(_threshold);
		_smoothness = Mathf.Clamp(_smoothness, 0f, 0.5f);

		// Update effect in editor
		if (Application.isPlaying && _material != null)
		{
			UpdateEffect();
		}
	}

#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		if (_outerCircle == null || _innerCircle == null || _rectTransform == null)
		{
			return;
		}

		// Draw debug visualization
		Gizmos.color = Color.cyan;

		// Draw effect bounds
		Vector3[] corners = new Vector3[4];
		_rectTransform.GetWorldCorners(corners);
		for (int i = 0; i < 4; i++)
		{
			Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
		}

		// Draw lines from circles to effect center
		Gizmos.color = Color.yellow;
		Gizmos.DrawLine(_outerCircle.position, _rectTransform.position);
		Gizmos.DrawLine(_innerCircle.position, _rectTransform.position);
	}
#endif
}
}


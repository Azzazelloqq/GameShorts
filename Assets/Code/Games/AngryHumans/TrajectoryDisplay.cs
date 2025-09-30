using UnityEngine;

namespace Code.Games.AngryHumans
{
internal class TrajectoryDisplay : MonoBehaviour
{
	[SerializeField]
	private LineRenderer _lineRenderer;

	[SerializeField]
	private int _trajectoryPointsCount = 30;

	[SerializeField]
	private float _timeStep = 0.1f;

	private void Awake()
	{
		if (_lineRenderer == null)
		{
			_lineRenderer = GetComponent<LineRenderer>();
		}

		Hide();
	}

	public void ShowTrajectory(Vector3 startPosition, Vector3 velocity)
	{
		_lineRenderer.enabled = true;

		var points = new Vector3[_trajectoryPointsCount];
		var gravity = Physics.gravity;

		for (var i = 0; i < _trajectoryPointsCount; i++)
		{
			var time = i * _timeStep;
			points[i] = CalculatePointPosition(startPosition, velocity, gravity, time);
		}

		_lineRenderer.positionCount = _trajectoryPointsCount;
		_lineRenderer.SetPositions(points);
	}

	private Vector3 CalculatePointPosition(Vector3 startPosition, Vector3 velocity, Vector3 gravity, float time)
	{
		var position = startPosition;
		position += velocity * time;
		position += 0.5f * gravity * time * time;
		return position;
	}

	public void Hide()
	{
		_lineRenderer.enabled = false;
	}
}
}
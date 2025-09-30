using UnityEngine;

namespace Code.Games.AngryHumans
{
internal class LaunchPlatform : MonoBehaviour
{
	[SerializeField]
	private Transform _spawnPoint;

	private Human _currentHuman;

	private void Awake()
	{
		if (_spawnPoint == null)
		{
			_spawnPoint = transform;
		}
	}

	public void PlaceHuman(Human human)
	{
		if (human == null)
		{
			return;
		}

		if (_currentHuman != null && _currentHuman.gameObject != null)
		{
			Destroy(_currentHuman.gameObject);
		}

		_currentHuman = human;
		_currentHuman.transform.position = _spawnPoint.position;
		_currentHuman.transform.rotation = _spawnPoint.rotation;
		_currentHuman.SetOnPlatform(true);
	}

	public Human GetCurrentHuman()
	{
		return _currentHuman;
	}

	public void ClearPlatform()
	{
		if (_currentHuman != null && _currentHuman.gameObject != null)
		{
			Destroy(_currentHuman.gameObject);
		}

		_currentHuman = null;
	}

	public Vector3 GetSpawnPosition()
	{
		return _spawnPoint.position;
	}
}
}
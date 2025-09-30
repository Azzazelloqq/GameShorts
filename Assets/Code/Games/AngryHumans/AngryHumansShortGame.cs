using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Games.AngryHumans
{
public class AngryHumansShortGame : MonoBehaviour, IShortGame3D
{
	[SerializeField]
	private Camera _camera;

	[SerializeField]
	private GraphicRaycaster _graphicRaycaster;

	[Header("Game Components")]
	[SerializeField]
	private HumanFactory _humanFactory;

	[SerializeField]
	private LaunchPlatform _launchPlatform;

	[SerializeField]
	private LaunchController _launchController;

	[SerializeField]
	private InputHandler _inputHandler;

	[SerializeField]
	private TargetManager _targetManager;

	private RenderTexture _renderTexture;
	private bool _isGameActive;
	private bool _isPaused;
	private int _currentScore = 0;

	public bool IsPreloaded { get; private set; }

	public async ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
	{
		_renderTexture = RenderTextureUtils.GetRenderTextureForShortGame(_camera);

		if (_humanFactory != null)
		{
			await _humanFactory.PreloadHumansAsync(cancellationToken);
		}

		if (_targetManager != null)
		{
			await _targetManager.PreloadStructuresAsync(cancellationToken);
		}

		IsPreloaded = true;
	}

	public RenderTexture GetRenderTexture()
	{
		return _renderTexture;
	}

	public void Dispose()
	{
		_renderTexture = null;
	}

	public void StartGame()
	{
		_isGameActive = true;
		_isPaused = false;
		_currentScore = 0;

		// Подписываемся на события целей
		if (_targetManager != null)
		{
			_targetManager.OnScoreChanged += HandleScoreChanged;
			_targetManager.OnAllStructuresCompleted += HandleAllStructuresCompleted;
			_targetManager.ResetScore();
			_targetManager.SpawnLevel();
		}

		SpawnNewHuman();
	}

	public void PauseGame()
	{
		_isPaused = true;
	}

	public void UnpauseGame()
	{
		_isPaused = false;
	}

	public void RestartGame()
	{
		StopGame();
		StartGame();
	}

	public void StopGame()
	{
		_isGameActive = false;
		_isPaused = false;

		// Отписываемся от событий целей
		if (_targetManager != null)
		{
			_targetManager.OnScoreChanged -= HandleScoreChanged;
			_targetManager.OnAllStructuresCompleted -= HandleAllStructuresCompleted;
			_targetManager.ClearAllStructures();
		}

		if (_launchPlatform != null)
		{
			_launchPlatform.ClearPlatform();
		}

		if (_launchController != null)
		{
			_launchController.Reset();
		}
	}

	public void EnableInput()
	{
		_graphicRaycaster.enabled = true;
		
		if (_inputHandler != null)
		{
			_inputHandler.enabled = true;
		}
	}

	public void DisableInput()
	{
		_graphicRaycaster.enabled = false;
		
		if (_inputHandler != null)
		{
			_inputHandler.enabled = false;
		}
	}

	private void SpawnNewHuman()
	{
		if (_humanFactory == null || _launchPlatform == null)
		{
			return;
		}

		if (!_isGameActive)
		{
			return;
		}

		var human = _humanFactory.CreateRandomHuman(transform);

		if (human != null)
		{
			// Подписываемся на событие падения
			human.OnFellBelowPlatform += OnHumanFellBelowPlatform;
			_launchPlatform.PlaceHuman(human);
		}
	}

	private void OnHumanFellBelowPlatform()
	{
		if (_isGameActive && !_isPaused)
		{
			SpawnNewHuman();
		}
	}

	public void OnHumanLanded()
	{
		if (_isGameActive && !_isPaused)
		{
			SpawnNewHuman();
		}
	}

	private void HandleScoreChanged(int newScore)
	{
		_currentScore = newScore;
		Debug.Log($"Score updated: {_currentScore}");
	}

	private void HandleAllStructuresCompleted()
	{
		Debug.Log("All structures completed! Level finished!");
		// Здесь можно добавить логику завершения уровня
		// Например, показать экран победы или загрузить следующий уровень
	}

	public int GetCurrentScore()
	{
		return _currentScore;
	}
}
}
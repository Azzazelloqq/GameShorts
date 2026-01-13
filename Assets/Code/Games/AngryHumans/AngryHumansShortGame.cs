using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Games.AngryHumans
{
public class AngryHumansShortGame : MonoBehaviour, IShortGame3D
{
	[SerializeField]
	private Camera _camera;

	[SerializeField]
	private Camera _uiCamera;

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

	[SerializeField]
	private ScoreController _scoreController;

	[SerializeField]
	private LevelManager _levelManager;

	private RenderTexture _renderTexture;
	private bool _isGameActive;
	private bool _isPaused;
	private bool _isStarting;
	private bool _disposed;
	private int _currentScore = 0;

	public bool IsPreloaded { get; private set; }

	public async UniTask PreloadGameAsync(CancellationToken cancellationToken = default)
	{
		_renderTexture = RenderTextureUtils.GetRenderTextureForShortGame(_camera, _uiCamera);

		if (_humanFactory != null)
		{
			await _humanFactory.PreloadHumansAsync(cancellationToken);
		}

		// Preload current level addressable asset so StartGame doesn't hitch on first load.
		if (_levelManager != null)
		{
			await _levelManager.PreloadCurrentLevelAssetAsync(cancellationToken);
		}

		await LoadLevel();
        
		IsPreloaded = true;
	}

	public RenderTexture GetRenderTexture()
	{
		return _renderTexture;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		// Best-effort: ensure runtime subscriptions/state are cleaned up even if Dispose is called directly.
		StopGame();

		RenderTextureUtils.ReleaseAndDestroy(ref _renderTexture, _camera, _uiCamera);
		IsPreloaded = false;

		Destroy(gameObject);
	}
	
	private void OnDestroy()
	{
		Dispose();
	}

	public async void StartGame()
	{
		if (_isStarting)
		{
			Debug.LogWarning("[AngryHumans] StartGame called while already starting, ignoring.");
			return;
		}
		
		if (_disposed)
		{
			return;
		}

		_isStarting = true;
		_isGameActive = true;
		_isPaused = false;
		_currentScore = 0;

		InitializeScoreController();
		if (_disposed)
		{
			_isStarting = false;
			return;
		}
		SubscribeToTargetEvents();

		SpawnNewHuman();
		_isStarting = false;
	}

	public void Disable()
	{
		gameObject.SetActive(false);
	}

	public void Enable()
	{
		gameObject.SetActive(true);
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

		UnsubscribeFromTargetEvents();
		CleanupScoreController();

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
		if (_inputHandler != null)
		{
			_inputHandler.enabled = true;
		}
	}

	public void DisableInput()
	{
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
			human.OnFellBelowPlatform += OnHumanFellBelowPlatform;
			_launchPlatform.PlaceHuman(human);
		}
	}

	private void OnHumanFellBelowPlatform()
	{
		if (!_isGameActive || _isPaused)
		{
			return;
		}

		if (_scoreController != null)
		{
			_scoreController.UseAttempt();

			if (!_scoreController.IsGameOver)
			{
				SpawnNewHuman();
			}
		}
		else
		{
			SpawnNewHuman();
		}
	}

	private void HandleScoreChanged(int newScore)
	{
		_currentScore = newScore;

		if (_scoreController != null)
		{
			_scoreController.UpdateScore(newScore);
		}
	}

	private void HandleTargetDestroyedForScore(TargetStructure structure, Target target, int scoreValue)
	{
		if (_scoreController != null && target != null)
		{
			_scoreController.OnTargetDestroyed(target, scoreValue);
		}
	}

	private void HandleAllStructuresCompleted()
	{
	}

	private void HandleGameOver()
	{
		_isGameActive = false;

		if (_launchController != null)
		{
			_launchController.Reset();
		}
	}

	private void InitializeScoreController()
	{
		if (_scoreController != null)
		{
			_scoreController.Initialize(_camera);
			_scoreController.ResetScore();
			_scoreController.OnGameOver += HandleGameOver;
			_scoreController.OnRestartRequested += RestartGame;
		}
	}

	private async Task LoadLevel()
	{
		if (_levelManager != null)
		{
			await _levelManager.LoadCurrentLevel();
		}
		else if (_scoreController != null)
		{
			_scoreController.ResetAttempts();
		}
	}

	private void SubscribeToTargetEvents()
	{
		if (_targetManager == null)
		{
			return;
		}

		_targetManager.OnScoreChanged += HandleScoreChanged;
		_targetManager.OnAllStructuresCompleted += HandleAllStructuresCompleted;

		if (_scoreController != null)
		{
			_targetManager.OnTargetDestroyed += HandleTargetDestroyedForScore;
			_targetManager.OnStructureCompleted += _scoreController.OnStructureCompleted;
		}

		_targetManager.ResetScore();
	}

	private void UnsubscribeFromTargetEvents()
	{
		if (_targetManager == null)
		{
			return;
		}

		_targetManager.OnScoreChanged -= HandleScoreChanged;
		_targetManager.OnAllStructuresCompleted -= HandleAllStructuresCompleted;

		if (_scoreController != null)
		{
			_targetManager.OnTargetDestroyed -= HandleTargetDestroyedForScore;
			_targetManager.OnStructureCompleted -= _scoreController.OnStructureCompleted;
		}

		_targetManager.ClearAllStructures();
	}

	private void CleanupScoreController()
	{
		if (_scoreController == null)
		{
			return;
		}

		_scoreController.ResetScore();
		_scoreController.OnGameOver -= HandleGameOver;
		_scoreController.OnRestartRequested -= RestartGame;
	}
}
}
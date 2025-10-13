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
	private Camera _uiCamera;
	
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

	[SerializeField]
	private ScoreController _scoreController;
	
	[SerializeField]
	private LevelManager _levelManager;

	private RenderTexture _renderTexture;
	private bool _isGameActive;
	private bool _isPaused;
	private int _currentScore = 0;

	public bool IsPreloaded { get; private set; }

	public async ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
	{
		_renderTexture = RenderTextureUtils.GetRenderTextureForShortGame(_camera, _uiCamera);
		
		if (_humanFactory != null)
		{
			await _humanFactory.PreloadHumansAsync(cancellationToken);
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

	public async void StartGame()
	{
		_isGameActive = true;
		_isPaused = false;
		_currentScore = 0;

		// Инициализируем контроллер счета
		if (_scoreController != null)
		{
			_scoreController.Initialize(_camera);
			_scoreController.ResetScore();
			
			// Подписываемся на события
			_scoreController.OnGameOver += HandleGameOver;
			_scoreController.OnRestartRequested += RestartGame;
		}
		
		// Если есть LevelManager, загружаем уровень
		if (_levelManager != null)
		{
			await _levelManager.LoadCurrentLevel();
		}
		else
		{
			// Используем старую систему без конфигов
			if (_scoreController != null)
			{
				_scoreController.ResetAttempts();
			}
		}

		// Подписываемся на события целей
		if (_targetManager != null)
		{
			_targetManager.OnScoreChanged += HandleScoreChanged;
			_targetManager.OnAllStructuresCompleted += HandleAllStructuresCompleted;
			
			// Подписываемся на события для отображения очков
			if (_scoreController != null)
			{
				_targetManager.OnTargetDestroyed += HandleTargetDestroyedForScore;
				_targetManager.OnStructureCompleted += _scoreController.OnStructureCompleted;
			}
			
			_targetManager.ResetScore();
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
			
			// Отписываемся от событий для отображения очков
			if (_scoreController != null)
			{
				_targetManager.OnTargetDestroyed -= HandleTargetDestroyedForScore;
				_targetManager.OnStructureCompleted -= _scoreController.OnStructureCompleted;
			}
			
			_targetManager.ClearAllStructures();
		}

		// Сбрасываем контроллер счета
		if (_scoreController != null)
		{
			_scoreController.ResetScore();
			_scoreController.OnGameOver -= HandleGameOver;
			_scoreController.OnRestartRequested -= RestartGame;
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
			// Используем попытку при падении человечка
			if (_scoreController != null)
			{
				_scoreController.UseAttempt();
				
				// Если игра не закончилась, спавним нового человечка
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
	}

	public void OnHumanLanded()
	{
		if (_isGameActive && !_isPaused)
		{
			// При успешной посадке не тратим попытку, просто спавним нового
			if (_scoreController != null && !_scoreController.IsGameOver)
			{
				SpawnNewHuman();
			}
			else if (_scoreController == null)
			{
				SpawnNewHuman();
			}
		}
	}

	private void HandleScoreChanged(int newScore)
	{
		_currentScore = newScore;
		
		// Обновляем отображение счета
		if (_scoreController != null)
		{
			_scoreController.UpdateScore(newScore);
		}
		
		Debug.Log($"Score updated: {_currentScore}");
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
		Debug.Log("All structures completed! Level finished!");
		// Здесь можно добавить логику завершения уровня
		// Например, показать экран победы или загрузить следующий уровень
	}

	private void HandleGameOver()
	{
		Debug.Log("Game Over! No attempts left.");
		_isGameActive = false;
		
		// Останавливаем все активные снаряды
		if (_launchController != null)
		{
			_launchController.Reset();
		}
	}
	
	public int GetCurrentScore()
	{
		return _currentScore;
	}
}
}
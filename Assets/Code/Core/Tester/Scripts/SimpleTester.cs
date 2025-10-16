using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asteroids.Code.Games.Game1;
using Code.Core.ShortGamesCore.EscapeFromDark;
using Code.Core.ShortGamesCore.Game2;
using Code.Core.ShortGamesCore.Lawnmower;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Core.ShortGamesCore.Source.Pool;
using Code.Core.Tools.Pool;
using Code.Games;
using Code.Generated.Addressables;
using GameShorts.FlyHumans;
using InGameLogger;
using LightDI.Runtime;
using Lightseeker;
using ResourceLoader;
using ResourceLoader.AddressableResourceLoader;
using TickHandler;
using TickHandler.UnityTickHandler;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.Tester
{
internal class SimpleTester : MonoBehaviour
{
	[SerializeField]
	private GamePositioningConfig  _gamePositioningConfig;
	
	[Header("Settings")]
	[SerializeField]
	private Transform _gamesParent;

	[Header("UI Settings")]
	[SerializeField]
	private Transform gameListContainer;

	[SerializeField]
	private GameItemButton buttonPrefab;

	[SerializeField]
	private Transform buttonsContainer;

	[Header("Functional Buttons")]
	[SerializeField]
	private Transform functionalButtonsContainer;

	[SerializeField]
	private Button toggleFunctionalButtonsUI;

	[SerializeField]
	private Button homeButton;

	[SerializeField]
	private Button restartButton;
	
	private IShortGameFactory _gameFactory;
	private CancellationTokenSource _cancellationTokenSource;
	private IDiContainer _globalGameDiContainer;
	private UnityInGameLogger _logger;
	private AddressableResourceLoader _resourceLoader;
	private IShortGame _currentGame;

	private readonly List<GameItemButton> _createdButtons = new();
	private readonly List<Type> _gameTypes = new();

	private bool _functionalButtonsVisible = false;
	private bool _isGameRunning = false;
	private SimpleShortGamePool _pool;
	private PoolManager _poolObjects;
	private UnityTickHandler _tickHandler;

	private async void Start()
	{
		Application.targetFrameRate = 60;
 		_cancellationTokenSource = new CancellationTokenSource();
		var exitGameCancellationToken = Application.exitCancellationToken;

		if (_gamesParent == null)
		{
			_gamesParent = transform;
		}

		_globalGameDiContainer = DiContainerFactory.CreateContainer();

		await InitializeAsync(exitGameCancellationToken);
	}

	private async Task InitializeAsync(CancellationToken cancellationToken)
	{
		_logger = new UnityInGameLogger();
		_globalGameDiContainer.RegisterAsSingleton<IInGameLogger>(_logger);

		_pool = new SimpleShortGamePool(_logger);
		_globalGameDiContainer.RegisterAsSingleton<IShortGamesPool>(_pool);

		_poolObjects = new PoolManager();
		_globalGameDiContainer.RegisterAsSingleton<IPoolManager>(_poolObjects);

		_resourceLoader = new AddressableResourceLoader();
		_globalGameDiContainer.RegisterAsSingleton<IResourceLoader>(_resourceLoader);

		var dispatcher = gameObject.AddComponent<UnityDispatcherBehaviour>();
		_tickHandler = new UnityTickHandler(dispatcher);
		_globalGameDiContainer.RegisterAsSingleton<ITickHandler>(_tickHandler);

		var resourceMapping = GetResourceMapping();
		_gameFactory =
			AddressableShortGameFactoryFactory.CreateAddressableShortGameFactory(_gamesParent, resourceMapping, _gamePositioningConfig);
		_globalGameDiContainer.RegisterAsSingleton(_gameFactory);

		var games = GetGameTypes();
		foreach (var gameType in games)
		{
			try
			{
				await _gameFactory.PreloadGameResourcesAsync(gameType, cancellationToken);
				_logger.Log($"Preloaded resources for {gameType.Name}");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Failed to preload {gameType.Name}: {ex.Message}");
			}
		}

		CreateGameButtons();

		InitializeFunctionalButtons();
	}

	private IReadOnlyList<Type> GetGameTypes()
	{
		var types = new[]
		{
			typeof(AsteroidsGame),
			typeof(BoxTower),
			typeof(LawnmowerGame),
			typeof(EscapeFromDarkGame),
			typeof(Game2048),
			typeof(FlyHumansGame),
			typeof(LightseekerGame),
		};

		_gameTypes.AddRange(types);
		return types;
	}

	private Dictionary<Type, string> GetResourceMapping()
	{
		return new Dictionary<Type, string>
		{
			{ typeof(AsteroidsGame), ResourceIdsContainer.GameAsteroids.AsteroidGame },
			{ typeof(BoxTower), ResourceIdsContainer.GameBoxTower.BoxTower },
			{ typeof(LawnmowerGame), ResourceIdsContainer.GameLawnmover.GameLawnmower },
			{ typeof(EscapeFromDarkGame), ResourceIdsContainer.GameEscapeFromDark.EscapeFromDarkMain },
			{ typeof(Game2048), ResourceIdsContainer.GroupGame2048.Id2048Main},
			{ typeof(FlyHumansGame), ResourceIdsContainer.GameFlyHumans.FlyHumansMain},
			{ typeof(LightseekerGame), ResourceIdsContainer.GameLightseeker.LightseekerMain}
		};
	}

	private void CreateGameButtons()
	{
		if (buttonPrefab == null || buttonsContainer == null)
		{
			Debug.LogError("Button prefab or container not assigned!");
			return;
		}

		foreach (var gameType in _gameTypes)
		{
			var button = Instantiate(buttonPrefab, buttonsContainer);
			button.name = $"Button_{gameType.Name}";
			button.Setup(gameType.Name);
			button.OnButtonClicked += OnGameButtonClick;
			_createdButtons.Add(button);

			_logger.Log($"Created button for {gameType.Name}");
		}
	}

	private void InitializeFunctionalButtons()
	{
		// Инициализация кнопки переключения функциональных кнопок
		if (toggleFunctionalButtonsUI != null)
		{
			toggleFunctionalButtonsUI.onClick.AddListener(ToggleFunctionalButtons);
		}

		// Инициализация кнопки Home
		if (homeButton != null)
		{
			homeButton.onClick.AddListener(OnHomeButtonClick);
		}

		// Инициализация кнопки Restart
		if (restartButton != null)
		{
			restartButton.onClick.AddListener(OnRestartButtonClick);
		}

		// Устанавливаем начальное состояние UI
		SetInitialUIState();
	}

	private void SetInitialUIState()
	{
		// При первом запуске игра не запущена
		_isGameRunning = false;

		// Показываем контейнер списка игр
		if (gameListContainer != null)
		{
			gameListContainer.gameObject.SetActive(true);
		}

		// Скрываем контейнер функциональных элементов
		if (functionalButtonsContainer != null)
		{
			functionalButtonsContainer.gameObject.SetActive(false);
		}

		_logger.Log("Initial UI state set - showing game list, hiding functional container");
	}

	private void ToggleFunctionalButtons()
	{
		_functionalButtonsVisible = !_functionalButtonsVisible;
		UpdateFunctionalButtonsVisibility();
		_logger.Log($"Functional buttons visibility: {_functionalButtonsVisible}");
	}

	private void UpdateFunctionalButtonsVisibility()
	{
		functionalButtonsContainer.gameObject.SetActive(_functionalButtonsVisible);
	}

	private void OnHomeButtonClick()
	{
		_logger.Log("Home button clicked - stopping current game and showing game list");

		// Останавливаем текущую игру
		StopCurrentGame();

		// Обновляем состояние
		_isGameRunning = false;

		// Переключаем контейнеры: показываем список игр, скрываем функциональные элементы
		if (gameListContainer != null)
		{
			gameListContainer.gameObject.SetActive(true);
		}

		if (functionalButtonsContainer != null)
		{
			functionalButtonsContainer.gameObject.SetActive(false);
		}
	}

	private void OnRestartButtonClick()
	{
		_logger.Log("Restart button clicked - restarting current game");

		if (_currentGame != null)
		{
			// Перезапускаем игру
			_currentGame.StopGame();
			_currentGame.StartGame();
			_logger.Log("Game restarted successfully");

			// Сбрасываем видимость функциональных кнопок при рестарте
			_functionalButtonsVisible = false;
			UpdateFunctionalButtonsVisibility();
		}
		else
		{
			_logger.LogWarning("No current game to restart");
		}
	}

	private async void OnGameButtonClick(string gameName)
	{
		_logger.Log($"Loading game: {gameName}");

		// Найдем тип игры по имени
		var gameType = _gameTypes.Find(t => t.Name == gameName);
		if (gameType == null)
		{
			_logger.LogError($"Game type not found: {gameName}");
			return;
		}

		// Останавливаем текущую игру перед загрузкой новой
		StopCurrentGame();

		// Создаем новую игру через фабрику
		var game = await _gameFactory.CreateShortGameAsync(gameType, _cancellationTokenSource.Token);
		if (game != null)
		{
			_logger.Log($"Successfully created game: {gameName}");

			// Сохраняем ссылку на текущую игру
			_currentGame = game;

			// Запускаем игру
			_currentGame.StartGame();
			_logger.Log($"Started game: {gameName}");

			// Обновляем состояние - игра запущена
			_isGameRunning = true;

			// Сбрасываем видимость функциональных кнопок при запуске новой игры
			_functionalButtonsVisible = false;

			// Переключаем контейнеры: скрываем список игр, показываем функциональные элементы
			if (gameListContainer != null)
			{
				gameListContainer.gameObject.SetActive(false);
			}

			if (functionalButtonsContainer != null)
			{
				functionalButtonsContainer.gameObject.SetActive(true);
			}

			// Обновляем видимость функциональных кнопок внутри контейнера (скроет Home/Restart)
			UpdateFunctionalButtonsVisibility();
		}
		else
		{
			_logger.LogError($"Failed to create game: {gameName}");
		}
	}

	private void StopCurrentGame()
	{
		if (_currentGame == null)
		{
			return;
		}

		_logger.Log($"Stopping current game: {_currentGame.GetType().Name}");

		// Останавливаем игру
		_currentGame.StopGame();

		// Удаляем префаб игры если это MonoBehaviour
		if (_currentGame is MonoBehaviour gameMonoBehaviour && gameMonoBehaviour != null)
		{
			_logger.Log($"Destroying game prefab: {gameMonoBehaviour.name}");
			Destroy(gameMonoBehaviour.gameObject);
		}

		// Очищаем ссылку
		_currentGame = null;
	}

	private void OnDestroy()
	{
		_cancellationTokenSource?.Cancel();

		// Останавливаем текущую игру
		StopCurrentGame();

		// Отписываемся от событий кнопок игр
		foreach (var button in _createdButtons)
		{
			if (button != null)
			{
				button.OnButtonClicked -= OnGameButtonClick;
			}
		}

		// Отписываемся от функциональных кнопок
		if (toggleFunctionalButtonsUI != null)
		{
			toggleFunctionalButtonsUI.onClick.RemoveListener(ToggleFunctionalButtons);
		}

		if (homeButton != null)
		{
			homeButton.onClick.RemoveListener(OnHomeButtonClick);
		}

		if (restartButton != null)
		{
			restartButton.onClick.RemoveListener(OnRestartButtonClick);
		}

		_gameFactory?.Dispose();
		_cancellationTokenSource?.Dispose();
		_globalGameDiContainer?.Dispose();
	}
}
}
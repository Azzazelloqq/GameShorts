using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.GameStats;
using Code.Core.GamesLoader;
using Code.Core.GameSwiper.MVVM.Models;
using Code.Core.GameSwiper.MVVM.ViewModels;
using Code.Core.GameSwiper.MVVM.Views;
using Code.Generated.Addressables;
using InGameLogger;
using LightDI.Runtime;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Core.GameSwiper
{
/// <summary>
/// Controller that manages the MVVM-based GameSwiper.
/// Creates and initializes Model, ViewModel, and View components.
/// </summary>
public class GameSwiperController : IDisposable
{
	private readonly IShortGameServiceProvider _shortGameServiceProvider;
	private readonly IInGameLogger _logger;
	private readonly IResourceLoader _resourceLoader;
	private readonly IGameStatsService _gameStatsService;
	private readonly Transform _uiRoot;
	
	private GameSwiperViewModel _viewModel;
	private GameSwiperView _view;
	
	private bool _isInitialized;
	private bool _disposed;

	public GameSwiperController(
		Transform uiRoot,
		IShortGameServiceProvider shortGameServiceProvider,
		[Inject] IInGameLogger logger,
		[Inject] IResourceLoader resourceLoader,
		IGameStatsService gameStatsService)
	{
		_uiRoot = uiRoot ?? throw new ArgumentNullException(nameof(uiRoot));
		_shortGameServiceProvider = shortGameServiceProvider ?? throw new ArgumentNullException(nameof(shortGameServiceProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
		_gameStatsService = gameStatsService ?? throw new ArgumentNullException(nameof(gameStatsService));
	}

	/// <summary>
	/// Load UI and set up MVVM connections
	/// </summary>
	public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
	{
		if (_isInitialized)
		{
			_logger.LogWarning("GameSwiperController already initialized");
			return;
		}

		try
		{
			await InitializeSwiperView(cancellationToken);

			_isInitialized = true;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogException(ex);
			throw;
		}
	}
	
	/// <summary>
	/// Initialize MVVM-based GameSwiper
	/// </summary>
	private async ValueTask InitializeSwiperView(CancellationToken cancellationToken)
	{
		var prefab = await _resourceLoader.LoadResourceAsync<GameObject>(
			ResourceIdsContainer.Swiper.GameSwiperView,
			cancellationToken);
		
		var instance = Object.Instantiate(prefab, _uiRoot);
		_view = instance.GetComponent<GameSwiperView>();
		
		if (_view == null)
		{
			_view = instance.AddComponent<GameSwiperView>();
		}
		
		var settings = new SwiperSettings
		{
			AnimationDuration = 0.3f,
			UseScreenHeight = true,
			ImageSpacing = 1920f
		};

		var model = new GameSwiperModel(settings);
		
		_viewModel = new GameSwiperViewModel(model, _shortGameServiceProvider, _gameStatsService, _logger);
		await _viewModel.InitializeAsync(cancellationToken);
		
		await _view.InitializeAsync(_viewModel, cancellationToken);
		
		_viewModel.OnNextGameStartedAsync += OnMVVMNextGameStartedAsync;
		_viewModel.OnPreviousGameStartedAsync += OnMVVMPreviousGameStartedAsync;
		
		_logger.Log("MVVM GameSwiper initialized successfully");
	}
	
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		if (_view != null)
		{
			_view.Dispose();
			_view = null;
		}
		
		if (_viewModel != null)
		{
			_viewModel.OnNextGameStartedAsync -= OnMVVMNextGameStartedAsync;
			_viewModel.OnPreviousGameStartedAsync -= OnMVVMPreviousGameStartedAsync;
			_viewModel.Dispose();
			_viewModel = null;
		}
	}

	/// <summary>
	/// Handle the next game event from MVVM ViewModel
	/// </summary>
	private Task OnMVVMNextGameStartedAsync()
	{
		_logger.Log("Next game started via MVVM");
		return Task.CompletedTask;
	}
	
	/// <summary>
	/// Handle previous game event from MVVM ViewModel
	/// </summary>
	private Task OnMVVMPreviousGameStartedAsync()
	{
		_logger.Log("Previous game started via MVVM");
		return Task.CompletedTask;
	}
}
}
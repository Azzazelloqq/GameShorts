﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using InGameLogger;
using LightDI.Runtime;
using ResourceLoader;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Code.Core.ShortGamesCore.Source.Factory
{
public class AddressableShortGameFactory : IShortGameFactory
{
	private readonly IResourceLoader _resourceLoader;
	private readonly IInGameLogger _logger;
	private readonly CancellationTokenSource _disposeCancellationTokenSource;
	private readonly Transform _parent;
	private readonly Dictionary<Type, string> _resourcesInfo;
	
	private readonly Dictionary<Type, GameObject> _preloadedPrefabs = new();
	private readonly Dictionary<Type, int> _preloadRefCount = new();
	private readonly GamePositioningConfig _positioningConfig;
	private readonly Dictionary<GameType, int> _gameTypeCounters = new();
	private readonly Dictionary<GameType, Transform> _gameTypeParents = new();
	private bool _disposed;

	public AddressableShortGameFactory(
		Transform parent,
		Dictionary<Type, string> resourcesInfo,
		[Inject] IResourceLoader resourceLoader,
		[Inject] IInGameLogger logger,
		GamePositioningConfig positioningConfig = null)
	{
		_resourceLoader = resourceLoader;
		_logger = logger;
		_disposeCancellationTokenSource = new CancellationTokenSource();
		_parent = parent;
		_resourcesInfo = resourcesInfo;
		_positioningConfig = positioningConfig;
		
		InitializeGameTypeParents();
	}
	
	private void InitializeGameTypeParents()
	{
		// Initialize counters for each game type
		// We'll use the main parent transform directly instead of creating sub-containers
		foreach (GameType gameType in Enum.GetValues(typeof(GameType)))
		{
			_gameTypeParents[gameType] = _parent; // Use the same parent for all
			_gameTypeCounters[gameType] = 0;
		}
	}
	
	public void Dispose()
	{
		if (_disposed)
		{
			_logger.LogError("AddressableShortGameFactory already disposed, skipping");
			return;
		}
		
		_logger.Log("Disposing AddressableShortGameFactory - START");
		_disposed = true;
		
		try
		{
			if (!_disposeCancellationTokenSource.IsCancellationRequested)
			{
				_disposeCancellationTokenSource.Cancel();
			}
			
			DisposePreloadedPrefabs();
			
			_disposeCancellationTokenSource.Dispose();
			_logger.Log("Disposing AddressableShortGameFactory - COMPLETED");
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error during dispose: {ex.Message}");
		}
	}
	
	public async ValueTask<T> CreateShortGameAsync<T>(CancellationToken token) where T : Component, IShortGame
	{
		token.ThrowIfCancellationRequested();
		
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _disposeCancellationTokenSource.Token);
		
		GameObject prefab = null;
		if (_preloadedPrefabs.TryGetValue(typeof(T), out var preloadedPrefab))
		{
			prefab = preloadedPrefab;
			_logger.Log($"Using preloaded prefab for {typeof(T).Name}");
		}
		else
		{
			if (!_resourcesInfo.TryGetValue(typeof(T), out var resourceId))
			{
				_logger.LogError($"Can't find {typeof(T).FullName} resource id, need add in resources dictionary!");
				return null;
			}
			
			try
			{
				var loadedPrefab = await _resourceLoader.LoadResourceAsync<GameObject>(resourceId, linkedCts.Token);
				prefab = loadedPrefab;
				_logger.Log($"Loaded prefab for {typeof(T).Name} on demand");
			}
			catch (Exception e)
			{
				_logger.LogError($"Failed to load resource for {typeof(T).Name}: {e.Message}");
				return null;
			}
		}
		
		if (prefab == null)
		{
			_logger.LogError($"Prefab for {typeof(T).Name} is null");
			return null;
		}
		
		var gameType = GameTypeDetector.GetGameType(typeof(T));
		var instance = InstantiateGameWithPositioning(prefab, gameType, typeof(T));
		var shortGame = instance.GetComponent<T>();
		
		// If specific type not found, try base interface
		if (shortGame == null)
		{
			var baseGame = instance.GetComponent<IShortGame>();
			if (baseGame is T typedGame)
			{
				shortGame = typedGame;
				_logger.Log($"Found component via interface cast: {baseGame.GetType().Name}");
			}
		}
		
		if (shortGame == null)
		{
			// Log all components for debugging
			var components = instance.GetComponents<Component>();
			_logger.LogError($"Prefab for {typeof(T).Name} doesn't have component {typeof(T).Name}. Found components: {string.Join(", ", components.Select(c => c.GetType().Name))}");
			if (Application.isEditor && !Application.isPlaying)
				Object.DestroyImmediate(instance);
			else
				Object.Destroy(instance);
			return null;
		}
		
		return shortGame;
	}
	
	public async ValueTask<IShortGame> CreateShortGameAsync(Type gameType, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _disposeCancellationTokenSource.Token);
		
		if (!typeof(Component).IsAssignableFrom(gameType) || !typeof(IShortGame).IsAssignableFrom(gameType))
		{
			_logger.LogError($"Type {gameType.Name} must be Component and implement IShortGame");
			return null;
		}
		
		GameObject prefab;
		if (_preloadedPrefabs.TryGetValue(gameType, out var preloadedPrefab))
		{
			prefab = preloadedPrefab;
			_logger.Log($"Using preloaded prefab for {gameType.Name}");
		}
		else
		{
			if (!_resourcesInfo.TryGetValue(gameType, out var resourceId))
			{
				_logger.LogError($"Can't find {gameType.FullName} resource id, need add in resources dictionary!");
				return null;
			}
			
			try
			{
				var loadedPrefab = await _resourceLoader.LoadResourceAsync<GameObject>(resourceId, linkedCts.Token);
				prefab = loadedPrefab;
				_logger.Log($"Loaded prefab for {gameType.Name} on demand");
			}
			catch (Exception e)
			{
				_logger.LogError($"Failed to load resource for {gameType.Name}: {e.Message}");
				return null;
			}
		}
		
		if (prefab == null)
		{
			_logger.LogError($"Prefab for {gameType.Name} is null");
			return null;
		}
		
		var detectedGameType = GameTypeDetector.GetGameType(gameType);
		var instance = InstantiateGameWithPositioning(prefab, detectedGameType, gameType);
		
		// Try to get the component
		var shortGame = instance.GetComponent(gameType) as IShortGame;
		
		// If not found, try to get IShortGame interface directly
		if (shortGame == null)
		{
			shortGame = instance.GetComponent<IShortGame>();
			if (shortGame != null)
			{
				_logger.Log($"Found IShortGame component of type {shortGame.GetType().Name} instead of {gameType.Name}");
			}
		}
		
		if (shortGame == null)
		{
			// Log all components for debugging
			var components = instance.GetComponents<Component>();
			_logger.LogError($"Prefab for {gameType.Name} doesn't have component {gameType.Name}. Found components: {string.Join(", ", components.Select(c => c.GetType().Name))}");
			if (Application.isEditor && !Application.isPlaying)
				Object.DestroyImmediate(instance);
			else
				Object.Destroy(instance);
			return null;
		}
		
		return shortGame;
	}
	
	public async ValueTask PreloadGameResourcesAsync<T>(CancellationToken token) where T : Component, IShortGame
	{
		await PreloadGameResourcesAsync(typeof(T), token);
	}
	
	public async ValueTask PreloadGameResourcesAsync(Type gameType, CancellationToken token)
	{
		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _disposeCancellationTokenSource.Token);
		
		if (_preloadedPrefabs.ContainsKey(gameType))
		{
			_preloadRefCount[gameType]++;
			_logger.Log($"Prefab for {gameType.Name} already preloaded, ref count: {_preloadRefCount[gameType]}");
			return;
		}
		
		if (!_resourcesInfo.TryGetValue(gameType, out var resourceId))
		{
			var errorMsg = $"Can't find {gameType.FullName} resource id, need add in resources dictionary!";
			_logger.LogError(errorMsg);
			throw new InvalidOperationException(errorMsg);
		}
		
		try
		{
			var prefab = await _resourceLoader.LoadResourceAsync<GameObject>(resourceId, linkedCts.Token);
			_preloadedPrefabs[gameType] = prefab;
			_preloadRefCount[gameType] = 1;
			_logger.Log($"Preloaded prefab for {gameType.Name}");
		}
		catch (Exception e)
		{
			_logger.LogError($"Failed to preload {gameType.Name}: {e.Message}");
			throw;
		}
	}
	
	public void UnloadGameResources<T>() where T : Component, IShortGame
	{
		UnloadGameResources(typeof(T));
	}
	
	public void UnloadGameResources(Type gameType)
	{
		if (!_preloadedPrefabs.ContainsKey(gameType))
		{
			_logger.LogWarning($"No preloaded resources for {gameType.Name} to unload");
			return;
		}
		
		_preloadRefCount[gameType]--;
		
		if (_preloadRefCount[gameType] <= 0)
		{
			if (_preloadedPrefabs.TryGetValue(gameType, out var prefab))
			{
				_resourceLoader.ReleaseResource(prefab);
				_logger.Log($"Unloaded resources for {gameType.Name}");
			}
			
			_preloadedPrefabs.Remove(gameType);
			_preloadRefCount.Remove(gameType);
		}
		else
		{
			_logger.Log($"Decreased ref count for {gameType.Name}, current: {_preloadRefCount[gameType]}");
		}
	}
	
	private void DisposePreloadedPrefabs()
	{
		if (_preloadedPrefabs.Count <= 0)
		{
			return;
		}

		foreach (var prefab in _preloadedPrefabs.Values)
		{
			if (prefab == null)
			{
				continue;
			}

			_resourceLoader.ReleaseResource(prefab);
		}

		_preloadedPrefabs.Clear();
		_preloadRefCount.Clear();
	}
	
	private GameObject InstantiateGameWithPositioning(GameObject prefab, GameType gameType, Type gameClassType)
	{
		GameObject instance;
		Transform parentTransform = _gameTypeParents[gameType];
		
		switch (gameType)
		{
			case GameType.UI:
				instance = InstantiateUIGame(prefab, parentTransform);
				break;
				
			case GameType.TwoD:
				instance = Instantiate2DGame(prefab, parentTransform);
				break;
				
			case GameType.ThreeD:
				instance = Instantiate3DGame(prefab, parentTransform);
				break;
				
			default:
				// Fallback for unknown types
				instance = Object.Instantiate(prefab, parentTransform);
				break;
		}
		
		// Ensure the instance is active (even if prefab was inactive)
		if (!instance.activeSelf)
		{
			instance.SetActive(true);
			_logger.Log($"Activated instantiated {gameClassType.Name} (prefab was inactive)");
		}
		
		_gameTypeCounters[gameType]++;
		_logger.Log($"Instantiated {gameClassType.Name} as {gameType} game at position {instance.transform.position}");
		
		return instance;
	}
	
	private GameObject Instantiate3DGame(GameObject prefab, Transform parent)
	{
		var instance = Object.Instantiate(prefab, parent);
		
		// Position 3D games with spacing to avoid overlap
		if (_positioningConfig != null)
		{
			int index = _gameTypeCounters[GameType.ThreeD];
			instance.transform.position = _positioningConfig.GetPosition3D(index);
		}
		
		return instance;
	}
	
	private GameObject Instantiate2DGame(GameObject prefab, Transform parent)
	{
		var instance = Object.Instantiate(prefab, parent);
		
		// Position 2D games with spacing, offset from 3D games
		if (_positioningConfig != null)
		{
			int index = _gameTypeCounters[GameType.TwoD];
			instance.transform.position = _positioningConfig.GetPosition2D(index);
		}
		
		return instance;
	}
	
	private GameObject InstantiateUIGame(GameObject prefab, Transform parent)
	{
		GameObject instance;
		int index = _gameTypeCounters[GameType.UI];
		
		if (_positioningConfig != null && _positioningConfig.CreateSeparateCanvasForUIGames)
		{
			// Create a dedicated Canvas for this UI game
			var canvasGO = new GameObject($"UIGameCanvas_{index}");
			canvasGO.transform.SetParent(parent);
			
			var canvas = canvasGO.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = _positioningConfig != null ? _positioningConfig.GetCanvasSortOrder(index) : index;
			
			canvasGO.AddComponent<CanvasScaler>();
			canvasGO.AddComponent<GraphicRaycaster>();
			
			instance = Object.Instantiate(prefab, canvasGO.transform);
			
			// Reset position for UI elements
			var rectTransform = instance.GetComponent<RectTransform>();
			if (rectTransform != null)
			{
				rectTransform.anchoredPosition = Vector2.zero;
				rectTransform.localScale = Vector3.one;
			}
		}
		else
		{
			// Use existing Canvas structure
			instance = Object.Instantiate(prefab, parent);
		}
		
		return instance;
	}
}
}
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using InGameLogger;
using LightDI.Runtime;
using ResourceLoader;
using UnityEngine;
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
	private bool _disposed;

	public AddressableShortGameFactory(
		Transform parent,
		Dictionary<Type, string> resourcesInfo,
		[Inject] IResourceLoader resourceLoader,
		[Inject] IInGameLogger logger)
	{
		_resourceLoader = resourceLoader;
		_logger = logger;
		_disposeCancellationTokenSource = new CancellationTokenSource();
		_parent = parent;
		_resourcesInfo = resourcesInfo;
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
		
		var instance = Object.Instantiate(prefab, _parent);
		var shortGame = instance.GetComponent<T>();
		
		if (shortGame == null)
		{
			_logger.LogError($"Prefab for {typeof(T).Name} doesn't have component {typeof(T).Name}");
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
		
		var instance = Object.Instantiate(prefab, _parent);
		var shortGame = instance.GetComponent(gameType) as IShortGame;
		
		if (shortGame == null)
		{
			_logger.LogError($"Prefab for {gameType.Name} doesn't have component {gameType.Name}");
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

	public void Dispose()
	{
		if (_disposed)
		{
			_logger.Log("AddressableShortGameFactory already disposed, skipping");
			return;
		}
		
		_disposed = true;
		_logger.Log("Disposing AddressableShortGameFactory - START");
		
		try
		{
			_logger.Log($"Disposing: Checking cancellation token state");
			if (!_disposeCancellationTokenSource.IsCancellationRequested)
			{
				_logger.Log($"Disposing: Cancelling token source");
				_disposeCancellationTokenSource.Cancel();
			}
			
			_logger.Log($"Disposing: Releasing {_preloadedPrefabs.Count} prefabs");
			// Выгружаем все предзагруженные префабы
			foreach (var prefab in _preloadedPrefabs.Values)
			{
				_logger.Log($"Disposing: Releasing prefab {prefab?.name ?? "null"}");
				_resourceLoader.ReleaseResource(prefab);
			}
			
			_logger.Log($"Disposing: Clearing collections");
			_preloadedPrefabs.Clear();
			_preloadRefCount.Clear();
			
			_logger.Log($"Disposing: Disposing cancellation token source");
			_disposeCancellationTokenSource.Dispose();
			
			_logger.Log("Disposing AddressableShortGameFactory - COMPLETED");
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error during dispose: {ex.Message}");
			throw;
		}
	}
}
}
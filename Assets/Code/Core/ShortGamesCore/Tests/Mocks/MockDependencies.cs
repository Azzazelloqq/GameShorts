using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Core.ShortGamesCore.Source.Pool;
using InGameLogger;
using ResourceLoader;
using UnityEngine;

namespace Code.Core.ShotGamesCore.Tests.Mocks
{
    /// <summary>
    /// Мок для логгера
    /// </summary>
    public class MockLogger : IInGameLogger
    {
        public List<string> LoggedMessages { get; } = new();
        public List<string> LoggedWarnings { get; } = new();
        public List<string> LoggedErrors { get; } = new();
        public List<Exception> LoggedExceptions { get; } = new();
        
        public void Log(string message)
        {
            LoggedMessages.Add(message);
        }
        
        public void LogWarning(string message)
        {
            LoggedWarnings.Add(message);
        }
        
        public void LogError(string message)
        {
            LoggedErrors.Add(message);
        }
        
        public void LogError(Exception exception)
        {
            LoggedErrors.Add($"Exception: {exception.Message}");
            LoggedExceptions.Add(exception);
        }
        
        public void LogException(Exception exception)
        {
            LoggedExceptions.Add(exception);
            LoggedErrors.Add($"Exception: {exception.GetType().Name} - {exception.Message}");
        }
        
        public void Clear()
        {
            LoggedMessages.Clear();
            LoggedWarnings.Clear();
            LoggedErrors.Clear();
            LoggedExceptions.Clear();
        }
        
        public void Dispose()
        {
            // Nothing to dispose in mock
            Clear();
        }
    }
    
    /// <summary>
    /// Мок для загрузчика ресурсов
    /// </summary>
    public class MockResourceLoader : IResourceLoader
    {
        private readonly Dictionary<string, object> _resources = new();
        private readonly Dictionary<string, object> _cache = new();
        public int LoadCallCount { get; private set; }
        public int PreloadCallCount { get; private set; }
        public int ReleaseCallCount { get; private set; }
        public List<string> LoadedResourceIds { get; } = new();
        public List<string> PreloadedResourceIds { get; } = new();
        public List<string> ReleasedResourceIds { get; } = new();
        
        public bool ShouldThrowOnLoad { get; set; }
        public bool ShouldReturnNull { get; set; }
        
        public void AddResource(string resourceId, object resource)
        {
            _resources[resourceId] = resource;
        }
        
        public async Task PreloadInCacheAsync<TResource>(string resourceId, CancellationToken token)
        {
            PreloadCallCount++;
            PreloadedResourceIds.Add(resourceId);
            
            if (ShouldThrowOnLoad)
            {
                throw new Exception($"Failed to preload resource: {resourceId}");
            }
            
            // Симулируем асинхронную предзагрузку
            await Task.Delay(10, token);
            
            if (_resources.TryGetValue(resourceId, out var resource))
            {
                _cache[resourceId] = resource;
            }
            else
            {
                // Создаём фейковый ресурс для кэша
                object mockResource;
                if (typeof(TResource) == typeof(GameObject))
                {
                    mockResource = new GameObject($"MockCached_{resourceId}");
                }
                else if (typeof(TResource).IsSubclassOf(typeof(UnityEngine.Object)))
                {
                    // Для других Unity объектов возвращаем null или создаём GameObject
                    mockResource = new GameObject($"MockCached_{resourceId}");
                }
                else
                {
                    // Для не-Unity типов используем default
                    mockResource = default(TResource);
                }
                _cache[resourceId] = mockResource;
            }
        }
        
        public TResource LoadResource<TResource>(string resourceId)
        {
            LoadCallCount++;
            LoadedResourceIds.Add(resourceId);
            
            if (ShouldThrowOnLoad)
            {
                throw new Exception($"Failed to load resource: {resourceId}");
            }
            
            if (ShouldReturnNull)
            {
                return default(TResource);
            }
            
            // Сначала проверяем кэш
            if (_cache.TryGetValue(resourceId, out var cached))
            {
                return (TResource)cached;
            }
            
            if (_resources.TryGetValue(resourceId, out var resource))
            {
                return (TResource)resource;
            }
            
            // Создаём фейковый ресурс
            if (typeof(TResource) == typeof(GameObject))
            {
                return (TResource)(object)new GameObject($"MockResource_{resourceId}");
            }
            
            return default(TResource);
        }
        
        public void LoadResource<TResource>(string resourceId, Action<TResource> onResourceLoaded, CancellationToken token)
        {
            LoadCallCount++;
            LoadedResourceIds.Add(resourceId);
            
            if (ShouldThrowOnLoad)
            {
                throw new Exception($"Failed to load resource: {resourceId}");
            }
            
            // Симулируем асинхронную загрузку с callback
            Task.Run(async () =>
            {
                await Task.Delay(10, token);
                var resource = LoadResource<TResource>(resourceId);
                onResourceLoaded?.Invoke(resource);
            }, token);
        }
        
        public async Task<TResource> LoadResourceAsync<TResource>(string resourceId, CancellationToken token)
        {
            LoadCallCount++;
            LoadedResourceIds.Add(resourceId);
            
            if (ShouldThrowOnLoad)
            {
                throw new Exception($"Failed to load resource: {resourceId}");
            }
            
            if (ShouldReturnNull)
            {
                return default(TResource);
            }
            
            // Симулируем асинхронную загрузку
            await Task.Delay(10, token);
            
            // Сначала проверяем кэш
            if (_cache.TryGetValue(resourceId, out var cached))
            {
                return (TResource)cached;
            }
            
            if (_resources.TryGetValue(resourceId, out var resource))
            {
                return (TResource)resource;
            }
            
            // Создаём фейковый GameObject для тестов
            if (typeof(TResource) == typeof(GameObject))
            {
                var go = new GameObject($"MockResource_{resourceId}");
                return (TResource)(object)go;
            }
            
            return default(TResource);
        }
        
        public async Task<TComponent> LoadAndCreateAsync<TComponent, TParent>(
            string resourceId, 
            TParent parent, 
            CancellationToken token = default)
        {
            LoadCallCount++;
            LoadedResourceIds.Add(resourceId);
            
            if (ShouldThrowOnLoad)
            {
                throw new Exception($"Failed to load and create resource: {resourceId}");
            }
            
            // Симулируем асинхронную загрузку
            await Task.Delay(10, token);
            
            // Для тестов создаём GameObject с компонентом
            if (typeof(TComponent).IsSubclassOf(typeof(Component)))
            {
                var go = new GameObject($"MockCreated_{resourceId}");
                
                // Если parent - Transform, устанавливаем родителя
                if (parent is Transform transform)
                {
                    go.transform.SetParent(transform);
                }
                
                // Добавляем компонент
                var component = go.AddComponent(typeof(TComponent));
                return (TComponent)(object)component;
            }
            
            return default(TComponent);
        }
        
        public void ReleaseResource<TResource>(TResource resource)
        {
            ReleaseCallCount++;
            
            // Для GameObject уничтожаем объект
            if (resource is GameObject go)
            {
                ReleasedResourceIds.Add(go.name);
                // В тестах используем DestroyImmediate для немедленного удаления
                if (Application.isEditor && !Application.isPlaying)
                    GameObject.DestroyImmediate(go);
                else
                    GameObject.Destroy(go);
            }
            else if (resource != null)
            {
                ReleasedResourceIds.Add(resource.ToString());
            }
        }
        
        public void ReleaseAllResources()
        {
            foreach (var resource in _cache.Values.ToList()) // ToList чтобы избежать изменения коллекции во время итерации
            {
                if (resource is GameObject go)
                {
                    // В тестах используем DestroyImmediate для немедленного удаления
                    if (Application.isEditor && !Application.isPlaying)
                        GameObject.DestroyImmediate(go);
                    else
                        GameObject.Destroy(go);
                }
            }
            
            foreach (var resource in _resources.Values.ToList()) // ToList чтобы избежать изменения коллекции во время итерации
            {
                if (resource is GameObject go)
                {
                    // В тестах используем DestroyImmediate для немедленного удаления
                    if (Application.isEditor && !Application.isPlaying)
                        GameObject.DestroyImmediate(go);
                    else
                        GameObject.Destroy(go);
                }
            }
            
            _cache.Clear();
            _resources.Clear();
            ReleasedResourceIds.Add("ALL_RESOURCES");
        }
        
        // Старый метод для обратной совместимости с тестами
        public void UnloadResource(string resourceId)
        {
            if (_cache.TryGetValue(resourceId, out var cached))
            {
                _cache.Remove(resourceId);
                if (cached is GameObject go1)
                {
                    ReleasedResourceIds.Add(go1.name);
                    // В тестах используем DestroyImmediate для немедленного удаления
                    if (Application.isEditor && !Application.isPlaying)
                        GameObject.DestroyImmediate(go1);
                    else
                        GameObject.Destroy(go1);
                }
            }
            
            if (_resources.TryGetValue(resourceId, out var resource))
            {
                _resources.Remove(resourceId);
                if (resource is GameObject go2)
                {
                    ReleasedResourceIds.Add(go2.name);
                    // В тестах используем DestroyImmediate для немедленного удаления
                    if (Application.isEditor && !Application.isPlaying)
                        GameObject.DestroyImmediate(go2);
                    else
                        GameObject.Destroy(go2);
                }
            }
            
            ReleaseCallCount++;
        }
        
        public void Clear()
        {
            LoadCallCount = 0;
            PreloadCallCount = 0;
            ReleaseCallCount = 0;
            LoadedResourceIds.Clear();
            PreloadedResourceIds.Clear();
            ReleasedResourceIds.Clear();
            _cache.Clear();
            _resources.Clear();
        }
        
        public void Dispose()
        {
            ReleaseAllResources();
            Clear();
        }
    }
    
    /// <summary>
    /// Мок для фабрики игр
    /// </summary>
    public class MockShortGameFactory : IShortGameFactory
    {
        private readonly Dictionary<Type, GameObject> _prefabs = new();
        private readonly Dictionary<Type, bool> _preloadedTypes = new();
        
        public int CreateCallCount { get; private set; }
        public int PreloadCallCount { get; private set; }
        public int UnloadCallCount { get; private set; }
        public bool ShouldThrowOnCreate { get; set; }
        public bool ShouldReturnNull { get; set; }
        
        public List<Type> CreatedTypes { get; } = new();
        public List<Type> PreloadedTypes { get; } = new();
        public List<Type> UnloadedTypes { get; } = new();
        
        public void AddPrefab(Type gameType, GameObject prefab)
        {
            _prefabs[gameType] = prefab;
        }
        
        public async ValueTask<T> CreateShortGameAsync<T>(CancellationToken token) where T : Component, IShortGame
        {
            return await CreateShortGameAsync(typeof(T), token) as T;
        }
        
        public async ValueTask<IShortGame> CreateShortGameAsync(Type gameType, CancellationToken token)
        {
            CreateCallCount++;
            CreatedTypes.Add(gameType);
            
            if (ShouldThrowOnCreate)
            {
                throw new Exception($"Failed to create game: {gameType.Name}");
            }
            
            if (ShouldReturnNull)
            {
                return null;
            }
            
            // Симулируем асинхронное создание
            await Task.Delay(10, token);
            
            GameObject prefab;
            if (!_prefabs.TryGetValue(gameType, out prefab))
            {
                // Создаём фейковый GameObject для тестов
                prefab = new GameObject($"Mock_{gameType.Name}");
                
                // Добавляем соответствующий компонент
                if (gameType == typeof(MockPoolableShortGame))
                {
                    prefab.AddComponent<MockPoolableShortGame>();
                }
                else
                {
                    prefab.AddComponent<MockShortGame>();
                }
            }
            
            var instance = GameObject.Instantiate(prefab);
            return instance.GetComponent<IShortGame>();
        }
        
        public async ValueTask PreloadGameResourcesAsync<T>(CancellationToken token) where T : Component, IShortGame
        {
            await PreloadGameResourcesAsync(typeof(T), token);
        }
        
        public async ValueTask PreloadGameResourcesAsync(Type gameType, CancellationToken token)
        {
            PreloadCallCount++;
            PreloadedTypes.Add(gameType);
            _preloadedTypes[gameType] = true;
            
            // Симулируем асинхронную предзагрузку
            await Task.Delay(10, token);
        }
        
        public void UnloadGameResources<T>() where T : Component, IShortGame
        {
            UnloadGameResources(typeof(T));
        }
        
        public void UnloadGameResources(Type gameType)
        {
            UnloadCallCount++;
            UnloadedTypes.Add(gameType);
            _preloadedTypes.Remove(gameType);
        }
        
        public void Dispose()
        {
            _prefabs.Clear();
            _preloadedTypes.Clear();
        }
        
        public void Clear()
        {
            CreateCallCount = 0;
            PreloadCallCount = 0;
            UnloadCallCount = 0;
            CreatedTypes.Clear();
            PreloadedTypes.Clear();
            UnloadedTypes.Clear();
        }
    }
    
    /// <summary>
    /// Мок для пула игр
    /// </summary>
    public class MockShortGamesPool : IShortGamesPool
    {
        private readonly Dictionary<Type, Queue<IPoolableShortGame>> _pool = new();
        
        public int GetCallCount { get; private set; }
        public int ReleaseCallCount { get; private set; }
        public int WarmUpCallCount { get; private set; }
        public int ClearCallCount { get; private set; }
        
        public bool ShouldReturnFromPool { get; set; } = true;
        
        public bool TryGetShortGame<T>(out T game) where T : class, IPoolableShortGame
        {
            if (TryGetShortGame(typeof(T), out var pooledGame))
            {
                game = pooledGame as T;
                return game != null;
            }
            
            game = null;
            return false;
        }
        
        public bool TryGetShortGame(Type gameType, out IPoolableShortGame game)
        {
            GetCallCount++;
            game = null;
            
            if (!ShouldReturnFromPool)
            {
                return false;
            }
            
            if (_pool.TryGetValue(gameType, out var queue) && queue.Count > 0)
            {
                game = queue.Dequeue();
                return true;
            }
            
            return false;
        }
        
        public void ReleaseShortGame<T>(T game) where T : class, IPoolableShortGame
        {
            ReleaseShortGame((IPoolableShortGame)game);
        }
        
        public void ReleaseShortGame(IPoolableShortGame game)
        {
            ReleaseCallCount++;
            
            var type = game.GetType();
            if (!_pool.ContainsKey(type))
            {
                _pool[type] = new Queue<IPoolableShortGame>();
            }
            
            _pool[type].Enqueue(game);
        }
        
        public void WarmUpPool<T>(T game) where T : class, IPoolableShortGame
        {
            WarmUpPool((IPoolableShortGame)game);
        }
        
        public void WarmUpPool(IPoolableShortGame game)
        {
            WarmUpCallCount++;
            
            var type = game.GetType();
            if (!_pool.ContainsKey(type))
            {
                _pool[type] = new Queue<IPoolableShortGame>();
            }
            
            _pool[type].Enqueue(game);
        }
        
        public IEnumerable<Type> GetPooledGameTypes()
        {
            return _pool.Keys;
        }
        
        public void ClearPoolForType<T>() where T : class, IPoolableShortGame
        {
            ClearPoolForType(typeof(T));
        }
        
        public void ClearPoolForType(Type gameType)
        {
            ClearCallCount++;
            _pool.Remove(gameType);
        }
        
        public void Dispose()
        {
            _pool.Clear();
        }
        
        public void Clear()
        {
            GetCallCount = 0;
            ReleaseCallCount = 0;
            WarmUpCallCount = 0;
            ClearCallCount = 0;
            _pool.Clear();
        }
    }
}

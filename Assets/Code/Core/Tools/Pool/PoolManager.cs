using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Core.Tools.Pool
{
	public class PoolManager : IDisposable, IPoolManager
	{
		private Dictionary<GameObject, GameObjectPool> _pools;
		private List<Object> _unityObjects;
		protected bool isDisposed;

		private readonly Transform _poolRoot;

		public PoolManager()
		{
			GameObject poolObj = AddComponent(new GameObject($"Pools"));
			Object.DontDestroyOnLoad(poolObj);
			_poolRoot = poolObj.transform;
			_pools = new Dictionary<GameObject, GameObjectPool>();
		}

		private GameObjectPool CreateOrGetPool(GameObject prefab)
		{
			if (_pools.TryGetValue(prefab, out GameObjectPool existedPool))
				return existedPool;
			GameObject poolObj = AddComponent(new GameObject($"pool [{prefab.name}]"));
			poolObj.transform.SetParent(_poolRoot);
			GameObjectPool createdPool = new GameObjectPool(new GameObjectPool.Ctx
			{
					prefab = prefab,
					parrent = poolObj.transform
			});
			_pools.Add(prefab, createdPool);
			return createdPool;
		}

		public GameObject Get(GameObject prefab)
		{
			if (isDisposed)
				return null;
			GameObjectPool pool = CreateOrGetPool(prefab);
			return pool?.Get();
		}
		public GameObject Get(GameObject prefab, Vector3 position)
		{
			if (isDisposed)
				return null;
			GameObjectPool pool = CreateOrGetPool(prefab);
			return pool?.Get(position);
		}
		public GameObject Get(GameObject prefab, Transform parent)
		{
			if (isDisposed)
				return null;
			GameObjectPool pool = CreateOrGetPool(prefab);
			return pool?.Get(parent);
		}
		public GameObject Get(GameObject prefab, Vector3 position, float rotateDig)
		{
			if (isDisposed)
				return null;
			GameObjectPool pool = CreateOrGetPool(prefab);
			return pool?.Get(position, rotateDig);
		}
		public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotate)
		{
			if (isDisposed)
				return null;
			GameObjectPool pool = CreateOrGetPool(prefab);
			return pool?.Get(position, rotate);
		}
		public GameObject Get(GameObject prefab, Vector3 position, Transform parent , Quaternion rotate)
		{
			if (isDisposed)
				return null;
			GameObjectPool pool = CreateOrGetPool(prefab);
			return pool?.Get(position, parent, rotate);
		}

		public void Return(GameObject prefab, GameObject obj)
		{
			if (isDisposed)
				return;
			GameObjectPool pool = CreateOrGetPool(prefab);
			pool?.Return(obj);
		}
		
		public void Clear()
		{
			if (isDisposed)
				return;
				
			// Clear all pools but keep the pools themselves
			if (_pools != null)
			{
				foreach (var pool in _pools.Values)
				{
					pool?.Clear();
				}
			}
		}
		
		protected TObject AddComponent<TObject>(TObject obj) where TObject : Object
		{
			if (_unityObjects == null)
				_unityObjects = new List<Object>(1);
			_unityObjects.Add(obj);
			return obj;
		}

		public void Dispose()
		{
			if (isDisposed)
				return;
				
			isDisposed = true;
			
			// Dispose all pools
			if (_pools != null)
			{
				foreach (var pool in _pools.Values)
				{
					pool?.Dispose();
				}
				_pools.Clear();
			}
			
			// Destroy all Unity objects
			if (_unityObjects != null)
			{
				foreach (var unityObject in _unityObjects)
				{
					if (unityObject != null)
						Object.Destroy(unityObject);
				}
				_unityObjects.Clear();
			}
		}
	}
}
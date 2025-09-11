using UnityEngine;
using UnityEngine.Pool;

namespace Code.Core.Tools.Pool
{
	public class GameObjectPool : IBasePool<GameObject>
	{
		public struct Ctx
		{
			public GameObject prefab;
			public Transform parrent;
		}

		private readonly Ctx _ctx;
		private ObjectPool<GameObject> _pool;
		
		public GameObjectPool(Ctx ctx)
		{
			_ctx = ctx;
			_pool = new ObjectPool<GameObject>(instGameObject, PoolGet, PoolReturn, DestroyObject, true, 20, 30);

			GameObject instGameObject()
			{
				return GameObject.Instantiate(_ctx.prefab, _ctx.parrent);
			}

			void PoolGet(GameObject poolObject)
			{
				
			}

			void PoolReturn(GameObject poolObject)
			{
				poolObject.gameObject.SetActive(false);
			}

			void DestroyObject(GameObject poolObject)
			{
				GameObject.Destroy(poolObject);
			}
		}

		public GameObject Get()
		{
			var gameObject = _pool.Get();
			gameObject.gameObject.SetActive(true);
			return gameObject;
		}
		public GameObject Get(Vector3 position)
		{
			var gameObject = _pool.Get();
			gameObject.transform.position = position;
			gameObject.gameObject.SetActive(true);
			return gameObject;
		}
		public GameObject Get(Transform parent)
		{
			var gameObject = _pool.Get();
			gameObject.transform.SetParent(parent);
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.transform.rotation = Quaternion.identity;
			gameObject.transform.localRotation = Quaternion.identity;
			gameObject.gameObject.SetActive(true);
			return gameObject;
		}
		public GameObject Get(Vector3 position, float rotateDig)
		{
			var gameObject = _pool.Get();
			gameObject.transform.position = position;
			gameObject.transform.rotation = Quaternion.Euler(0, 0, rotateDig );
			
			gameObject.gameObject.SetActive(true);
			return gameObject;
		}
		public void Return(GameObject element)
		{
			 element.transform.SetParent(_ctx.parrent);
			_pool.Release(element);
		}
	}
}
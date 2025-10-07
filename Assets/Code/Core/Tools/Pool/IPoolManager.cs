using System;
using UnityEngine;

namespace Code.Core.Tools.Pool
{
	public interface IPoolManager : IDisposable
	{
		GameObject Get(GameObject prefab);
		GameObject Get(GameObject prefab, Vector3 position);
		GameObject Get(GameObject prefab, Transform parent);
		GameObject Get(GameObject prefab, Vector3 position, float rotateDig);
		GameObject Get(GameObject prefab, Vector3 position, Quaternion rotate);
		GameObject Get(GameObject prefab,  Vector3 position, Transform parent , Quaternion rotate);
		void Return(GameObject prefab, GameObject obj);
		void Clear();
	}
}
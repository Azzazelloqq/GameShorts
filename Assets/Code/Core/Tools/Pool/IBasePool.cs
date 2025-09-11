using UnityEngine;

namespace Code.Core.Tools.Pool
{
	public interface IBasePool<T>
	{
		public T Get();
		public T Get(Vector3 position);
		public T Get(Transform parent);
		public T Get(Vector3 position, float rotateDig);
		public void Return(T element);
	}
}
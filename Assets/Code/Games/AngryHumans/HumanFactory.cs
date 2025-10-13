using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Random = UnityEngine.Random;

namespace Code.Games.AngryHumans
{
internal class HumanFactory : MonoBehaviour
{
	[Serializable]
	public struct HumanTypeData
	{
		[SerializeField]
		private string _name;

		[SerializeField]
		private AssetReference _humanPrefabReference;

		public string Name => _name;
		public AssetReference HumanPrefabReference => _humanPrefabReference;

		public HumanTypeData(string name, AssetReference humanPrefabReference)
		{
			_name = name;
			_humanPrefabReference = humanPrefabReference;
		}
	}

	[SerializeField]
	private HumanTypeData[] _humanTypes;

	private readonly Dictionary<string, GameObject> _loadedPrefabs = new();
	private readonly List<AsyncOperationHandle<GameObject>> _handles = new();

	#if UNITY_EDITOR
	private void OnValidate()
	{
		for (var i = 0; i < _humanTypes.Length; i++)
		{
			var humanTypeData = _humanTypes[i];

			if (!string.IsNullOrEmpty(humanTypeData.Name))
			{
				continue;
			}

			if (humanTypeData.HumanPrefabReference == null)
			{
				continue;
			}

			_humanTypes[i] = new HumanTypeData(humanTypeData.HumanPrefabReference.editorAsset.name,
				humanTypeData.HumanPrefabReference);
		}
	}
	#endif

	public async Task PreloadHumansAsync(CancellationToken cancellationToken = default)
	{
		foreach (var humanType in _humanTypes)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}

			var handle = Addressables.LoadAssetAsync<GameObject>(humanType.HumanPrefabReference);
			_handles.Add(handle);

			var prefab = await handle.Task;

			if (!cancellationToken.IsCancellationRequested && prefab != null)
			{
				_loadedPrefabs[humanType.Name] = prefab;
			}
		}
	}

	public Human CreateRandomHuman(Transform parent = null)
	{
		if (_loadedPrefabs.Count == 0)
		{
			return null;
		}

		var keys = new List<string>(_loadedPrefabs.Keys);
		var randomKey = keys[Random.Range(0, keys.Count)];

		return CreateHuman(randomKey, parent);
	}

	public Human CreateHuman(string typeName, Transform parent = null)
	{
		if (!_loadedPrefabs.TryGetValue(typeName, out var prefab))
		{
			return null;
		}

		var instance = Instantiate(prefab, parent);
		return instance.GetComponent<Human>();
	}

	private void OnDestroy()
	{
		foreach (var handle in _handles)
		{
			if (handle.IsValid())
			{
				Addressables.Release(handle);
			}
		}

		_handles.Clear();
		_loadedPrefabs.Clear();
	}
}
}
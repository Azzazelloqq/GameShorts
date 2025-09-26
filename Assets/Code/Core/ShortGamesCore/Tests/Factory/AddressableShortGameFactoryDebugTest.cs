using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShotGamesCore.Tests.Mocks;
using NUnit.Framework;
using UnityEngine;

namespace Code.Core.ShotGamesCore.Tests.Factory
{
[TestFixture]
public class AddressableShortGameFactoryDebugTest
{
	[Test]
	public async Task TestDispose_SimpleVersion()
	{
		// Простая версия теста для отладки
		var logger = new MockLogger();
		var resourceLoader = new MockResourceLoader();
		var parent = new GameObject("Parent").transform;

		var resourcesInfo = new System.Collections.Generic.Dictionary<Type, string>
		{
			{ typeof(MockShortGame), "MockShortGame_Resource" }
		};

		var factory = new AddressableShortGameFactory(parent, resourcesInfo, resourceLoader, logger);

		try
		{
			// Создаём префаб
			var prefab = new GameObject("TestPrefab");
			prefab.AddComponent<MockShortGame>();
			resourceLoader.AddResource("MockShortGame_Resource", prefab);

			Debug.Log("1. Starting preload...");

			// Предзагружаем
			await factory.PreloadGameResourcesAsync<MockShortGame>(CancellationToken.None);

			Debug.Log("2. Preload completed");
			Debug.Log($"   Logged messages: {string.Join(", ", logger.LoggedMessages)}");

			// Dispose
			Debug.Log("3. Calling Dispose...");
			factory.Dispose();

			Debug.Log("4. Dispose completed");
			Debug.Log($"   Release call count: {resourceLoader.ReleaseCallCount}");

			// Clean up
			GameObject.DestroyImmediate(prefab);
			GameObject.DestroyImmediate(parent.gameObject);

			// Test completed successfully (without hanging)
		}
		catch (Exception ex)
		{
			Debug.LogError($"Test failed with exception: {ex}");
			throw;
		}
	}

	[Test]
	public void TestDispose_SyncVersion()
	{
		// Полностью синхронная версия для сравнения
		var logger = new MockLogger();
		var resourceLoader = new MockResourceLoader();
		var parent = new GameObject("Parent").transform;

		var resourcesInfo = new System.Collections.Generic.Dictionary<Type, string>
		{
			{ typeof(MockShortGame), "MockShortGame_Resource" }
		};

		var factory = new AddressableShortGameFactory(parent, resourcesInfo, resourceLoader, logger);

		try
		{
			Debug.Log("1. Creating factory");

			// Просто вызываем Dispose без предзагрузки
			Debug.Log("2. Calling Dispose...");
			factory.Dispose();

			Debug.Log("3. Dispose completed");

			// Clean up
			GameObject.DestroyImmediate(parent.gameObject);

			Assert.Pass("Sync test completed without hanging");
		}
		catch (Exception ex)
		{
			Debug.LogError($"Sync test failed with exception: {ex}");
			throw;
		}
	}
}
}
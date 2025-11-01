using LightDI.Runtime;
using UnityEditor;

namespace EditorTools
{
	/// <summary>
	/// Handles cleanup of static data when Domain Reload is disabled.
	/// This ensures that static fields are properly reset between Play Mode sessions.
	/// This class is Editor-only and won't be included in builds.
	/// </summary>
	public static class StaticDataCleaner
	{
		/// <summary>
		/// Manual cleanup method that can be called from OnDestroy or OnApplicationQuit
		/// </summary>
		public static void ManualCleanup()
		{
			CleanupDiContainers();
		}

		/// <summary>
		/// Editor initialization. Subscribes to Play Mode state changes.
		/// </summary>
		[InitializeOnLoadMethod]
		private static void Initialize()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			// Cleanup when exiting Play Mode in Editor
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				CleanupDiContainers();
			}
		}

		private static void CleanupDiContainers()
		{
			DiContainerProvider.Dispose();
		}
	}
}


using System;
using System.Collections;
using IngameDebugConsole;
using UnityEngine;
#if UNITY_EDITOR
using Screen = UnityEngine.Device.Screen; // To support Device Simulator on Unity 2021.1+
#endif

namespace Code.Utils
{
	public sealed class DebugConsoleInitializer : MonoBehaviour
	{
		[SerializeField]
		private GameObject _consolePrefab;

		[SerializeField]
		[Min( 0 )]
		private int _instantiateAfterFrames = 3;

		[SerializeField]
		[Tooltip( "If enabled, will wait until Screen & Canvas sizes become valid before enabling DebugLogManager (prevents NaN popup position bug)." )]
		private bool _waitForUiToBeReady = true;

		[SerializeField]
		[Min( 0 )]
		[Tooltip( "Fallback delay if Wait For UI To Be Ready is disabled." )]
		private int _enableDebugLogManagerAfterFrames = 1;

		[SerializeField]
		[Min( 1 )]
		[Tooltip( "Safety cap so we don't wait forever in edge cases." )]
		private int _maxWaitFrames = 120;

		[SerializeField]
		[Tooltip( "If the debug console saved an invalid popup position (NaN/Infinity) in PlayerPrefs (IDGPPos), clear it on startup." )]
		private bool _clearCorruptedPopupPosition = true;

		private IEnumerator Start()
		{
#if DEVELOPMENT_BUILD || UNITY_EDITOR
			if( !_consolePrefab )
				yield break;

			for( int i = 0; i < _instantiateAfterFrames; i++ )
				yield return null;

			if( _clearCorruptedPopupPosition )
				ClearCorruptedPopupPositionPref();

			GameObject consoleInstance = Instantiate( _consolePrefab );

			// The NaN issue happens when DebugLogManager.Start() triggers DebugLogPopup.UpdatePosition(true)
			// before Canvas/Screen dimensions are initialized (common on first frame in Editor/Device Simulator).
			// To avoid touching the AssetStore package, we simply delay DebugLogManager.Start().
			DebugLogManager manager = consoleInstance.GetComponentInChildren<DebugLogManager>( true );
			if( manager )
			{
				manager.enabled = false;

				if( _waitForUiToBeReady )
					yield return WaitForUiReady( manager, _maxWaitFrames );
				else
				{
					for( int i = 0; i < _enableDebugLogManagerAfterFrames; i++ )
						yield return null;
				}

				manager.enabled = true;
			}
#endif
		}

		private static IEnumerator WaitForUiReady( DebugLogManager manager, int maxWaitFrames )
		{
			RectTransform canvasRect = manager.transform as RectTransform;

			int cappedFrames = Mathf.Max( 1, maxWaitFrames );
			for( int i = 0; i < cappedFrames; i++ )
			{
				if( IsUiReady( canvasRect ) )
					yield break;

				yield return null;
			}
		}

		private static bool IsUiReady( RectTransform canvasRect )
		{
			if( Screen.width <= 0 || Screen.height <= 0 )
				return false;

			if( !canvasRect )
				return true; // Can't validate; don't block forever

			Vector2 size = canvasRect.rect.size;
			return size.x > 0f && size.y > 0f && IsFinite( size.x ) && IsFinite( size.y );
		}

		private static bool IsFinite( float value )
		{
			return !float.IsNaN( value ) && !float.IsInfinity( value );
		}

		private static void ClearCorruptedPopupPositionPref()
		{
			const string key = "IDGPPos";
			if( !PlayerPrefs.HasKey( key ) )
				return;

			string json = PlayerPrefs.GetString( key, string.Empty );
			if( string.IsNullOrEmpty( json ) )
				return;

			// JsonUtility can serialize NaN/Infinity into a string; if it happens once, it persists across runs.
			if( json.IndexOf( "NaN", StringComparison.OrdinalIgnoreCase ) >= 0 ||
				json.IndexOf( "Infinity", StringComparison.OrdinalIgnoreCase ) >= 0 )
			{
				PlayerPrefs.DeleteKey( key );
				PlayerPrefs.Save();
			}
		}
	}
}
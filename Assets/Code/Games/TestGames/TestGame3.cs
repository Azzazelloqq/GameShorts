using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Games.TestGames
{
public class TestGame3: MonoBehaviour, IShortGame3D
{
	[SerializeField]
	private Camera _camera;

	[SerializeField]
	private Transform _targetToRotate;

	[SerializeField]
	private GraphicRaycaster _graphicRaycaster;
	
	private Coroutine _rotateRoutine;
	private RenderTexture _rt;

	public bool IsPreloaded { get; private set; }
	
	public void Dispose()
	{
		if (_rotateRoutine != null)
		{
			StopCoroutine(_rotateRoutine);
			_rotateRoutine = null;
		}
		
		// Clean up camera target texture
		if (_camera != null)
		{
			_camera.targetTexture = null;
		}
		
		// Clean up RenderTexture
		if (_rt != null)
		{
			_rt.Release();
			_rt = null;
		}
		
		IsPreloaded = false;
	}
	
	public async ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
	{
		// Use actual screen dimensions to match device display
		int width = Screen.width;
		int height = Screen.height;
		
		// Create RenderTexture matching screen resolution
		_rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
		{
			antiAliasing = 2,
			useMipMap = false,
			autoGenerateMips = false
		};
		_rt.Create();
		
		// Assign the RenderTexture to the camera
		if (_camera != null)
		{
			_camera.targetTexture = _rt;
			// Set camera aspect ratio to match screen
			_camera.aspect = (float)width / height;
		}
		
		await Task.Delay(100, cancellationToken);
		
		// Mark as preloaded
		IsPreloaded = true;
	}

	public RenderTexture GetRenderTexture()
	{
		return _rt;
	}

	public void StartGame()
	{
		if (_rotateRoutine != null)
		{
			StopCoroutine(_rotateRoutine);
		}

		_rotateRoutine = StartCoroutine(RotateTarget());
	}

	private IEnumerator RotateTarget()
	{
		while (true)
		{
			_targetToRotate.Rotate(Vector3.right, 1);
			yield return null;
		}
	}

	public void Disable()
	{
		gameObject.SetActive(false);
	}

	public void Enable()
	{
		gameObject.SetActive(true);
	}

	public void RestartGame()
	{
		if (_rotateRoutine != null)
		{
			StopCoroutine(_rotateRoutine);
		}
		
		_rotateRoutine = StartCoroutine(RotateTarget());
	}

	public void StopGame()
	{
		if (_rotateRoutine != null)
		{
			StopCoroutine(_rotateRoutine);
			_rotateRoutine = null;
		}
	}

	public void EnableInput()
	{
		_graphicRaycaster.enabled = true;
	}

	public void DisableInput()
	{
		_graphicRaycaster.enabled = false;
	}
}
}
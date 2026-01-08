using UnityEngine;

namespace Code.Utils
{
public static class RenderTextureUtils
{
	public static RenderTexture GetRenderTextureForShortGame(Camera mainGameCamera, Camera uiCamera = null)
	{
		if (mainGameCamera != null)
		{
			mainGameCamera.targetTexture = null;
		}
		
		if (uiCamera != null)
		{
			uiCamera.targetTexture = null;
		}

		var width = Screen.width;
		var height = Screen.height;

		// Create RenderTexture matching screen resolution
		var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
		{
			antiAliasing = 2,
			useMipMap = false,
			autoGenerateMips = false
		};
		renderTexture.Create();

		// Assign the RenderTexture to the camera
		if (mainGameCamera != null)
		{
			mainGameCamera.targetTexture = renderTexture;
			// Set camera aspect ratio to match screen
			mainGameCamera.aspect = (float)width / height;
		}

		if (uiCamera != null)
		{
			uiCamera.targetTexture = renderTexture;
		}

		return renderTexture;
	}
	
	/// <summary>
	/// Releases GPU memory and destroys the RenderTexture instance. Also detaches it from provided cameras.
	/// </summary>
	public static void ReleaseAndDestroy(ref RenderTexture renderTexture, params Camera[] cameras)
	{
		if (renderTexture == null)
		{
			return;
		}

		if (cameras != null)
		{
			foreach (var camera in cameras)
			{
				if (camera != null && camera.targetTexture == renderTexture)
				{
					camera.targetTexture = null;
				}
			}
		}

		if (renderTexture.IsCreated())
		{
			renderTexture.Release();
		}

		if (Application.isEditor && !Application.isPlaying)
		{
			UnityEngine.Object.DestroyImmediate(renderTexture);
		}
		else
		{
			UnityEngine.Object.Destroy(renderTexture);
		}

		renderTexture = null;
	}
}
}
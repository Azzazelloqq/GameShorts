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
}
}
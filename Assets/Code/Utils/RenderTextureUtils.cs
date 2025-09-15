using UnityEngine;

namespace Code.Utils
{
public static class RenderTextureUtils
{
	public static RenderTexture GetRenderTexture(Camera camera)
	{
		if (camera != null)
		{
			camera.targetTexture = null;
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
		if (camera != null)
		{
			camera.targetTexture = renderTexture;
			// Set camera aspect ratio to match screen
			camera.aspect = (float)width / height;
		}

		return renderTexture;
	}
}
}
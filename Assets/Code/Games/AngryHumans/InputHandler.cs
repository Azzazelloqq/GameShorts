using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.Games.AngryHumans
{
/// <summary>
/// Handles UI input and forwards it to LaunchController
/// Should be placed on a UI Panel under Canvas
/// </summary>
internal class InputHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
	[SerializeField]
	private LaunchController _launchController;

	public void OnPointerDown(PointerEventData eventData)
	{
		if (_launchController != null)
		{
			_launchController.OnPointerDown(eventData);
		}
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (_launchController != null)
		{
			_launchController.OnDrag(eventData);
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (_launchController != null)
		{
			_launchController.OnPointerUp(eventData);
		}
	}
}
}
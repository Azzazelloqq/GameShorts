using Disposable;
using UnityEngine;

namespace SceneContext
{
public class SceneContextView : MonoBehaviourDisposable
{
	[SerializeField]
	private Camera _camera;

	public Camera Camera => _camera;

	[SerializeField]
	private Canvas mainCanvas;

	[SerializeField]
	private Transform uiParent;

	public Transform UiParent
		=> uiParent;

	public Canvas MainCanvas
		=> mainCanvas;
}
}
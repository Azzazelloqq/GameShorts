using Disposable;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game2
{
internal class CameraFollow : MonoBehaviourDisposable
{
    [Header("Follow Settings")]
    [SerializeField]
    private Transform target; // Tower root or spawner

    [SerializeField]
    private float followSpeed = 2f;

    [SerializeField]
    private float verticalOffset = 5f;

    [SerializeField]
    private float targetHeightRatio = 0.7f; // Keep tower in upper 30% of screen

    private Camera cam;
    private Vector3 initialPosition;
    private BlockSpawner spawner;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        initialPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        var towerHeight = spawner != null ? spawner.GetTowerHeight() : target.position.y;

        var targetY = towerHeight + verticalOffset;

        var targetPosition = new Vector3(initialPosition.x, targetY, initialPosition.z);

        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
    }
}
}
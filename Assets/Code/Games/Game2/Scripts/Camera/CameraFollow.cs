using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game2
{
    public class CameraFollow : BaseMonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] private Transform target; // Tower root or spawner
        [SerializeField] private float followSpeed = 2f;
        [SerializeField] private float verticalOffset = 5f;
        [SerializeField] private float targetHeightRatio = 0.7f; // Keep tower in upper 30% of screen

        private Camera cam;
        private Vector3 initialPosition;
        private BlockSpawner spawner;

        protected override void Awake()
        {
            base.Awake();
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            
            initialPosition = transform.position;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null && target.TryGetComponent<BlockSpawner>(out var blockSpawner))
            {
                spawner = blockSpawner;
            }
        }

        public void ResetPosition()
        {
            transform.position = initialPosition;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Get tower height
            float towerHeight = 0f;
            if (spawner != null)
            {
                towerHeight = spawner.GetTowerHeight();
            }
            else
            {
                // Fallback: use target position
                towerHeight = target.position.y;
            }

            // Calculate desired camera position
            float targetY = towerHeight + verticalOffset;
            
            // Keep the camera's X and Z at initial position, only follow Y
            Vector3 targetPosition = new Vector3(initialPosition.x, targetY, initialPosition.z);
            
            // Smooth follow
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        }
    }
}

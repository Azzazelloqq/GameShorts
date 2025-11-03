using Code.Core.BaseDMDisposable.Scripts;
using Code.Games.Game2.Scripts.Core;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game2
{
    internal class BlockMover : BaseMonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float speed = 2f;
        [SerializeField] private float limit = 5f;
        
        private Axis axis;
        private float direction = 1f;
        private bool isMoving = false;
        private bool isPaused = false;
        private Vector3 startPosition;

        public bool IsMoving => isMoving;
        public Vector3 Position => transform.position;

        public void StartMoving(Axis movementAxis, float moveSpeed, float moveLimit)
        {
            axis = movementAxis;
            speed = moveSpeed;
            limit = moveLimit;
            direction = 1f;
            isMoving = true;
            isPaused = false;
            startPosition = transform.position;
        }

        public void StopMoving()
        {
            isMoving = false;
            isPaused = false;
        }

        public BlockData GetBlockData()
        {
            var bounds = GetComponent<Renderer>().bounds;
            return new BlockData(transform.position, bounds.size, axis);
        }

        public void PauseMoving()
        {
            if (!isMoving)
                return;

            isPaused = true;
            isMoving = false;
        }

        public void ResumeMoving()
        {
            if (!isPaused)
                return;

            isPaused = false;
            isMoving = true;
        }

        private void Update()
        {
            if (!isMoving) return;

            float deltaTime = Time.deltaTime * speed * direction;
            
            if (axis == Axis.X)
            {
                transform.position += new Vector3(deltaTime, 0, 0);
            }
            else
            {
                transform.position += new Vector3(0, 0, deltaTime);
            }

            // Check boundaries and reverse direction
            float currentPosition = (axis == Axis.X) ? transform.position.x : transform.position.z;
            float startPos = (axis == Axis.X) ? startPosition.x : startPosition.z;
            float distance = currentPosition - startPos;
            
            if (Mathf.Abs(distance) >= limit)
            {
                // Reverse direction
                direction *= -1f;
                
                // Clamp position to stay within bounds
                float clampedDistance = Mathf.Sign(distance) * limit;
                if (axis == Axis.X)
                {
                    transform.position = new Vector3(startPos + clampedDistance, transform.position.y, transform.position.z);
                }
                else
                {
                    transform.position = new Vector3(transform.position.x, transform.position.y, startPos + clampedDistance);
                }
            }
        }
    }
}

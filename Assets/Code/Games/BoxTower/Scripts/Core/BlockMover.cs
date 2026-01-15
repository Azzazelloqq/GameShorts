using Disposable;
using Code.Games.Game2.Scripts.Core;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game2
{
internal class BlockMover : MonoBehaviourDisposable
{
    [Header("Movement Settings")]
    [SerializeField]
    private float speed = 2f;

    [SerializeField]
    private float limit = 5f;

    private Axis axis;
    private float direction = 1f;
    private bool isPaused;
    private Vector3 startPosition;

    public bool IsMoving { get; private set; }

    public void StartMoving(Axis movementAxis, float moveSpeed, float moveLimit)
    {
        axis = movementAxis;
        speed = moveSpeed;
        limit = moveLimit;
        direction = 1f;
        IsMoving = true;
        isPaused = false;
        startPosition = transform.position;
    }

    public void StopMoving()
    {
        IsMoving = false;
        isPaused = false;
    }

    public BlockData GetBlockData()
    {
        var bounds = GetComponent<Renderer>().bounds;
        return new BlockData(transform.position, bounds.size, axis);
    }

    public void PauseMoving()
    {
        if (!IsMoving)
        {
            return;
        }

        isPaused = true;
        IsMoving = false;
    }

    public void ResumeMoving()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;
        IsMoving = true;
    }

    private void Update()
    {
        if (!IsMoving)
        {
            return;
        }

        var deltaTime = Time.deltaTime * speed * direction;

        if (axis == Axis.X)
        {
            transform.position += new Vector3(deltaTime, 0, 0);
        }
        else
        {
            transform.position += new Vector3(0, 0, deltaTime);
        }

        var currentPosition = axis == Axis.X ? transform.position.x : transform.position.z;
        var startPos = axis == Axis.X ? startPosition.x : startPosition.z;
        var distance = currentPosition - startPos;

        if (Mathf.Abs(distance) < limit)
        {
            return;
        }

        direction *= -1f;

        var clampedDistance = Mathf.Sign(distance) * limit;
        if (axis == Axis.X)
        {
            transform.position =
                new Vector3(startPos + clampedDistance, transform.position.y, transform.position.z);
        }
        else
        {
            transform.position =
                new Vector3(transform.position.x, transform.position.y, startPos + clampedDistance);
        }
    }
}
}
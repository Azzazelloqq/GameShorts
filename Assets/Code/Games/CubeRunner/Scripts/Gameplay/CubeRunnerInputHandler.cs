using System;
using Code.Core.BaseDMDisposable.Scripts;
using LightDI.Runtime;
using R3;
using TickHandler;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameShorts.CubeRunner.Gameplay
{
    internal class CubeRunnerInputHandler : BaseDisposable
    {
        internal struct Ctx
        {
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private readonly Subject<Vector2Int> _swipeStream = new Subject<Vector2Int>();
        private readonly float _swipeThresholdPixels = 50f;

        private bool _isInputEnabled = true;
        private bool _isPointerDown;
        private Vector2 _pointerDownPosition;
        private Vector2 _pointerUpPosition;
        private readonly ITickHandler _tickHandler;

        public Subject<Vector2Int> Swipes => _swipeStream;

        public CubeRunnerInputHandler(Ctx ctx, [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            _tickHandler.FrameUpdate += OnUpdate;

            if (_ctx.isPaused != null)
            {
                AddDispose(_ctx.isPaused.Subscribe(isPaused =>
                {
                    _isInputEnabled = !isPaused;
                    ResetPointerState();
                }));
            }
        }

        private void OnUpdate(float _)
        {
            if (!_isInputEnabled)
            {
                return;
            }

            HandleTouchInput();
#if UNITY_EDITOR
            HandleMouseInput();
#endif
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0)
            {
                return;
            }

            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    TryBeginPointer(touch.position, touch.fingerId);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    TryEndPointer(touch.position);
                    break;
            }
        }

        private void HandleMouseInput()
        {
            if (Input.touchCount > 0)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryBeginPointer(Input.mousePosition, -1);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                TryEndPointer(Input.mousePosition);
            }
        }

        private void TryBeginPointer(Vector2 screenPosition, int fingerId)
        {
            if (IsPointerOverUI(fingerId))
            {
                return;
            }

            _pointerDownPosition = screenPosition;
            _isPointerDown = true;
        }

        private void TryEndPointer(Vector2 screenPosition)
        {
            if (!_isPointerDown)
            {
                return;
            }

            _isPointerDown = false;
            _pointerUpPosition = screenPosition;
            DetectSwipe();
        }

        private void DetectSwipe()
        {
            Vector2 delta = _pointerUpPosition - _pointerDownPosition;
            float distance = delta.magnitude;

            float threshold = Mathf.Max(10f, _swipeThresholdPixels);
            if (distance < threshold)
            {
                return;
            }

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                _swipeStream.OnNext(delta.x > 0f ? Vector2Int.right : Vector2Int.left);
            }
            else
            {
                _swipeStream.OnNext(delta.y > 0f ? Vector2Int.up : Vector2Int.down);
            }
        }

        private static bool IsPointerOverUI(int fingerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            if (fingerId >= 0)
            {
                return EventSystem.current.IsPointerOverGameObject(fingerId);
            }

            return EventSystem.current.IsPointerOverGameObject();
        }

        private void ResetPointerState()
        {
            _isPointerDown = false;
            _pointerDownPosition = Vector2.zero;
            _pointerUpPosition = Vector2.zero;
        }

        protected override void OnDispose()
        {
            _tickHandler.FrameUpdate -= OnUpdate;
            _swipeStream.OnCompleted();
            _swipeStream.Dispose();
            ResetPointerState();
        }
    }
}


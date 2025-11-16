using System;
using Code.Core.BaseDMDisposable.Scripts;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameShorts.CubeRunner.Gameplay
{
    internal class CubeRunnerInputHandler : BaseDisposable
    {
        internal struct Ctx
        {
            public ReactiveProperty<bool> isPaused;
            public float swipeThreshold;
        }

        private readonly Ctx _ctx;
        private readonly Subject<CubeRunnerSwipeDirection> _swipeStream = new Subject<CubeRunnerSwipeDirection>();
        private readonly IDisposable _updateSubscription;

        private bool _isInputEnabled = true;
        private bool _isPointerDown;
        private Vector2 _pointerDownPosition;
        private Vector2 _pointerUpPosition;

        public Subject<CubeRunnerSwipeDirection> Swipes => _swipeStream;

        public CubeRunnerInputHandler(Ctx ctx)
        {
            _ctx = ctx;
            _updateSubscription = Observable.EveryUpdate()
                .Subscribe(_ => OnUpdate());
            AddDispose(_updateSubscription);

            if (_ctx.isPaused != null)
            {
                AddDispose(_ctx.isPaused.Subscribe(isPaused =>
                {
                    _isInputEnabled = !isPaused;
                    ResetPointerState();
                }));
            }
        }

        private void OnUpdate()
        {
            if (!_isInputEnabled)
            {
                return;
            }

            HandleTouchInput();
            HandleMouseInput();
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

            float threshold = Mathf.Max(10f, _ctx.swipeThreshold);
            if (distance < threshold)
            {
                return;
            }

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                _swipeStream.OnNext(delta.x > 0f ? CubeRunnerSwipeDirection.Right : CubeRunnerSwipeDirection.Left);
            }
            else
            {
                _swipeStream.OnNext(delta.y > 0f ? CubeRunnerSwipeDirection.Up : CubeRunnerSwipeDirection.Down);
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
            _swipeStream.OnCompleted();
            _swipeStream.Dispose();
            ResetPointerState();
        }
    }
}


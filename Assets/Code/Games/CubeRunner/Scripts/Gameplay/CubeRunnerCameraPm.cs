using Disposable;
using GameShorts.CubeRunner.View;
using LightDI.Runtime;
using TickHandler;
using UnityEngine;

namespace GameShorts.CubeRunner.Gameplay
{
    internal class CubeRunnerCameraPm : DisposableBase
    {
        internal struct Ctx
        {
            public CubeRunnerSceneContextView sceneContextView;
            public CubeManager cubeManager;
            public float fixedHorizontalDistance;
        }

        private readonly Ctx _ctx;
        private readonly ITickHandler _tickHandler;
        private readonly Transform _cameraTransform;
        private readonly float _fallbackDistance = 6f;

        private Vector3 _offsetXZ;
        private float _initialHeight;
        private bool _offsetInitialized;
        private CubeView _trackedCubeView;

        public CubeRunnerCameraPm(Ctx ctx, [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;

            _cameraTransform = _ctx.sceneContextView != null
                ? _ctx.sceneContextView.MainCamera != null
                    ? _ctx.sceneContextView.MainCamera.transform
                    : null
                : null;

            if (_cameraTransform == null)
            {
                Debug.LogError("CubeRunnerCameraPm: отсутствует ссылка на MainCamera в контексте.");
                return;
            }

            _initialHeight = _cameraTransform.localPosition.y;
            _tickHandler.FrameLateUpdate += OnLateUpdate;
        }

        private void OnLateUpdate(float deltaTime)
        {
            if (_cameraTransform == null)
                return;

            var cubeView = _ctx.cubeManager?.CurrentCubeView;
            if (cubeView == null)
                return;

            if (_trackedCubeView != cubeView)
            {
                _trackedCubeView = cubeView;
                _offsetInitialized = false;
            }

            Vector3 cubePosition = cubeView.VisualRoot.position;
            EnsureOffsetInitialized(cubePosition);

            Vector3 desiredPosition = new Vector3(
                cubePosition.x + _offsetXZ.x,
                _initialHeight,
                cubePosition.z + _offsetXZ.z);

            _cameraTransform.position = desiredPosition;
            _cameraTransform.LookAt(cubePosition);
        }

        private void EnsureOffsetInitialized(Vector3 cubePosition)
        {
            if (_offsetInitialized || _cameraTransform == null)
                return;

            Vector3 offset = _cameraTransform.position - cubePosition;
            offset.y = 0f;

            if (offset.sqrMagnitude <= Mathf.Epsilon)
            {
                Vector3 forward = _cameraTransform.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude <= Mathf.Epsilon)
                {
                    forward = Vector3.back;
                }

                offset = -forward.normalized *
                         (_ctx.fixedHorizontalDistance > 0f ? _ctx.fixedHorizontalDistance : _fallbackDistance);
            }
            else
            {
                float distance = _ctx.fixedHorizontalDistance > 0f
                    ? _ctx.fixedHorizontalDistance
                    : offset.magnitude;
                offset = offset.normalized * distance;
            }

            _offsetXZ = new Vector3(offset.x, 0f, offset.z);
            _offsetInitialized = true;
        }

        protected override void OnDispose()
        {
            if (_tickHandler != null)
            {
                _tickHandler.FrameLateUpdate -= OnLateUpdate;
            }

            base.OnDispose();
        }
    }
}





using System;
using System.Threading;
using Disposable;
using LightDI.Runtime;
using UnityEngine;
using R3;
using TickHandler;
using Cysharp.Threading.Tasks;

namespace Code.Core.ShortGamesCore.Game2
{
    internal class BoxTowerCameraPm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public BoxTowerSceneContextView sceneContextView;
            public TowerModel towerModel;
        }

        private readonly Ctx _ctx;
        private Vector3 _initialCameraPosition;
        private Quaternion _initialCameraRotation;
        private readonly float _followSpeed = 2f;
        private readonly float _verticalOffset = 5f;
        private readonly ITickHandler _tickHandler;
        private bool _initialized;
        private bool _subscriptionsSet;

        public BoxTowerCameraPm(Ctx ctx, [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            SetupCamera();
            SetupSubscriptions();
            _initialized = true;
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            Initialize();

            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        private void SetupCamera()
        {
            // Store initial camera position and setup 45-degree view
            if (_ctx.sceneContextView.MainCamera != null)
            {
                // Position camera at an angle to view tower at 45 degrees
                var camera = _ctx.sceneContextView.MainCamera;

                // Get tower root position to properly center the camera on it
                Vector3 towerPosition = _ctx.sceneContextView.TowerRoot.position;

                // Set camera position for 45-degree diagonal view relative to tower
                Vector3 cameraOffset = new Vector3(4f, 6f, 4f); // Diagonal position, a bit further and higher
                camera.transform.position = towerPosition + cameraOffset;

                // Look at a point slightly below tower center to see the tower better
                Vector3 lookAtPoint = towerPosition + new Vector3(0f, -1f, 0f);
                camera.transform.LookAt(lookAtPoint);

                _initialCameraPosition = camera.transform.position;
                _initialCameraRotation = camera.transform.rotation;
            }
        }

        private void SetupSubscriptions()
        {
            if (_subscriptionsSet)
            {
                return;
            }
            _subscriptionsSet = true;

            // Subscribe to tower height changes
            AddDisposable(_ctx.towerModel.TowerHeight.Subscribe(OnTowerHeightChanged));

            // Reset camera when tower is cleared
            AddDisposable(_ctx.towerModel.TowerHeight.Where(height => height == 0f)
                .Subscribe(_ => ResetCameraPosition()));

            // Subscribe to scene updates for smooth camera movement
            _tickHandler.FrameLateUpdate += UpdateCamera;
        }

        private void OnTowerHeightChanged(float newHeight)
        {
            // Camera will smoothly follow in UpdateCamera
        }

        private void UpdateCamera(float deltaTime)
        {
            if (_ctx.sceneContextView.MainCamera == null) return;

            var camera = _ctx.sceneContextView.MainCamera;
            float towerHeight = _ctx.towerModel.TowerHeight.Value;
            
            // Calculate desired camera position based on tower height
            float targetY = _initialCameraPosition.y + towerHeight;
            
            // Keep the diagonal offset while following tower height
            Vector3 targetPosition = new Vector3(
                _initialCameraPosition.x, 
                targetY, 
                _initialCameraPosition.z);
            
            // Smooth follow position only - keep rotation fixed
            camera.transform.position = Vector3.Lerp(
                camera.transform.position, 
                targetPosition, 
                _followSpeed * deltaTime);
            
            // Keep the initial rotation fixed - no dynamic LookAt
            camera.transform.rotation = _initialCameraRotation;
        }

        private void ResetCameraPosition()
        {
            if (_ctx.sceneContextView.MainCamera != null)
            {
                var camera = _ctx.sceneContextView.MainCamera;
                camera.transform.position = _initialCameraPosition;
                camera.transform.rotation = _initialCameraRotation;
            }
        }

        protected override void OnDispose()
        {
            // Unsubscribe from scene updates
            _tickHandler.FrameLateUpdate -= UpdateCamera;
            
            base.OnDispose();
        }
    }
}

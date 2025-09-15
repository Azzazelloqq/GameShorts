using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using R3;

namespace Code.Core.ShortGamesCore.Game2
{
    internal class BoxTowerCameraPm : BaseDisposable
    {
        public struct Ctx
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

        public BoxTowerCameraPm(Ctx ctx)
        {
            _ctx = ctx;
            
            // Store initial camera position and setup 45-degree view
            if (_ctx.sceneContextView.MainCamera != null)
            {
                // Position camera at an angle to view tower at 45 degrees
                var camera = _ctx.sceneContextView.MainCamera;
                
                // Set camera position for 45-degree diagonal view
                Vector3 cameraOffset = new Vector3(4f, 6f, 4f); // Diagonal position, a bit further and higher
                camera.transform.position = cameraOffset;
                
                // Look at a point slightly below center to see the tower better
                Vector3 lookAtPoint = new Vector3(0f, -1f, 0f);
                camera.transform.LookAt(lookAtPoint);
                
                _initialCameraPosition = camera.transform.position;
                _initialCameraRotation = camera.transform.rotation;
            }
            
            // Subscribe to tower height changes
            AddDispose(_ctx.towerModel.TowerHeight.Subscribe(OnTowerHeightChanged));
            
            // Subscribe to game state changes to reset camera
            // We'll need access to game model for this, for now just reset on tower height 0
            AddDispose(_ctx.towerModel.TowerHeight.Where(height => height == 0f)
                .Subscribe(_ => ResetCameraPosition()));
            
            // Subscribe to scene updates for smooth camera movement
            _ctx.sceneContextView.OnUpdated += UpdateCamera;
        }

        private void OnTowerHeightChanged(float newHeight)
        {
            // Camera will smoothly follow in UpdateCamera
        }

        private void UpdateCamera()
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
                _followSpeed * Time.deltaTime);
            
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
            _ctx.sceneContextView.OnUpdated -= UpdateCamera;
            
            base.OnDispose();
        }
    }
}

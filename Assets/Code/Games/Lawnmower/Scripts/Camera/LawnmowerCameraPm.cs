using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Player;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Level;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.View;
using TickHandler;
using LightDI.Runtime;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Camera
{
    internal class LawnmowerCameraPm : BaseDisposable
    {
        public struct Ctx
        {
            public LawnmowerSceneContextView sceneContextView;
            public LawnmowerPlayerPm playerPm;
            public LawnmowerLevelManager levelManager;
        }

        private readonly Ctx _ctx;
        private LawnmowerCameraController _cameraController;

        public LawnmowerCameraPm(Ctx ctx)
        {
            _ctx = ctx;
            
            InitializeCameraController();
        }

        private void InitializeCameraController()
        {
            if (_ctx.sceneContextView?.MainCamera == null)
            {
                Debug.LogError("LawnmowerCameraPm: Main camera is null!");
                return;
            }

            var cameraCtx = new LawnmowerCameraController.Ctx
            {
                camera = _ctx.sceneContextView.MainCamera,
                playerPm = _ctx.playerPm,
                levelManager = _ctx.levelManager,
                settings = _ctx.sceneContextView.CameraSettingsAsset
            };

            _cameraController = LawnmowerCameraControllerFactory.CreateLawnmowerCameraController(cameraCtx);
            AddDispose(_cameraController);
            
            Debug.Log("LawnmowerCameraPm: Camera controller initialized");
        }

        public void UpdateTarget(LawnmowerPlayerPm newPlayerPm)
        {
            _cameraController?.SetTarget(newPlayerPm);
        }

        public void UpdateSettings(LawnmowerCameraSettings newSettings)
        {
            _cameraController?.SetCameraSettings(newSettings);
        }
    }
}

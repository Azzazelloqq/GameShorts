using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Player;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level;
using UnityEngine;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Camera
{
    internal class EscapeFromDarkCameraPm : BaseDisposable
    {
        internal struct Ctx
        {
            public EscapeFromDarkSceneContextView sceneContextView;
            public EscapeFromDarkPlayerPm playerPm;
            public EscapeFromDarkLevelPm levelPm;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private EscapeFromDarkCameraController _cameraController;

        public EscapeFromDarkCameraPm(Ctx ctx)
        {
            _ctx = ctx;
            
            InitializeCameraController();
        }

        private void InitializeCameraController()
        {
            if (_ctx.sceneContextView?.MainCamera == null)
            {
                Debug.LogError("EscapeFromDarkCameraPm: Main camera is null!");
                return;
            }

            var cameraCtx = new EscapeFromDarkCameraController.Ctx
            {
                camera = _ctx.sceneContextView.MainCamera,
                playerPm = _ctx.playerPm,
                levelPm = _ctx.levelPm
            };

            _cameraController = EscapeFromDarkCameraControllerFactory.CreateEscapeFromDarkCameraController(cameraCtx);
            AddDispose(_cameraController);
            
            // Принудительно фокусируемся на игроке
            _cameraController.FocusOnPlayer();

            Debug.Log("EscapeFromDarkCameraPm: Camera controller initialized");
        }


        public void FocusOnPlayer()
        {
            _cameraController?.FocusOnPlayer();
        }

        protected override void OnDispose()
        {
            // Контроллер камеры будет очищен автоматически через AddDispose
            base.OnDispose();
        }
    }
}

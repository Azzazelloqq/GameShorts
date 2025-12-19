using System;
using System.Collections.Generic;
using System.Threading;
using Disposable;
using Code.Core.Tools.Pool;
using Cysharp.Threading.Tasks;
using GameShorts.CubeRunner.Core;
using GameShorts.CubeRunner.Data;
using GameShorts.CubeRunner.Level;
using GameShorts.CubeRunner.View;
using LightDI.Runtime;
using R3;
using TickHandler;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace GameShorts.CubeRunner.Gameplay
{
    internal class CubeRunnerGameplayPm : DisposableBase
    {
        internal struct Ctx
        {
            public CubeRunnerSceneContextView sceneContextView;
            public ReactiveProperty<bool> isPaused;
            public Action onGameOver;
            public CubeManager cubeManager;
            public LevelManager levelManager;
        }

        private readonly Ctx _ctx;

        private Vector2Int _startGridPosition;
        private int _currentLevel;
        private readonly ITickHandler _tickHandler;
        private Vector3 _currentCubeDimensions;
        private bool _checkDead = true;
        private bool _newLevel;
        private float _newLevelTimer = 0;

        public CubeRunnerGameplayPm(Ctx ctx, [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
        }

        private void OnUpdate(float deltaTime)
        {
            if (_newLevel)
            {
                if (_newLevelTimer > 0)
                {
                    _newLevelTimer -= deltaTime;
                    return;
                }
                LoadNextLevel();
            }
            if (_ctx.cubeManager.CurrentCubeView == null) 
                return;
            var cubeView = _ctx.cubeManager.CurrentCubeView;
            
            if (!_ctx.cubeManager.IsRotating && _ctx.levelManager.IsWin(cubeView))
            {
                _ctx.cubeManager.DisableControl();
                _ctx.cubeManager.CurrentCubeView.SetActiveControl(false);
                _checkDead = false;
                _newLevel = true;
                _newLevelTimer = 2;
            }
            else if ((cubeView.transform.position.y < -20) && _checkDead)
            {
                _ctx.cubeManager.RespawnCube();
            }
        }

        private void LoadNextLevel()
        {
            _tickHandler.FrameUpdate -= OnUpdate;
            _ctx.levelManager.PlayerBorderDetected -= PlayerBorderDetected;
            _ctx.cubeManager.ClearCube();
            StartNewLevel();
        }

        private void PlayerBorderDetected()
        {
            _ctx.cubeManager.CurrentCubeView.Rigidbody.freezeRotation = false;
            _ctx.cubeManager.DisableControl();
        }

        public void StartNewLevel()
        {
            _newLevel = false;
            _checkDead = true;
            _currentLevel++;
            _currentCubeDimensions = UpdateDimensions();
            _ctx.levelManager.GenerateLevel(_currentCubeDimensions);
            _ctx.cubeManager.SpawnCube(_currentCubeDimensions,  _ctx.levelManager.StartCellPosition.ToVector3());
            _tickHandler.FrameUpdate += OnUpdate;
            _ctx.levelManager.PlayerBorderDetected += PlayerBorderDetected;
        }

        private Vector3 UpdateDimensions()
        {
            switch (_currentLevel)
            {
                case 1:
                   // return Vector3.one;
                case 2-3:
                    var result = new Vector3(1, 2, 1);
                    return result;
                default:
                    return new Vector3(Random.Range(1,3), Random.Range(1,3), Random.Range(1,3));
            }
        }

        private void HandleGameOver()
        {
            _ctx.onGameOver?.Invoke();
        }

        protected override void OnDispose()
        {
            _ctx.levelManager.PlayerBorderDetected -= PlayerBorderDetected;
            _tickHandler.FrameUpdate -= OnUpdate;
            base.OnDispose();
        }
    }
}


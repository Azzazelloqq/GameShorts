using Disposable;
using GameShorts.CubeRunner.Data;
using UnityEngine;

namespace GameShorts.CubeRunner.View
{
    internal class CubeRunnerSceneContextView : MonoBehaviourDisposable
    {
        [Header("Scene Roots")]
        [SerializeField]
        private Transform _worldRoot;

        [SerializeField]
        private Transform _tilesRoot;

        [SerializeField]
        private Transform _cubeSpawnPoint;

        [Header("Camera")]
        [SerializeField]
        private Camera _mainCamera;

        [Header("UI")]
        [SerializeField]
        private Canvas _uiCanvas;

        [Header("Settings")]
        [SerializeField]
        private CubeRunnerGameSettings _gameSettings;

        public Transform WorldRoot => _worldRoot;
        public Transform TilesRoot => _tilesRoot;
        public Transform CubeSpawnPoint => _cubeSpawnPoint;
        public Camera MainCamera => _mainCamera;
        public Canvas UiCanvas => _uiCanvas;
        public CubeRunnerGameSettings GameSettings => _gameSettings;
    }
}


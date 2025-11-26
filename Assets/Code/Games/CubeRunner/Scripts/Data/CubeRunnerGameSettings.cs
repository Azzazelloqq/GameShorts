using GameShorts.CubeRunner.View;
using UnityEngine;

namespace GameShorts.CubeRunner.Data
{
    [CreateAssetMenu(fileName = "CubeRunnerGameSettings", menuName = "GameShorts/CubeRunner/GameSettings")]
    internal class CubeRunnerGameSettings : ScriptableObject
    {
        [SerializeField]
        private float _tileSize = 1f;

        [SerializeField] private float _spawnHeight = 10;

        [Header("Cube")]
        [SerializeField]
        private Vector3 _cubeDimensions = Vector3.one;

        [Header("Prefabs")]
        [SerializeField]
        private GameObject _cubePrefab;

        [SerializeField]
        private GameObject _tilePrefab;
        [SerializeField]
        private GameObject _borderPrefab;

        public float TileSize => _tileSize;
        
        public Vector3 CubeDimensions => new Vector3(
            Mathf.Max(0.01f, _cubeDimensions.x),
            Mathf.Max(0.01f, _cubeDimensions.y),
            Mathf.Max(0.01f, _cubeDimensions.z));
        
        public GameObject CubePrefab => _cubePrefab;
        public GameObject TilePrefab => _tilePrefab;
        public GameObject BorderPrefab => _borderPrefab;
        
        public float SpawnHeight => _spawnHeight;
    }
}


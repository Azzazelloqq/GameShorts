using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Games._2048.Scripts.Input;
using UnityEngine;

namespace Code.Games._2048.Scripts.View
{
    internal class Game2048SceneContextView : SceneContextView
    {
        [SerializeField] 
        private Transform _gameSpawnPoint;
        
        
        [SerializeField]
        private Game2048InputAreaView _inputAreaView;
        
        [SerializeField]
        private GameObject _cubePrefab;
        
        [Header("Game Settings")]
        
        [SerializeField]
        private float _launchForce = 10f;
        
        [SerializeField]
        private float _spawnDelay = 1f;
        
        [Header("Merge Settings")]
        
        [SerializeField]
        private float _mergeUpwardForce = 5f;

        public Transform GameSpawnPoint => _gameSpawnPoint;
        public Game2048InputAreaView InputAreaView => _inputAreaView;
        public GameObject CubePrefab => _cubePrefab;
        public float LaunchForce => _launchForce;
        public float SpawnDelay => _spawnDelay;
        public float MergeUpwardForce => _mergeUpwardForce;
    }
}

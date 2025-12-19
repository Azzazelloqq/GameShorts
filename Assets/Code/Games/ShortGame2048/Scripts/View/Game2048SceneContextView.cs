using SceneContext;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Games
{
    internal class Game2048SceneContextView : SceneContextView
    {
        [SerializeField] 
        private Transform _gameSpawnPoint;
        
        [SerializeField] 
        private Transform _finishGamePoint;
        
        [SerializeField]
        private Game2048InputAreaView _inputAreaView;
        
        [SerializeField]
        private GameObject _cubePrefab;
        
        [Header("Game Settings")]
        
        [SerializeField]
        private float _launchForce = 10f;
        
        [SerializeField]
        private float _spawnDelay = 1f;
        
        [Header("Movement Constraints")]
        
        [SerializeField]
        private float _minX = -1f;
        
        [SerializeField]
        private float _maxX = 1f;
        
        [Header("Merge Settings")]
        
        [SerializeField]
        private float _mergeUpwardForce = 5f;
        
        [SerializeField]
        private float _mergeForwardForce = 8f;
        
        [Header("Pause UI")]
        
        [SerializeField]
        private Button _pauseButton;
        
        [SerializeField]
        private GameObject _pausePanel;
        
        [Header("Main UI")]
        
        [SerializeField]
        private Game2048MainUIView _mainUIView;
        
        [Header("Finish UI")]
        
        [SerializeField]
        private Game2048FinishScreenView _finishScreenView;
        
        [SerializeField] 
        private Game2048StartScreenView _startScreenView;

        public Transform GameSpawnPoint => _gameSpawnPoint;
        public Game2048InputAreaView InputAreaView => _inputAreaView;
        public GameObject CubePrefab => _cubePrefab;
        public float LaunchForce => _launchForce;
        public float SpawnDelay => _spawnDelay;
        public float MinX => _minX;
        public float MaxX => _maxX;
        public float MergeUpwardForce => _mergeUpwardForce;
        public float MergeForwardForce => _mergeForwardForce;
        public Button PauseButton => _pauseButton;
        public GameObject PausePanel => _pausePanel;
        public Game2048FinishScreenView FinishScreenView => _finishScreenView;
        public Game2048MainUIView MainUIView => _mainUIView;
        public Transform FinishGamePoint => _finishGamePoint;
        public Game2048StartScreenView StartScreenView => _startScreenView;
    }
}

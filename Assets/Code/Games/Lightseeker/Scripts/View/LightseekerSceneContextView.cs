using System.Collections.Generic;
using SceneContext;
using UnityEngine;

namespace Lightseeker
{
    internal class LightseekerSceneContextView : SceneContextView
    {
        [Header("Player Settings")]
        
        [SerializeField]
        private LightseekerPlayerView _playerPrefab;
        
        [SerializeField]
        private FloatingJoystick _joystick;
        
        [Header("Level Settings")]
        [SerializeField]
        private List<LevelSection> _levelSections = new List<LevelSection>();
        
        [SerializeField] private GameObject _starPrefab;
        [SerializeField] private Transform _starPlaceholder;
        
        [Header("UI")]
        [SerializeField]
        private LightseekerMainUIView _mainUIView;

        public LightseekerPlayerView PlayerPrefab => _playerPrefab;
        public FloatingJoystick Joystick => _joystick;
        public IReadOnlyList<LevelSection> LevelSections => _levelSections;
        public GameObject StarPrefab => _starPrefab;

        public Transform StarPlaceholder => _starPlaceholder;
        public LightseekerMainUIView MainUIView => _mainUIView;
    }
}


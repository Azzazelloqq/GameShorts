using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.FlyHumans.Gameplay;
using GameShorts.FlyHumans.UI;
using UnityEngine;

namespace GameShorts.FlyHumans.View
{
    internal class FlyHumansSceneContextView : BaseMonoBehaviour
    {
        [Header("Player")]
        [SerializeField] private CharacterView _character;
        
        [Header("World")]
        [SerializeField] private WorldBlocksView _worldBlocksView;
        
        [Header("Camera")]
        [SerializeField] private CameraView _cameraView;
        
        [Header("UI")]
        [SerializeField] private Canvas _uiCanvas;
        [SerializeField] private FlyHumansStartUIView _startUIView;
        [SerializeField] private FlyHumansMainUIView _mainUIView;
        
        // Properties
        public CharacterView Character => _character;
        public WorldBlocksView WorldBlocksView => _worldBlocksView;
        public CameraView CameraView => _cameraView;
        public Canvas UiCanvas => _uiCanvas;
        public FlyHumansStartUIView StartUIView => _startUIView;
        public FlyHumansMainUIView MainUIView => _mainUIView;
    }
}


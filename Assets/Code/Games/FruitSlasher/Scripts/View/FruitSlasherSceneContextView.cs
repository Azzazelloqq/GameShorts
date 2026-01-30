using Code.Games.FruitSlasher.Scripts.Input;
using Disposable;
using TMPro;
using UnityEngine;

namespace Code.Games.FruitSlasher.Scripts.View
{
    internal class FruitSlasherSceneContextView: MonoBehaviourDisposable
    {
        [Header("Camera")]
        [SerializeField] private Camera _mainCamera;
        
        [Header("UI")]
        [SerializeField] private Canvas _uiCanvas;
        [SerializeField] private TextMeshProUGUI _score;
        
        [SerializeField] private FruitSpawnerView  _fruitSpawnerView;
        [SerializeField] private InputAreaView  _inputAreaView;
        [SerializeField] private BladeView  _bladeView;
        
        public Canvas UiCanvas => _uiCanvas;
        public Camera MainCamera => _mainCamera;

        public TextMeshProUGUI Score => _score;
        public FruitSpawnerView FruitSpawnerView => _fruitSpawnerView;

        public InputAreaView InputAreaView => _inputAreaView;

        public BladeView BladeView => _bladeView;
    }
}
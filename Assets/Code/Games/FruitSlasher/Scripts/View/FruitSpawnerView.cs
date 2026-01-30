using Disposable;
using Unity.VisualScripting;
using UnityEngine;

namespace Code.Games.FruitSlasher.Scripts.View
{
    public class FruitSpawnerView : MonoBehaviourDisposable
    {
        [SerializeField] private Collider _spawner;
        [SerializeField] private FruitInfo[] _fruitsPrefabs;
        [SerializeField] private float _minDelay;
        [SerializeField] private float _maxDelay;
        
        [SerializeField] private float _minAngle;
        [SerializeField] private float _maxAngle;
        
        [SerializeField] private float _minForce;
        [SerializeField] private float _maxForce;
        
        [SerializeField] private float _lifeTime;

        

        public Collider Spawner => _spawner;
        public FruitInfo[] FruitsPrefabs => _fruitsPrefabs;
        public float MinDelay => _minDelay;
        public float MaxDelay => _maxDelay;
        public float MinAngle => _minAngle;
        public float MaxAngle => _maxAngle;
        public float MinForce => _minForce;
        public float MaxForce => _maxForce;
        public float LifeTime => _lifeTime;
    }
    
    [System.Serializable]
    public struct FruitInfo
    {
        public GameObject FruitPrefab;
        public int FruitPoint;
    }
}
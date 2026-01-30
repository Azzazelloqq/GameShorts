using System;
using Disposable;
using UnityEngine;

namespace Code.Games.FruitSlasher.Scripts.View
{
    public class FruitView: MonoBehaviourDisposable
    {
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Collider _fruitCollider;
        [SerializeField] private GameObject _whole;
        [SerializeField] private GameObject _sliced;
        [SerializeField] private Rigidbody[] _slicedRigidbody;
        [SerializeField] private ParticleSystem _particles;
        
        public Rigidbody Rigidbody => _rigidbody;

        public GameObject Whole => _whole;
        public GameObject Sliced => _sliced;

        public Rigidbody[] SlicedRigidbody => _slicedRigidbody;

        public Collider FruitCollider => _fruitCollider;

        public ParticleSystem Particles => _particles;

        public Action Slicing;
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;
            Slicing.Invoke();
        }

        public void Reset()
        {
            _whole.SetActive(true);
            _sliced.SetActive(false);
            _particles.gameObject.SetActive(false);
            FruitCollider.enabled = true;
            foreach (var slicedRig in SlicedRigidbody)
            {
                slicedRig.gameObject.transform.localPosition = Vector3.zero;
                slicedRig.gameObject.transform.localRotation= Quaternion.identity;
                slicedRig.linearVelocity =  Vector3.zero;
            }
        }
    }
}
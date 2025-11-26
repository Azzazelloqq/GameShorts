using System.Collections;
using Code.Core.BaseDMDisposable.Scripts;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace GameShorts.CubeRunner.View
{
    internal class CubeView : BaseMonoBehaviour
    {
        public struct Ctx
        {
            public Vector3 scale;
        }
        
        [SerializeField]
        private Transform _visualRoot;
        
        [SerializeField]
        private BoxCollider _boxCollider;
        
        [SerializeField]
        private Rigidbody _rigidbody;
        
        private Ctx _ctx;
        private Vector3 _logicalLocalPosition;
        private Vector3 _cubeDimensions;
        private bool _isGrounded;

        public bool IsGrounded => _isGrounded;

        public Transform VisualRoot =>  transform;
        
        public BoxCollider Collider => _boxCollider;
        
        public Bounds ColliderBounds => _boxCollider != null
            ? _boxCollider.bounds
            : new Bounds(transform.position, Vector3.zero);

        public Rigidbody Rigidbody => _rigidbody;

        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
        }

        public void SetCubeDimensions(Vector3 cubeDimensions)
        {
            UpdateCubeDimensions(cubeDimensions);
        }

        public void SetActiveControl(bool isActive)
        {
            _rigidbody.freezeRotation = !isActive;
            IsCollisionTrigger(!isActive);
        }

        public void IsCollisionTrigger(bool isTrigger)
        {
            _boxCollider.isTrigger = isTrigger;
        }

        void OnCollisionEnter(Collision theCollision)
        {
            var tileView = theCollision.gameObject.GetComponent<TileView>();
            _isGrounded = tileView != null;
           // _rigidbody.freezeRotation = !_isGrounded;
        }

        private void UpdateCubeDimensions(Vector3 dimensions)
        {
            _cubeDimensions = new Vector3(
                Mathf.Max(0.01f, dimensions.x),
                Mathf.Max(0.01f, dimensions.y),
                Mathf.Max(0.01f, dimensions.z));
                
            VisualRoot.localScale = _cubeDimensions;
        }

    }
}


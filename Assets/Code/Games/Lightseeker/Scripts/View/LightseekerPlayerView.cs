using UnityEngine;

namespace Lightseeker
{
    internal class LightseekerPlayerView : MonoBehaviour
    {
        [SerializeField] 
        private CharacterController _characterController;
        
        [SerializeField] 
        private Transform _visualTransform;

        [SerializeField] private Animator _animator;

        public CharacterController CharacterController => _characterController;
        public Transform VisualTransform => _visualTransform;
        public Animator Animator => _animator;

        private void OnTriggerEnter(Collider other)
        {
            var starView = other.GetComponent<StarView>();
            if (starView != null)
            {
                starView.Collect();
            }
        }
    }
}


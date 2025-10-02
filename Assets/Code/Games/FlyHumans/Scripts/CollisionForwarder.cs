using UnityEngine;

namespace GameShorts.AngryFlier
{
    public class CollisionForwarder: MonoBehaviour
    {
        private RagdollRoot _root;

        void Awake() => _root = GetComponentInParent<RagdollRoot>();

        void OnCollisionEnter(Collision c)  => _root?.HandleCollisionEnter(c, this);
        void OnCollisionStay(Collision c)   => _root?.HandleCollisionStay(c, this);
        void OnCollisionExit(Collision c)   => _root?.HandleCollisionExit(c, this);

        void OnTriggerEnter(Collider other) => _root?.HandleTriggerEnter(other, this);
        void OnTriggerExit(Collider other)  => _root?.HandleTriggerExit(other, this);
    }
}
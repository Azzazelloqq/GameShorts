using System;
using UnityEngine;

namespace GameShorts.AngryFlier
{
    public class RagdollRoot: MonoBehaviour
    {
        public Action CollisionEnter;
        public void HandleCollisionEnter(Collision c, Component fromPart)
        {
            if (!c.gameObject.CompareTag("Player"))
                CollisionEnter?.Invoke();
        }

        public void HandleCollisionStay(Collision c, Component fromPart) { }
        public void HandleCollisionExit(Collision c, Component fromPart) { }

        public void HandleTriggerEnter(Collider other, Component fromPart) { }
        public void HandleTriggerExit(Collider other, Component fromPart) { }
    }
}
using System;
using UnityEngine;

namespace GameShorts.FlyHumans.Gameplay
{
    internal class BaseCollisionHandler : MonoBehaviour
    {
        public Action OnCollision;

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Player"))
            {
                OnCollision?.Invoke();
            }
        }
    }
}


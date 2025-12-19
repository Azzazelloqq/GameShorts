using System;
using Disposable;
using UnityEngine;

namespace GameShorts.CubeRunner.View
{
    public class BorderView: MonoBehaviourDisposable
    {
        public Action PlayerDetected;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.CompareTag("Player"))
                PlayerDetected?.Invoke();
        }
    }
}
using System;
using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;

namespace GameShorts.CubeRunner.View
{
    public class BorderView: BaseMonoBehaviour
    {
        public Action PlayerDetected;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.transform.CompareTag("Player"))
                PlayerDetected?.Invoke();
        }
    }
}
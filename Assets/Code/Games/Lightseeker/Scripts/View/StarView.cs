using System;
using UnityEngine;

namespace Lightseeker
{
    internal class StarView : MonoBehaviour
    {
        public event Action<StarView> OnCollected;

        public void Collect()
        {
            OnCollected?.Invoke(this);
            gameObject.SetActive(false);
        }

        public void Reset()
        {
            gameObject.SetActive(true);
        }
    }
}


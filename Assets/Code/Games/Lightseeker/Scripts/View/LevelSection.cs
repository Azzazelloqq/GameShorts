using System.Collections.Generic;
using UnityEngine;

namespace Lightseeker
{
    internal class LevelSection : MonoBehaviour
    {
        [SerializeField]
        private List<Transform> _spawnPoints = new List<Transform>();

        public IReadOnlyList<Transform> SpawnPoints => _spawnPoints;

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}


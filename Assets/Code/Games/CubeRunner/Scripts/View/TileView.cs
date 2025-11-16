using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;

namespace GameShorts.CubeRunner.View
{
    public class TileView : BaseMonoBehaviour
    {
        [SerializeField]
        private MeshRenderer _meshRenderer;

        public Vector3 WorldPosition
        {
            get => transform.position;
            set => transform.position = value;
        }

        public Vector3 LocalPosition
        {
            get => transform.localPosition;
            set => transform.localPosition = value;
        }

        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        public void SetColor(Color color)
        {
            if (_meshRenderer != null)
            {
                _meshRenderer.material.color = color;
            }
        }
    }
}


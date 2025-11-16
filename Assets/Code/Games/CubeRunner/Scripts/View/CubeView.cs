using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;

namespace GameShorts.CubeRunner.View
{
    public class CubeView : BaseMonoBehaviour
    {
        [SerializeField]
        private Transform _visualRoot;

        public Transform VisualRoot => _visualRoot != null ? _visualRoot : transform;

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

        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void SetScale(Vector3 scale)
        {
            VisualRoot.localScale = scale;
        }
    }
}


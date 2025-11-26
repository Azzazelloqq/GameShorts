using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;

namespace GameShorts.CubeRunner.View
{
    public class TileView : BaseMonoBehaviour
    {
        [SerializeField]
        private MeshRenderer _meshRenderer;

        public MeshRenderer MeshRenderer => _meshRenderer;
        private bool _isPlayerEnter;
        private bool _isExitTile;

        public bool IsExitTile
        {
            get => _isExitTile;
            set
            {
                if (value)
                {
                    var color = value ? Color.red : Color.white;
                    color.a = value ? 0.5f : 1f;
                    SetColor(color);
                    
                }
                _isExitTile = value;
            }
        }
        public bool IsPlayerEnter
        {
            get => _isPlayerEnter;
            private set
            {
                if (_isExitTile)
                {
                    var color = value ? Color.lawnGreen : Color.red;
                    color.a = 0.5f;
                    SetColor(color);
                }
                _isPlayerEnter = value;
            }
        }

        public void SetColor(Color color)
        {
            if (_meshRenderer != null)
            {
                _meshRenderer.material.color = color;
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            IsPlayerEnter = collision.transform.CompareTag("Player");
        }
        
        private void OnCollisionExit(Collision collision)
        {
            IsPlayerEnter = false;
        }

        public void ResetView()
        {
            SetColor(Color.white);
        }
    }
}


using System;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Games._2048.Scripts.Models;
using Code.Games._2048.Scripts.View;
using UnityEngine;

namespace Code.Games._2048.Scripts.Presenters
{
    internal class CubePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CubeModel model;
            public GameObject cubePrefab;
            public Transform spawnPoint;
        }

        private readonly Ctx _ctx;
        private Game2048CubeView _view;

        public Guid Id => _ctx.model.id;
        public Game2048CubeView View => _view;

        public CubePm(Ctx ctx)
        {
            _ctx = ctx;
            
            CreateView();
        }

        private void CreateView()
        {
            GameObject cubeObject = UnityEngine.Object.Instantiate(_ctx.cubePrefab, _ctx.spawnPoint.position, _ctx.spawnPoint.rotation);
            _view = cubeObject.GetComponent<Game2048CubeView>();
            
            if (_view == null)
            {
                Debug.LogError("CubePm: Spawned cube doesn't have Game2048CubeView component!");
                UnityEngine.Object.Destroy(cubeObject);
                return;
            }
        }

        protected override void OnDispose()
        {
            if (_view != null && _view.gameObject != null)
            {
                UnityEngine.Object.Destroy(_view.gameObject);
            }
            base.OnDispose();
        }
    }
}

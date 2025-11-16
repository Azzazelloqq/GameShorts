using System.Collections.Generic;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using GameShorts.CubeRunner.Data;
using GameShorts.CubeRunner.View;
using LightDI.Runtime;
using UnityEngine;

namespace GameShorts.CubeRunner.Level
{
    internal class TileManager : BaseDisposable
    {
        internal struct Ctx
        {
            public Transform tilesRoot;
            public CubeRunnerGameSettings gameSettings;
            public IPoolManager poolManager;
        }

        private readonly Ctx _ctx;
        private readonly Dictionary<Vector2Int, TileView> _tiles = new Dictionary<Vector2Int, TileView>();
        private readonly IPoolManager _poolManager;

        public TileManager(Ctx ctx, [Inject] IPoolManager poolManager )
        {
            _ctx = ctx;
            _poolManager = poolManager;
        }

        public bool HasTile(Vector2Int gridPosition)
        {
            return _tiles.ContainsKey(gridPosition);
        }

        public TileView GetTile(Vector2Int gridPosition)
        {
            _tiles.TryGetValue(gridPosition, out var tile);
            return tile;
        }

        public TileView SpawnTile(Vector2Int gridPosition, Vector3 localPosition)
        {
            if (_ctx.gameSettings.TilePrefab == null)
            {
                return null;
            }

            if (_tiles.TryGetValue(gridPosition, out var existing))
            {
                existing.LocalPosition = localPosition;
                existing.SetActive(true);
                return existing;
            }

            GameObject tileObject = _poolManager.Get(_ctx.gameSettings.TilePrefab.gameObject, _ctx.tilesRoot);

            var tileView = tileObject.GetComponent<TileView>();
            if (tileView == null)
            {
                tileView = tileObject.AddComponent<TileView>();
            }

            tileView.LocalPosition = localPosition;
            tileView.SetActive(true);
            _tiles[gridPosition] = tileView;
            return tileView;
        }

        public void RemoveTile(Vector2Int gridPosition)
        {
            if (!_tiles.TryGetValue(gridPosition, out var tileView))
            {
                return;
            }

            _tiles.Remove(gridPosition);
            if (tileView != null)
            {
                tileView.SetActive(false); 
                _poolManager.Return(_ctx.gameSettings.TilePrefab.gameObject, tileView.gameObject);
            }
        }

        public void RemoveTilesBeforeRow(int rowIndexExclusive)
        {
            var toRemove = new List<Vector2Int>();
            foreach (var pair in _tiles)
            {
                if (pair.Key.y < rowIndexExclusive)
                {
                    toRemove.Add(pair.Key);
                }
            }

            foreach (var gridPosition in toRemove)
            {
                RemoveTile(gridPosition);
            }
        }

        protected override void OnDispose()
        {
            var keys = new List<Vector2Int>(_tiles.Keys);
            foreach (var gridPosition in keys)
            {
                RemoveTile(gridPosition);
            }
            _tiles.Clear();
        }
    }
}


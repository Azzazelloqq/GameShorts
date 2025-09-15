using System;
using System.Threading;
using System.Collections.Generic;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using Code.Games.Game2.Scripts.Core;
using LightDI.Runtime;
using UnityEngine;
using R3;

namespace Code.Core.ShortGamesCore.Game2
{
    internal class BoxTowerTowerPm : BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public BoxTowerSceneContextView sceneContextView;
            public TowerModel towerModel;
            public GameModel gameModel;
        }

        private readonly Ctx _ctx;
        private IPoolManager _poolManager;

        private List<GameObject> _placedBlocks = new List<GameObject>();
        private List<GameObject> _activeChunks = new List<GameObject>();
        private List<GameObject> _fallingBlocks = new List<GameObject>(); // Track falling blocks for cleanup
        private GameObject _currentMovingBlock;
        private BlockMover _currentMover;

        public BoxTowerTowerPm(Ctx ctx,
            [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            // Subscribe to model events
            AddDispose(_ctx.gameModel.CurrentState.Subscribe(OnGameStateChanged));
            _ctx.towerModel.OnChunkCreated += CreateChunk;
            
            // Subscribe to scene updates
            _ctx.sceneContextView.OnUpdated += OnSceneUpdate;
        }

        private void OnGameStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Ready:
                    PrepareGame();
                    break;
                case GameState.Running:
                    StartGame();
                    break;
                case GameState.GameOver:
                    StopGame();
                    break;
            }
        }

        private void PrepareGame()
        {
            ClearTower();
        }

        private void StartGame()
        {
            SpawnBaseBlock();
            SpawnNextMovingBlock();
        }

        private void StopGame()
        {
            if (_currentMover != null)
            {
                _currentMover.StopMoving();
            }
        }

        private void ClearTower()
        {
            // Return all placed blocks to pool
            foreach (var block in _placedBlocks)
            {
                if (block != null)
                {
                    _poolManager.Return(_ctx.sceneContextView.BlockPrefab, block);
                }
            }
            _placedBlocks.Clear();

            // Return current moving block to pool
            if (_currentMovingBlock != null)
            {
                _poolManager.Return(_ctx.sceneContextView.BlockPrefab, _currentMovingBlock);
                _currentMovingBlock = null;
                _currentMover = null;
            }

            // Return all falling blocks to pool immediately
            ReturnAllFallingBlocks();

            // Return all active chunks to pool
            ReturnAllChunks();
            
            // Force clear any remaining children in TowerRoot (safety cleanup)
            if (_ctx.sceneContextView.TowerRoot != null)
            {
                for (int i = _ctx.sceneContextView.TowerRoot.childCount - 1; i >= 0; i--)
                {
                    var child = _ctx.sceneContextView.TowerRoot.GetChild(i);
                    if (child != null)
                    {
                        // Try to return to pool first
                        _poolManager.Return(_ctx.sceneContextView.BlockPrefab, child.gameObject);
                    }
                }
            }

            // Also clean up any falling objects in the scene that might not be children of TowerRoot
            ClearAllFallingObjects();
            
            // Reset background color
            if (_ctx.sceneContextView.ColorManager != null)
            {
                _ctx.sceneContextView.ColorManager.ResetBackgroundColor();
            }
        }

        private void SpawnBaseBlock()
        {
            Vector3 position = _ctx.sceneContextView.TowerRoot.position;
            GameObject baseBlock = _poolManager.Get(_ctx.sceneContextView.BlockPrefab, position);
            baseBlock.transform.localScale = _ctx.towerModel.BlockSize;
            baseBlock.transform.SetParent(_ctx.sceneContextView.TowerRoot);

            // Remove mover component if exists and add Rigidbody for physics
            if (baseBlock.TryGetComponent<BlockMover>(out var mover))
            {
                UnityEngine.Object.DestroyImmediate(mover);
            }

            if (!baseBlock.TryGetComponent<Rigidbody>(out _))
            {
                var rb = baseBlock.AddComponent<Rigidbody>();
                rb.isKinematic = true; // Base block should be static
            }

            _placedBlocks.Add(baseBlock);

            // Set block color
            if (baseBlock.TryGetComponent<Renderer>(out var renderer))
            {
                var material = renderer.material;
                if (_ctx.sceneContextView.ColorManager != null)
                {
                    material.color = _ctx.sceneContextView.ColorManager.GetBlockColor(0);
                }
            }

            // Set initial block data
            var bounds = renderer.bounds;
            var initialBlockData = new BlockData(baseBlock.transform.position, bounds.size, Axis.X);
            _ctx.towerModel.PlaceBlock(initialBlockData);
            
            // Update background color
            if (_ctx.sceneContextView.ColorManager != null)
            {
                _ctx.sceneContextView.ColorManager.UpdateBackgroundColor(_ctx.towerModel.BlocksPlaced.Value);
            }
        }

        private void SpawnNextMovingBlock()
        {
            Vector3 spawnPosition = _ctx.towerModel.GetNextSpawnPosition();
            
            _currentMovingBlock = _poolManager.Get(_ctx.sceneContextView.BlockPrefab, spawnPosition);
            
            // Set size based on last placed block
            Vector3 newSize = _ctx.towerModel.LastPlacedBlock.Value.size;
            _currentMovingBlock.transform.localScale = newSize;
            
            // Set color for moving block (next block color)
            if (_currentMovingBlock.TryGetComponent<Renderer>(out var renderer))
            {
                var material = renderer.material;
                if (_ctx.sceneContextView.ColorManager != null)
                {
                    material.color = _ctx.sceneContextView.ColorManager.GetBlockColor(_ctx.towerModel.BlocksPlaced.Value);
                }
            }
            
            // Setup mover component
            if (!_currentMovingBlock.TryGetComponent<BlockMover>(out _currentMover))
            {
                _currentMover = _currentMovingBlock.AddComponent<BlockMover>();
            }
            
            // Remove Rigidbody while moving
            if (_currentMovingBlock.TryGetComponent<Rigidbody>(out var rb))
            {
                UnityEngine.Object.DestroyImmediate(rb);
            }
            
            // Start movement
            _currentMover.StartMoving(
                _ctx.towerModel.CurrentAxis.Value, 
                _ctx.towerModel.CurrentSpeed.Value, 
                _ctx.towerModel.MoveLimit);
        }

        public bool TryPlaceCurrentBlock()
        {
            if (_currentMovingBlock == null || _currentMover == null || !_currentMover.IsMoving)
                return false;

            // Stop the current block
            _currentMover.StopMoving();
            
            // Get current block data
            BlockData currentBlockData = _currentMover.GetBlockData();
            
            // Calculate placement result
            PlaceResult result = BlockSlicer.TryPlace(_ctx.towerModel.LastPlacedBlock.Value, currentBlockData);
            
            if (result.success)
            {
                // Place the block successfully
                PlaceBlock(result.placedBlock);
                
                // Update tower model
                _ctx.towerModel.PlaceBlock(result.placedBlock);
                
                // Create chunk if there's overhang
                if (result.hasChunk)
                {
                    _ctx.towerModel.CreateChunk(result.chunkCenter, result.chunkSize);
                }
                
                // Spawn next block
                SpawnNextMovingBlock();
                
                return true;
            }
            else
            {
                // Game over - current block falls
                MakeCurrentBlockFall();
                return false;
            }
        }

        private void PlaceBlock(BlockData blockData)
        {
            // Update block position and scale
            _currentMovingBlock.transform.position = blockData.center;
            _currentMovingBlock.transform.localScale = blockData.size;
            _currentMovingBlock.transform.SetParent(_ctx.sceneContextView.TowerRoot);
            
            // Set block color based on current block count
            if (_currentMovingBlock.TryGetComponent<Renderer>(out var renderer))
            {
                var material = renderer.material;
                if (_ctx.sceneContextView.ColorManager != null)
                {
                    material.color = _ctx.sceneContextView.ColorManager.GetBlockColor(_ctx.towerModel.BlocksPlaced.Value);
                }
            }
            
            // Add Rigidbody and make it kinematic (static)
            var rb = _currentMovingBlock.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            
            // Remove mover component
            if (_currentMover != null)
            {
                UnityEngine.Object.DestroyImmediate(_currentMover);
                _currentMover = null;
            }
            
            _placedBlocks.Add(_currentMovingBlock);
            _currentMovingBlock = null;
            
            // Update background color after placing block
            if (_ctx.sceneContextView.ColorManager != null)
            {
                _ctx.sceneContextView.ColorManager.UpdateBackgroundColor(_ctx.towerModel.BlocksPlaced.Value);
            }
        }

        private void MakeCurrentBlockFall()
        {
            if (_currentMovingBlock == null) return;
            
            // Add Rigidbody for falling physics
            var rb = _currentMovingBlock.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            
            // Remove mover component
            if (_currentMover != null)
            {
                UnityEngine.Object.DestroyImmediate(_currentMover);
                _currentMover = null;
            }
            
            // Track falling block for cleanup
            _fallingBlocks.Add(_currentMovingBlock);
            
            // Return to pool after some time
            var fallingBlock = _currentMovingBlock;
            AddDispose(Observable.Timer(TimeSpan.FromSeconds(3f)).Subscribe(_ => 
            {
                ReturnFallingBlock(fallingBlock);
            }));
            
            _currentMovingBlock = null;
        }

        private void CreateChunk(Vector3 center, Vector3 size)
        {
            if (_ctx.sceneContextView.ChunkPrefab == null) return;

            GameObject chunk = _poolManager.Get(_ctx.sceneContextView.ChunkPrefab, center);
            if (chunk != null)
            {
                chunk.transform.localScale = size;
                _activeChunks.Add(chunk);
                
                // Ensure it has physics components
                if (!chunk.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb = chunk.AddComponent<Rigidbody>();
                }
                rb.isKinematic = false;
                
                // Add some random force for more interesting falling
                Vector3 randomForce = new Vector3(
                    UnityEngine.Random.Range(-2f, 2f), 
                    UnityEngine.Random.Range(0f, 1f), 
                    UnityEngine.Random.Range(-2f, 2f)
                );
                rb.AddForce(randomForce, ForceMode.Impulse);

                // Return chunk to pool after lifetime
                AddDispose(Observable.Timer(TimeSpan.FromSeconds(1.5f)).Subscribe(_ => ReturnChunk(chunk)));
            }
        }

        private void ReturnChunk(GameObject chunk)
        {
            if (chunk == null || !_activeChunks.Contains(chunk)) return;

            _activeChunks.Remove(chunk);
            
            // Reset chunk properties
            var rb = chunk.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            _poolManager.Return(_ctx.sceneContextView.ChunkPrefab, chunk);
        }

        private void ReturnAllChunks()
        {
            var chunksToReturn = new List<GameObject>(_activeChunks);
            foreach (var chunk in chunksToReturn)
            {
                ReturnChunk(chunk);
            }
        }

        private void OnSceneUpdate()
        {
            // Update tower height based on placed blocks
            if (_placedBlocks.Count > 0)
            {
                float maxHeight = 0f;
                foreach (var block in _placedBlocks)
                {
                    if (block != null)
                    {
                        var bounds = block.GetComponent<Renderer>().bounds;
                        float blockTop = bounds.center.y + bounds.size.y * 0.5f;
                        maxHeight = Mathf.Max(maxHeight, blockTop);
                    }
                }
                _ctx.towerModel.TowerHeight.Value = maxHeight;
            }
        }

        private void ReturnFallingBlock(GameObject fallingBlock)
        {
            if (fallingBlock == null || !_fallingBlocks.Contains(fallingBlock)) return;

            _fallingBlocks.Remove(fallingBlock);
            _poolManager.Return(_ctx.sceneContextView.BlockPrefab, fallingBlock);
        }

        private void ReturnAllFallingBlocks()
        {
            var blocksToReturn = new List<GameObject>(_fallingBlocks);
            foreach (var block in blocksToReturn)
            {
                if (block != null)
                {
                    _poolManager.Return(_ctx.sceneContextView.BlockPrefab, block);
                }
            }
            _fallingBlocks.Clear();
        }

        private void ClearAllFallingObjects()
        {
            // Find all objects with Rigidbody that are not kinematic (falling objects)
            var allRigidbodies = UnityEngine.Object.FindObjectsOfType<Rigidbody>();
            
            foreach (var rb in allRigidbodies)
            {
                if (rb != null && !rb.isKinematic && rb.gameObject != null)
                {
                    // Check if it's a block or chunk object that should be returned to pool
                    var gameObj = rb.gameObject;
                    
                    // Try to return as block prefab first
                    if (_ctx.sceneContextView.BlockPrefab != null)
                    {
                        _poolManager.Return(_ctx.sceneContextView.BlockPrefab, gameObj);
                        continue;
                    }
                    
                    // Try to return as chunk prefab
                    if (_ctx.sceneContextView.ChunkPrefab != null)
                    {
                        _poolManager.Return(_ctx.sceneContextView.ChunkPrefab, gameObj);
                        continue;
                    }
                }
            }
        }

        protected override void OnDispose()
        {
            // Unsubscribe from events
            _ctx.sceneContextView.OnUpdated -= OnSceneUpdate;
            if (_ctx.towerModel != null)
            {
                _ctx.towerModel.OnChunkCreated -= CreateChunk;
            }
            
            base.OnDispose();
        }
    }
}

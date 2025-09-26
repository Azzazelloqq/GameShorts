using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using UnityEngine;
using System.Collections.Generic;
using Code.Games.Game2.Scripts.Core;

namespace Code.Core.ShortGamesCore.Game2
{
    internal class BlockSpawner : BaseMonoBehaviour
    {
        [Header("Block Settings")]
        [SerializeField] private GameObject blockPrefab;
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private Vector3 blockSize = new Vector3(3f, 0.5f, 3f);
        
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeedStart = 2f;
        [SerializeField] private float moveSpeedMax = 6f;
        [SerializeField] private float speedIncrement = 0.1f;
        [SerializeField] private int blocksPerSpeedIncrease = 2;
        [SerializeField] private float moveLimit = 5f;

        [Header("Spawn Settings")]
        [SerializeField] private float blockSpacing = 0.5f;
        [SerializeField] private Transform towerRoot;
        [SerializeField] private float chunkLifetime = 1.5f;

        private List<GameObject> placedBlocks = new List<GameObject>();
        private GameObject currentMovingBlock;
        private BlockMover currentMover;
        private Axis currentAxis = Axis.X;
        private float currentSpeed;
        private int blocksPlaced = 0;
        private List<GameObject> activeChunks = new List<GameObject>();

        [Inject] private IPoolManager _poolManager;

        public bool HasMovingBlock => currentMovingBlock != null && currentMover != null && currentMover.IsMoving;
        public BlockData LastPlacedBlockData { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            currentSpeed = moveSpeedStart;
        }

        public void StartSequence()
        {
            ClearTower();
            SpawnBaseBlock();
            SpawnNextMovingBlock();
        }

        private void ClearTower()
        {
            // Return all placed blocks to pool
            foreach (var block in placedBlocks)
            {
                if (block != null) 
                {
                    _poolManager.Return(blockPrefab, block);
                }
            }
            placedBlocks.Clear();
            
            // Return current moving block to pool
            if (currentMovingBlock != null)
            {
                _poolManager.Return(blockPrefab, currentMovingBlock);
                currentMovingBlock = null;
                currentMover = null;
            }

            // Return all active chunks to pool
            ReturnAllChunks();
            
            blocksPlaced = 0;
            currentSpeed = moveSpeedStart;
            currentAxis = Axis.X;
        }

        private void SpawnBaseBlock()
        {
            Vector3 position = towerRoot.position;
            GameObject baseBlock = _poolManager.Get(blockPrefab, position);
            baseBlock.transform.localScale = blockSize;
            baseBlock.transform.SetParent(towerRoot);
            
            // Remove mover component if exists and add Rigidbody for physics
            if (baseBlock.TryGetComponent<BlockMover>(out var mover))
            {
                DestroyImmediate(mover);
            }
            
            if (!baseBlock.TryGetComponent<Rigidbody>(out _))
            {
                var rb = baseBlock.AddComponent<Rigidbody>();
                rb.isKinematic = true; // Base block should be static
            }

            placedBlocks.Add(baseBlock);
            
            // Set initial block data
            var renderer = baseBlock.GetComponent<Renderer>();
            var bounds = renderer.bounds;
            LastPlacedBlockData = new BlockData(baseBlock.transform.position, bounds.size, Axis.X);
        }

        private void SpawnNextMovingBlock()
        {
            // Calculate position above the last placed block
            Vector3 spawnPosition = GetNextSpawnPosition();
            
            currentMovingBlock = _poolManager.Get(blockPrefab, spawnPosition);
            
            // Set size based on last placed block
            Vector3 newSize = LastPlacedBlockData.size;
            currentMovingBlock.transform.localScale = newSize;
            
            // Setup mover component
            if (!currentMovingBlock.TryGetComponent<BlockMover>(out currentMover))
            {
                currentMover = currentMovingBlock.AddComponent<BlockMover>();
            }
            
            // Remove Rigidbody while moving
            if (currentMovingBlock.TryGetComponent<Rigidbody>(out var rb))
            {
                DestroyImmediate(rb);
            }
            
            // Start movement
            currentMover.StartMoving(currentAxis, currentSpeed, moveLimit);
        }

        private Vector3 GetNextSpawnPosition()
        {
            float height = LastPlacedBlockData.center.y + LastPlacedBlockData.size.y * 0.5f + blockSize.y * 0.5f + blockSpacing;
            return new Vector3(LastPlacedBlockData.center.x, height, LastPlacedBlockData.center.z);
        }

        public PlaceResult TryPlaceCurrent()
        {
            if (!HasMovingBlock) return new PlaceResult(false);

            // Stop the current block
            currentMover.StopMoving();
            
            // Get current block data
            BlockData currentBlockData = currentMover.GetBlockData();
            
            // Calculate placement result
            PlaceResult result = BlockSlicer.TryPlace(LastPlacedBlockData, currentBlockData);
            
            if (result.success)
            {
                // Place the block successfully
                PlaceBlock(result.placedBlock);
                
                // Update for next block
                LastPlacedBlockData = result.placedBlock;
                blocksPlaced++;
                
                // Switch axis for next block
                currentAxis = (currentAxis == Axis.X) ? Axis.Z : Axis.X;
                
                // Increase speed periodically
                if (blocksPlaced % blocksPerSpeedIncrease == 0)
                {
                    currentSpeed = Mathf.Min(currentSpeed + speedIncrement, moveSpeedMax);
                }
                
                // Spawn next block
                SpawnNextMovingBlock();
            }
            else
            {
                // Game over - current block falls
                MakeCurrentBlockFall();
            }
            
            return result;
        }

        private void PlaceBlock(BlockData blockData)
        {
            // Update block position and scale
            currentMovingBlock.transform.position = blockData.center;
            currentMovingBlock.transform.localScale = blockData.size;
            currentMovingBlock.transform.SetParent(towerRoot);
            
            // Add Rigidbody and make it kinematic (static)
            var rb = currentMovingBlock.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            
            // Remove mover component
            if (currentMover != null)
            {
                DestroyImmediate(currentMover);
                currentMover = null;
            }
            
            placedBlocks.Add(currentMovingBlock);
            currentMovingBlock = null;
        }

        private void MakeCurrentBlockFall()
        {
            if (currentMovingBlock == null) return;
            
            // Add Rigidbody for falling physics
            var rb = currentMovingBlock.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            
            // Remove mover component
            if (currentMover != null)
            {
                DestroyImmediate(currentMover);
                currentMover = null;
            }
            
            // The block will fall due to gravity
            // We don't add it to placedBlocks since it's not successfully placed
            // Return to pool after some time
            StartCoroutine(ReturnBlockToPoolAfterDelay(currentMovingBlock, 3f));
            currentMovingBlock = null;
        }

        public void CreateChunk(Vector3 center, Vector3 size)
        {
            if (chunkPrefab == null) return;

            GameObject chunk = _poolManager.Get(chunkPrefab, center);
            if (chunk != null)
            {
                chunk.transform.localScale = size;
                activeChunks.Add(chunk);
                
                // Ensure it has physics components
                if (!chunk.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb = chunk.AddComponent<Rigidbody>();
                }
                rb.isKinematic = false;
                
                // Add some random force for more interesting falling
                Vector3 randomForce = new Vector3(
                    Random.Range(-2f, 2f), 
                    Random.Range(0f, 1f), 
                    Random.Range(-2f, 2f)
                );
                rb.AddForce(randomForce, ForceMode.Impulse);

                // Return chunk to pool after lifetime
                StartCoroutine(ReturnChunkToPoolAfterDelay(chunk, chunkLifetime));
            }
        }

        private System.Collections.IEnumerator ReturnChunkToPoolAfterDelay(GameObject chunk, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnChunk(chunk);
        }

        private System.Collections.IEnumerator ReturnBlockToPoolAfterDelay(GameObject block, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (block != null)
            {
                _poolManager.Return(blockPrefab, block);
            }
        }

        private void ReturnChunk(GameObject chunk)
        {
            if (chunk == null || !activeChunks.Contains(chunk)) return;

            activeChunks.Remove(chunk);
            
            // Reset chunk properties
            var rb = chunk.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            _poolManager.Return(chunkPrefab, chunk);
        }

        public void ReturnAllChunks()
        {
            var chunksToReturn = new List<GameObject>(activeChunks);
            foreach (var chunk in chunksToReturn)
            {
                ReturnChunk(chunk);
            }
        }

        public float GetTowerHeight()
        {
            if (placedBlocks.Count == 0) return 0f;
            
            float maxHeight = 0f;
            foreach (var block in placedBlocks)
            {
                if (block != null)
                {
                    var bounds = block.GetComponent<Renderer>().bounds;
                    float blockTop = bounds.center.y + bounds.size.y * 0.5f;
                    maxHeight = Mathf.Max(maxHeight, blockTop);
                }
            }
            return maxHeight;
        }
    }
}

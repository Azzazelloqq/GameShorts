using System.Collections.Generic;
using UnityEngine;

namespace GameShorts.FlyHumans.View
{
    /// <summary>
    /// View компонент для блоков мира - только визуализация и данные
    /// </summary>
    public class WorldBlocksView : MonoBehaviour
    {
        [Header("Block Settings")]
        [SerializeField] private WorldBlock _startBlock;
        [SerializeField] private List<GameObject> _blockPrefabs = new List<GameObject>();
        [SerializeField] private Transform _blocksContainer;
        
        [Header("Movement Settings")]
        [SerializeField] private float _worldSpeed = 5f;
        [SerializeField] private float _removalDistance = 30f; // Расстояние позади персонажа для удаления блока
        
        private List<WorldBlock> _activeBlocks = new List<WorldBlock>();
        
        // Properties (только данные)
        public WorldBlock StartBlock => _startBlock;
        public List<GameObject> BlockPrefabs => _blockPrefabs;
        public Transform BlocksContainer => _blocksContainer;
        public float WorldSpeed => _worldSpeed;
        public float RemovalDistance => _removalDistance;
        public List<WorldBlock> ActiveBlocks => _activeBlocks;
        
        public void Initialize()
        {
            // Если контейнер не задан, создаем
            if (_blocksContainer == null)
            {
                _blocksContainer = new GameObject("BlocksContainer").transform;
                _blocksContainer.SetParent(transform);
            }
            
            // Добавляем стартовый блок в список активных
            if (_startBlock != null && !_activeBlocks.Contains(_startBlock))
            {
                _activeBlocks.Add(_startBlock);
            }
        }
        
        /// <summary>
        /// Двигает блок на указанную дистанцию
        /// </summary>
        public void MoveBlock(WorldBlock block, Vector3 movement)
        {
            if (block != null)
            {
                block.transform.position += movement;
            }
        }
        
        /// <summary>
        /// Добавляет блок в список активных
        /// </summary>
        public void AddActiveBlock(WorldBlock block)
        {
            if (block != null && !_activeBlocks.Contains(block))
            {
                _activeBlocks.Add(block);
            }
        }
        
        /// <summary>
        /// Удаляет блок из списка активных
        /// </summary>
        public void RemoveActiveBlock(WorldBlock block)
        {
            if (block != null && _activeBlocks.Contains(block))
            {
                _activeBlocks.Remove(block);
            }
        }
        
        /// <summary>
        /// Очищает список активных блоков (кроме стартового)
        /// </summary>
        public void ClearActiveBlocks()
        {
            _activeBlocks.Clear();
            
            // Возвращаем стартовый блок
            if (_startBlock != null)
            {
                _activeBlocks.Add(_startBlock);
            }
        }
        
        private void OnDrawGizmos()
        {
            // Визуализация направления движения мира
            if (_activeBlocks.Count > 0 && _activeBlocks[0] != null)
            {
                Vector3 start = _activeBlocks[0].transform.position;
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(start, start + Vector3.back * 5f);
            }
        }
    }
}


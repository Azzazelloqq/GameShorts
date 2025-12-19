using System.Collections.Generic;
using Code.Core.Tools.Pool;
using Disposable;
using GameShorts.FlyHumans.Logic;
using GameShorts.FlyHumans.View;
using LightDI.Runtime;
using UnityEngine;

namespace GameShorts.FlyHumans.Presenters
{
    /// <summary>
    /// Презентер для управления блоками мира
    /// </summary>
    internal class WorldBlocksPm : DisposableBase
    {
        internal struct Ctx
        {
            public WorldBlocksView worldBlocksView;
            public Transform characterTransform; // Для отслеживания позиции персонажа
        }

        private readonly Ctx _ctx;
        private Vector3 _movementDirection = Vector3.back; // Мир движется назад
        private bool _isMoving = false;
        private float _currentSpeed;
        private float _targetSpeed;
        private float _speedAcceleration = 10f; // Скорость ускорения в единицах в секунду (быстрое ускорение)
        private readonly IPoolManager _poolManager;
        private readonly Dictionary<WorldBlock, WorldBlockModel> _blockModels = new Dictionary<WorldBlock, WorldBlockModel>();
        private Vector3 _startBlockInitialPosition;

        public bool IsMoving 
        { 
            get => _isMoving; 
            set => _isMoving = value; 
        }
        
        public float CurrentSpeed => _currentSpeed;

        public WorldBlocksPm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            Initialize();
        }

        private void Initialize()
        {
            if (_ctx.worldBlocksView != null)
            {
                _ctx.worldBlocksView.Initialize();
                _currentSpeed = 0f; // Начинаем с нулевой скорости
                _targetSpeed = _ctx.worldBlocksView.WorldSpeed;
                
                // Создаем модель для стартового блока, если он есть
                if (_ctx.worldBlocksView.StartBlock != null)
                {
                    // Сохраняем начальную позицию стартового блока
                    _startBlockInitialPosition = _ctx.worldBlocksView.StartBlock.transform.position;
                    
                    var startBlockModel = new WorldBlockModel(_ctx.worldBlocksView.StartBlock, _poolManager);
                    _blockModels[_ctx.worldBlocksView.StartBlock] = startBlockModel;
                }
            }
        }

        /// <summary>
        /// Обновление мира (вызывается каждый кадр)
        /// </summary>
        public void UpdateWorld(float deltaTime)
        {
            if (_ctx.worldBlocksView == null) return;
            
            // Постепенно увеличиваем скорость до целевой
            if (_currentSpeed < _targetSpeed)
            {
                _currentSpeed = Mathf.Min(_currentSpeed + _speedAcceleration * deltaTime, _targetSpeed);
                Debug.Log($"Accelerating: Current={_currentSpeed:F2}, Target={_targetSpeed:F2}");
            }
            else if (_currentSpeed > _targetSpeed)
            {
                _currentSpeed = Mathf.Max(_currentSpeed - _speedAcceleration * deltaTime, _targetSpeed);
            }
            
            // Двигаем все блоки только если движение включено
            if (_isMoving)
            {
                MoveBlocks(deltaTime);
                
                // Проверяем, нужно ли спавнить новый блок
                CheckAndSpawnNextBlock();
                
                // Удаляем блоки, которые ушли за персонажа
                RemoveOldBlocks();
            }
            else
            {
                Debug.LogWarning($"World not moving! IsMoving={_isMoving}");
            }
            
            // Обновляем трафик на всех блоках ВСЕГДА (независимо от движения мира)
            UpdateAllBlocksTraffic(deltaTime);
        }

        private void UpdateAllBlocksTraffic(float deltaTime)
        {
            foreach (var blockModel in _blockModels.Values)
            {
                blockModel.UpdateTraffic(deltaTime);
            }
        }

        private void MoveBlocks(float deltaTime)
        {
            Vector3 movement = _movementDirection * _currentSpeed * deltaTime;
            Debug.Log($"MoveBlocks: Speed={_currentSpeed:F2}, Movement={movement}, BlockCount={_ctx.worldBlocksView.ActiveBlocks.Count}");
            
            foreach (var block in _ctx.worldBlocksView.ActiveBlocks)
            {
                _ctx.worldBlocksView.MoveBlock(block, movement);
            }
        }

        private void CheckAndSpawnNextBlock()
        {
            if (_ctx.characterTransform == null || _ctx.worldBlocksView.ActiveBlocks.Count == 0) 
                return;
            
            // Проверяем последний блок
            int lastIndex = _ctx.worldBlocksView.ActiveBlocks.Count - 1;
            WorldBlock lastBlock = _ctx.worldBlocksView.ActiveBlocks[lastIndex];
            
            if (lastBlock != null && lastBlock.ShouldSpawnNext(_ctx.characterTransform.position))
            {
                SpawnNextBlock(lastBlock);
            }
        }

        private void SpawnNextBlock(WorldBlock previousBlock)
        {
            if (_ctx.worldBlocksView.BlockPrefabs.Count == 0)
            {
                Debug.LogWarning("No block prefabs available to spawn!");
                return;
            }
            
            // Выбираем случайный префаб
            int randomIndex = Random.Range(0, _ctx.worldBlocksView.BlockPrefabs.Count);
            var prefab = _ctx.worldBlocksView.BlockPrefabs[randomIndex];
            
            // Спавним блок из пула
            var newBlockObj = _poolManager.Get(prefab.gameObject, _ctx.worldBlocksView.BlocksContainer);
            var newBlock = newBlockObj.GetComponent<WorldBlock>();
            
            // ВАЖНО: Сбрасываем состояние блока после получения из пула
            newBlock.HasSpawnedNext = false;
            newBlock.transform.localScale = Vector3.one;
            newBlock.transform.rotation = Quaternion.identity;
            
            // Выравниваем начальную точку нового блока с конечной точкой предыдущего
            if (previousBlock.EndPoint != null && newBlock.StartPoint != null)
            {
                Vector3 targetPosition = previousBlock.EndPoint.position;
                newBlock.AlignStartPointTo(targetPosition);
                
                // Проверяем правильность выравнивания
                float alignmentError = Vector3.Distance(newBlock.StartPoint.position, targetPosition);
                if (alignmentError > 0.01f)
                {
                    Debug.LogWarning($"[Spawn] Alignment error: {alignmentError}. Check attachment points on prefabs!");
                }
                
                Debug.Log($"[Spawn] Spawned '{newBlock.gameObject.name}' after '{previousBlock.gameObject.name}' at position {newBlock.transform.position}");
            }
            
            // Создаем модель блока с трафиком
            var blockModel = WorldBlockModelFactory.CreateWorldBlockModel(newBlock);
            _blockModels[newBlock] = blockModel;
            
            // Добавляем в список активных
            _ctx.worldBlocksView.AddActiveBlock(newBlock);
            
            // Помечаем, что предыдущий блок уже заспавнил следующий
            previousBlock.HasSpawnedNext = true;
        }

        private void RemoveOldBlocks()
        {
            if (_ctx.characterTransform == null || _ctx.worldBlocksView == null) return;
            
            // Удаляем блоки, которые находятся слишком далеко позади персонажа
            var activeBlocks = _ctx.worldBlocksView.ActiveBlocks;
            
            for (int i = activeBlocks.Count - 1; i >= 0; i--)
            {
                WorldBlock block = activeBlocks[i];
                
                if (block != null && block != _ctx.worldBlocksView.StartBlock)
                {
                    // Проверяем расстояние от конца блока до персонажа
                    if (block.EndPoint != null)
                    {
                        float distanceBehind = (_ctx.characterTransform.position - block.EndPoint.position).z;
                        
                        if (distanceBehind > _ctx.worldBlocksView.RemovalDistance)
                        {
                            Debug.Log($"[RemoveOldBlocks] Removing block at distance: {distanceBehind}");
                            
                            // Очищаем модель блока и его трафик
                            if (_blockModels.TryGetValue(block, out var blockModel))
                            {
                                blockModel.Dispose();
                                _blockModels.Remove(block);
                            }
                            
                            // ВАЖНО: Сначала удаляем из списка активных, потом возвращаем в пул
                            _ctx.worldBlocksView.RemoveActiveBlock(block);
                            _poolManager.Return(block.gameObject, block.gameObject);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Сброс мира в начальное состояние
        /// </summary>
        public void ResetWorld()
        {
            if (_ctx.worldBlocksView == null) return;
            
            // Останавливаем движение
            _isMoving = false;
            
            // Сбрасываем скорость до нуля
            _currentSpeed = 0f;
            
            // Удаляем все заспавненные блоки (кроме стартового)
            var activeBlocks = _ctx.worldBlocksView.ActiveBlocks;
            for (int i = activeBlocks.Count - 1; i >= 0; i--)
            {
                WorldBlock block = activeBlocks[i];
                if (block != null && block != _ctx.worldBlocksView.StartBlock)
                {
                    // Очищаем модель блока и его трафик при удалении блока
                    if (_blockModels.TryGetValue(block, out var blockModel))
                    {
                        Debug.Log($"Disposing traffic for removed block: {block.gameObject.name}");
                        blockModel.Dispose();
                        _blockModels.Remove(block);
                    }
                    
                    // Удаляем из списка активных
                    _ctx.worldBlocksView.RemoveActiveBlock(block);
                    
                    // Возвращаем в пул
                    _poolManager.Return(block.gameObject, block.gameObject);
                }
            }
            
            // Сбрасываем состояние стартового блока
            if (_ctx.worldBlocksView.StartBlock != null)
            {
                _ctx.worldBlocksView.StartBlock.HasSpawnedNext = false;
                
                // Возвращаем стартовый блок на начальную позицию
                _ctx.worldBlocksView.StartBlock.transform.position = _startBlockInitialPosition;
                
                // Пересоздаем трафик на стартовом блоке
                if (_blockModels.TryGetValue(_ctx.worldBlocksView.StartBlock, out var startBlockModel))
                {
                    startBlockModel.Dispose();
                    _blockModels.Remove(_ctx.worldBlocksView.StartBlock);
                }
                
                var newStartBlockModel = new WorldBlockModel(_ctx.worldBlocksView.StartBlock, _poolManager);
                _blockModels[_ctx.worldBlocksView.StartBlock] = newStartBlockModel;
            }
            
            // Очищаем список активных блоков
            _ctx.worldBlocksView.ClearActiveBlocks();
        }

        protected override void OnDispose()
        {
            // Очищаем все модели блоков
            foreach (var blockModel in _blockModels.Values)
            {
                blockModel.Dispose();
            }
            _blockModels.Clear();
            
            ResetWorld();
            base.OnDispose();
        }
    }
}


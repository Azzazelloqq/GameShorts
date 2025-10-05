using Code.Core.Tools.Pool;
using GameShorts.FlyHumans.Presenters;
using GameShorts.FlyHumans.View;
using LightDI.Runtime;
using UnityEngine;

namespace GameShorts.FlyHumans.Logic
{
    /// <summary>
    /// Модель для одного блока мира, управляет его трафиком
    /// </summary>
    internal class WorldBlockModel
    {
        private readonly WorldBlock _block;
        private BlockTrafficPm _trafficPm;

        public WorldBlock Block => _block;

        public WorldBlockModel(WorldBlock block, [Inject] IPoolManager poolManager)
        {
            _block = block;

            // Если у блока есть трафик, создаем презентер через DI
            if (_block.TrafficView != null && _block.TrafficView.HasTraffic)
            {
                Debug.Log($"Creating traffic for block: {_block.gameObject.name}");
                _trafficPm = BlockTrafficPmFactory.CreateBlockTrafficPm(new BlockTrafficPm.Ctx
                {
                    trafficView = _block.TrafficView
                });
                _trafficPm.StartTraffic();
                Debug.Log($"Traffic started for block: {_block.gameObject.name}");
            }
            else
            {
                if (_block.TrafficView == null)
                    Debug.LogWarning($"Block {_block.gameObject.name} has no TrafficView!");
                else if (!_block.TrafficView.HasTraffic)
                    Debug.LogWarning($"Block {_block.gameObject.name} TrafficView has no traffic configured!");
            }
        }

        /// <summary>
        /// Обновление трафика блока
        /// </summary>
        public void UpdateTraffic(float deltaTime)
        {
            _trafficPm?.UpdateTraffic(deltaTime);
        }

        /// <summary>
        /// Очистить блок и его трафик
        /// </summary>
        public void Dispose()
        {
            _trafficPm?.Dispose();
            _trafficPm = null;
        }
    }
}


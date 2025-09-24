using System;
using Code.Games.Game2.Scripts.Core;
using R3;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game2
{
    internal class TowerModel
    {
        public ReactiveProperty<BlockData> LastPlacedBlock { get; } = new ReactiveProperty<BlockData>();
        public ReactiveProperty<float> TowerHeight { get; } = new ReactiveProperty<float>(0f);
        public ReactiveProperty<int> BlocksPlaced { get; } = new ReactiveProperty<int>(0);
        public ReactiveProperty<Axis> CurrentAxis { get; } = new ReactiveProperty<Axis>(Axis.X);
        public ReactiveProperty<float> CurrentSpeed { get; } = new ReactiveProperty<float>(2f);
        
        // Settings
        public Vector3 BlockSize { get; set; } = new Vector3(1.25f, 0.25f, 1.25f);
        public float MoveSpeedStart { get; set; } = 2f;
        public float MoveSpeedMax { get; set; } = 6f;
        public float SpeedIncrement { get; set; } = 0.1f;
        public int BlocksPerSpeedIncrease { get; set; } = 2;
        public float MoveLimit { get; set; } = 2.5f;
        public float BlockSpacing { get; set; } = 0f;

        public event Action<Vector3, Vector3> OnChunkCreated; // center, size

        public void Initialize()
        {
            LastPlacedBlock.Value = default;
            TowerHeight.Value = 0f;
            BlocksPlaced.Value = 0;
            CurrentAxis.Value = Axis.X;
            CurrentSpeed.Value = MoveSpeedStart;
        }

        public void PlaceBlock(BlockData blockData)
        {
            LastPlacedBlock.Value = blockData;
            BlocksPlaced.Value++;
            
            // Switch axis for next block
            CurrentAxis.Value = (CurrentAxis.Value == Axis.X) ? Axis.Z : Axis.X;
            
            // Increase speed periodically
            if (BlocksPlaced.Value % BlocksPerSpeedIncrease == 0)
            {
                CurrentSpeed.Value = Mathf.Min(CurrentSpeed.Value + SpeedIncrement, MoveSpeedMax);
            }
            
            // Update tower height
            float newHeight = blockData.center.y + blockData.size.y * 0.5f;
            if (newHeight > TowerHeight.Value)
            {
                TowerHeight.Value = newHeight;
            }
        }

        public void CreateChunk(Vector3 center, Vector3 size)
        {
            OnChunkCreated?.Invoke(center, size);
        }

        public Vector3 GetNextSpawnPosition()
        {
            var lastBlock = LastPlacedBlock.Value;
            float height = lastBlock.center.y + lastBlock.size.y * 0.5f + BlockSize.y * 0.5f + BlockSpacing;
            return new Vector3(lastBlock.center.x, height, lastBlock.center.z);
        }
    }
}

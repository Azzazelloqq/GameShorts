using R3;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Player
{
    internal class LawnmowerPlayerModel
    {
        // Position and movement
        public ReactiveProperty<Vector2> Position;
        public ReactiveProperty<Vector2> MovementDirection;
        
        // Movement properties
        public ReactiveProperty<float> CurrentSpeed;
        public ReactiveProperty<float> MaxSpeed;
        public ReactiveProperty<float> AccelerationSpeed;
        public ReactiveProperty<float> DecelerationSpeed;
        
        // Rotation properties
        public ReactiveProperty<float> CurrentRotation; // в градусах
        public ReactiveProperty<float> TargetRotation;
        public ReactiveProperty<float> RotationSpeed;
        public ReactiveProperty<bool> UseAcceleration;
        public ReactiveProperty<bool> InstantRotation;
        
        // Cutting properties
        public ReactiveProperty<float> CuttingRadius;
        public ReactiveProperty<bool> IsMoving;
        
        // Level progress
        public ReactiveProperty<int> CurrentLevelIndex;
        public ReactiveProperty<float> TotalGrassCut;
        
        // Grass Container System
        public ReactiveProperty<float> GrassContainerCurrentAmount;
        public ReactiveProperty<float> GrassContainerMaxCapacity;
        public ReactiveProperty<bool> IsInEmptyingZone;
        public ReactiveProperty<float> EmptyingProgress; // 0-1, прогресс опустошения

        public LawnmowerPlayerModel()
        {
            Position = new ReactiveProperty<Vector2>(Vector2.zero);
            MovementDirection = new ReactiveProperty<Vector2>(Vector2.zero);
            CurrentSpeed = new ReactiveProperty<float>(0);
            MaxSpeed = new ReactiveProperty<float>(5f);
            AccelerationSpeed = new ReactiveProperty<float>(8f);
            DecelerationSpeed = new ReactiveProperty<float>(7f);
            CurrentRotation = new ReactiveProperty<float>(0f);
            TargetRotation = new ReactiveProperty<float>(0f);
            RotationSpeed = new ReactiveProperty<float>(360f);
            UseAcceleration = new ReactiveProperty<bool>(false);
            InstantRotation = new ReactiveProperty<bool>(false);
            CuttingRadius = new ReactiveProperty<float>(1f);
            IsMoving = new ReactiveProperty<bool>(false);
            CurrentLevelIndex = new ReactiveProperty<int>(0);
            TotalGrassCut = new ReactiveProperty<float>(0f);
            
            // Initialize grass container system
            GrassContainerCurrentAmount = new ReactiveProperty<float>(0f);
            GrassContainerMaxCapacity = new ReactiveProperty<float>(100f);
            IsInEmptyingZone = new ReactiveProperty<bool>(false);
            EmptyingProgress = new ReactiveProperty<float>(0f);
        }
    }
}

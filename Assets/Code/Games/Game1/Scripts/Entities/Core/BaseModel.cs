using System;
using R3;
using UnityEngine;

namespace Asteroids.Code.Games.Game1.Scripts.Entities.Core
{
    internal class BaseModel
    {
        public event Action<int?> OnDestroy;
        public int Id;
        public EntityType EntityType;
        public int Reward;
        public ReactiveProperty<Vector2> Position;
        public ReactiveProperty<float> CurrentAngle;
        //Move 
        public ReactiveProperty<float> CurrentSpeed;
        public ReactiveProperty<float> MaxSpeed;
        public ReactiveProperty<float> AccelerationSpeed;
        public ReactiveProperty<float> DecelerationSpeed;
		
        //Rotate
        public ReactiveProperty<float> CurrentRotateSpeed;
        public ReactiveProperty<float> MaxRotateSpeed;
        public ReactiveProperty<float> AccelerationRotateSpeed;
        public ReactiveProperty<float> DecelerationRotateSpeed;
        
        public BaseModel()
        {
            Position = new ReactiveProperty<Vector2>(Vector2.zero);
            CurrentAngle = new ReactiveProperty<float>(0);
            CurrentSpeed = new ReactiveProperty<float>(0);
            MaxSpeed = new ReactiveProperty<float>(0);
            AccelerationSpeed = new ReactiveProperty<float>(0);
            DecelerationSpeed = new ReactiveProperty<float>(0);
            CurrentRotateSpeed = new ReactiveProperty<float>(0);
            MaxRotateSpeed = new ReactiveProperty<float>(0);
            AccelerationRotateSpeed = new ReactiveProperty<float>(0);
            DecelerationRotateSpeed = new ReactiveProperty<float>(0);
        }

        public void Destroy(int? killerId = null)
        {
            OnDestroy?.Invoke(killerId);
        }
    }
}
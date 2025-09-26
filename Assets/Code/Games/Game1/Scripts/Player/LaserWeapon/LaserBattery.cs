using R3;
using UnityEngine;

namespace Logic.Player.LaserWeapon
{
    internal class LaserBattery
    {
        public ReactiveProperty<float> Charge; // 0-1
        public ReactiveProperty<float> LastShot;
        public ReactiveProperty<float> RechargeCooldown;
        public bool IsReady => Charge.Value >= 1f;
        
        public LaserBattery(float rechargeCooldown)
        {
            Charge = new ReactiveProperty<float>(100);
            LastShot = new ReactiveProperty<float>(Time.time);
            RechargeCooldown = new ReactiveProperty<float>(rechargeCooldown);
        }

        public void UpdateMe(float deltaTime)
        {
            var timePass = Time.time - LastShot.Value;
            Charge.Value = Mathf.Clamp(timePass / RechargeCooldown.Value, 0 , 1f);
        }
    }
}
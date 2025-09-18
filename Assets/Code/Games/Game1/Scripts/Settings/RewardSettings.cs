using UnityEngine;

namespace Logic.Settings
{
    [CreateAssetMenu(fileName = "RewardSettings", menuName = "MyAsteroids/Settings/Create rewards settings")]
    internal class RewardSettings : ScriptableObject
    {
        public int AsteroidBigReward;
        public int AsteroidSmallReward;
        public int UFOReward;
        
    }
}
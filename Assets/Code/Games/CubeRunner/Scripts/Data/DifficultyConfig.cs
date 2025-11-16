using UnityEngine;

namespace GameShorts.CubeRunner.Data
{
    [System.Serializable]
    public class DifficultyConfig
    {
        [Tooltip("Дистанция (в плитках), после которой применяется данный уровень сложности")]
        [SerializeField]
        private int _distanceThreshold = 0;

        [Tooltip("Вероятность появления пропуска (дырки) в плитке")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _gapProbability = 0.1f;

        [Tooltip("Максимальное количество последовательных пропусков")]
        [SerializeField]
        private int _maxGapStreak = 1;

        [Tooltip("Минимальное количество последовательных целых плиток между пропусками")]
        [SerializeField]
        private int _minSolidStreak = 2;

        [Tooltip("Дополнительный множитель скорости мира для сложности (1 = без изменений)")]
        [SerializeField]
        private float _worldSpeedMultiplier = 1f;

        public int DistanceThreshold => Mathf.Max(0, _distanceThreshold);
        public float GapProbability => Mathf.Clamp01(_gapProbability);
        public int MaxGapStreak => Mathf.Max(0, _maxGapStreak);
        public int MinSolidStreak => Mathf.Max(0, _minSolidStreak);
        public float WorldSpeedMultiplier => Mathf.Max(0f, _worldSpeedMultiplier);
    }
}


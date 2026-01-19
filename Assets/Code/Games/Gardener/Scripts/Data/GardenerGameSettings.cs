using UnityEngine;

namespace GameShorts.Gardener.Data
{
    /// <summary>
    /// Общие настройки игры Gardener
    /// </summary>
    [CreateAssetMenu(fileName = "GardenerGameSettings", menuName = "Gardener/Game Settings")]
    internal class GardenerGameSettings : ScriptableObject
    {
        [Header("Economy")]
        [SerializeField] private int _startingCapital = 100;
        
        [Header("Plot Settings")]
        [SerializeField] private PlotSettings _plotSettings;
        
        [Header("Plot Timings")]
        [SerializeField] private float _plotPreparationTime = 5f;
        [SerializeField] private float _harvestHoldTime = 2f;
        
        [Header("Platform Rotation")]
        [SerializeField] private float _rotationSensitivity = 0.5f;
        [SerializeField] private float _minVerticalAngle = -30f;
        [SerializeField] private float _maxVerticalAngle = 30f;
        
        public int StartingCapital => _startingCapital;
        public PlotSettings PlotSettings => _plotSettings;
        public float PlotPreparationTime => _plotPreparationTime;
        public float HarvestHoldTime => _harvestHoldTime;
        public float RotationSensitivity => _rotationSensitivity;
        public float MinVerticalAngle => _minVerticalAngle;
        public float MaxVerticalAngle => _maxVerticalAngle;
    }
}


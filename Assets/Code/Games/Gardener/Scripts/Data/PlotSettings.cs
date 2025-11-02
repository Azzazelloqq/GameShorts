using UnityEngine;

namespace GameShorts.Gardener.Data
{
    /// <summary>
    /// Настройки для грядок
    /// </summary>
    [CreateAssetMenu(fileName = "PlotSettings", menuName = "Gardener/Plot Settings")]
    public class PlotSettings : ScriptableObject
    {
        [Header("Префаб")]
        [SerializeField] private GameObject _plotPrefab;
        
        [Header("UI")]
        [SerializeField] private Sprite _icon;
        
        public GameObject PlotPrefab => _plotPrefab;
        public Sprite Icon => _icon;
    }
}


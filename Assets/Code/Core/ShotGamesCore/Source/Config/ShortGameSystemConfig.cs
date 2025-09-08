using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Core.ShotGamesCore.Config
{
    /// <summary>
    /// Configuration ScriptableObject for the ShortGames system.
    /// Contains resource mappings and default settings.
    /// </summary>
    [CreateAssetMenu(fileName = "ShortGameSystemConfig", menuName = "ShortGames/System Config")]
    public class ShortGameSystemConfig : ScriptableObject
    {
        [Header("Resource Configuration")]
        [Tooltip("Mapping of game types to their Addressable resource IDs")]
        [SerializeField] private List<GameResourceEntry> gameResourceMapping = new();
        
        [Header("Default Settings")]
        [Tooltip("Default parent transform for instantiated games")]
        [SerializeField] private Transform defaultParent;
        
        [Tooltip("Maximum number of instances per game type in pool")]
        [Range(1, 10)]
        [SerializeField] private int maxPoolSizePerType = 3;
        
        [Header("Performance Settings")]
        [Tooltip("Enable preloading of all games on start")]
        [SerializeField] private bool preloadOnStart = true;
        
        [Tooltip("Time in seconds to keep unused resources in memory")]
        [SerializeField] private float unusedResourceTimeout = 60f;
        
        /// <summary>
        /// Get resource mapping as dictionary
        /// </summary>
        public Dictionary<Type, string> GetResourceMapping()
        {
            var mapping = new Dictionary<Type, string>();
            foreach (var entry in gameResourceMapping)
            {
                if (entry.gameType != null && !string.IsNullOrEmpty(entry.resourceId))
                {
                    mapping[Type.GetType(entry.gameType)] = entry.resourceId;
                }
            }
            return mapping;
        }
        
        public Transform DefaultParent => defaultParent;
        public int MaxPoolSizePerType => maxPoolSizePerType;
        public bool PreloadOnStart => preloadOnStart;
        public float UnusedResourceTimeout => unusedResourceTimeout;
        
        /// <summary>
        /// Entry for game resource mapping
        /// </summary>
        [System.Serializable]
        public class GameResourceEntry
        {
            [Tooltip("Full type name of the game (e.g., MyNamespace.MyGame)")]
            public string gameType;
            
            [Tooltip("Addressable resource ID or path")]
            public string resourceId;
            
            [Tooltip("Optional description")]
            public string description;
        }
    }
}

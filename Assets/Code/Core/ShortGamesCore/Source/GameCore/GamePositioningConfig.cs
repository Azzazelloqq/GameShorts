using UnityEngine;

namespace Code.Core.ShortGamesCore.Source.GameCore
{
    /// <summary>
    /// Central configuration for automatic game positioning.
    /// This is the single point of configuration for how games are positioned in the world.
    /// </summary>
    [CreateAssetMenu(fileName = "GamePositioningConfig", menuName = "ShortGames/Game Positioning Config")]
    public class GamePositioningConfig : ScriptableObject
    {
        [Header("3D Games Positioning")]
        [Tooltip("Distance between 3D games to prevent visual overlap")]
        [SerializeField] private float distance3DGames = 100f;
        
        [Tooltip("Base position for the first 3D game")]
        [SerializeField] private Vector3 base3DPosition = Vector3.zero;
        
        [Tooltip("Axis along which to position 3D games (e.g., (1,0,0) for X-axis)")]
        [SerializeField] private Vector3 positioning3DAxis = Vector3.right;
        
        [Header("2D Games Positioning")]
        [Tooltip("Distance between 2D games to prevent visual overlap")]
        [SerializeField] private float distance2DGames = 50f;
        
        [Tooltip("Base position for the first 2D game (should be far from 3D games)")]
        [SerializeField] private Vector3 base2DPosition = new Vector3(1000f, 0f, 0f);
        
        [Tooltip("Axis along which to position 2D games")]
        [SerializeField] private Vector3 positioning2DAxis = Vector3.right;
        
        [Header("UI Games Settings")]
        [Tooltip("Whether each UI game should have its own Canvas")]
        [SerializeField] private bool createSeparateCanvasForUIGames = true;
        
        [Tooltip("Canvas sort order increment for each UI game")]
        [SerializeField] private int canvasSortOrderIncrement = 100;
        
        // Properties for access
        public float Distance3DGames => distance3DGames;
        public Vector3 Base3DPosition => base3DPosition;
        public Vector3 Positioning3DAxis => positioning3DAxis.normalized;
        
        public float Distance2DGames => distance2DGames;
        public Vector3 Base2DPosition => base2DPosition;
        public Vector3 Positioning2DAxis => positioning2DAxis.normalized;
        
        public bool CreateSeparateCanvasForUIGames => createSeparateCanvasForUIGames;
        public int CanvasSortOrderIncrement => canvasSortOrderIncrement;
        
        /// <summary>
        /// Gets the position for a 3D game based on its index
        /// </summary>
        public Vector3 GetPosition3D(int gameIndex)
        {
            return Base3DPosition + (Positioning3DAxis * Distance3DGames * gameIndex);
        }
        
        /// <summary>
        /// Gets the position for a 2D game based on its index
        /// </summary>
        public Vector3 GetPosition2D(int gameIndex)
        {
            return Base2DPosition + (Positioning2DAxis * Distance2DGames * gameIndex);
        }
        
        /// <summary>
        /// Gets the canvas sort order for a UI game based on its index
        /// </summary>
        public int GetCanvasSortOrder(int gameIndex)
        {
            return gameIndex * CanvasSortOrderIncrement;
        }
        
        /// <summary>
        /// Creates a default configuration
        /// </summary>
        public static GamePositioningConfig CreateDefault()
        {
            var config = CreateInstance<GamePositioningConfig>();
            config.distance3DGames = 100f;
            config.base3DPosition = Vector3.zero;
            config.positioning3DAxis = Vector3.right;
            config.distance2DGames = 50f;
            config.base2DPosition = new Vector3(1000f, 0f, 0f);
            config.positioning2DAxis = Vector3.right;
            config.createSeparateCanvasForUIGames = true;
            config.canvasSortOrderIncrement = 100;
            return config;
        }
    }
}

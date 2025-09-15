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
        [SerializeField] private float _distance3DGames = 100f;
        
        [Tooltip("Base position for the first 3D game")]
        [SerializeField] private Vector3 _base3DPosition = Vector3.zero;
        
        [Tooltip("Axis along which to position 3D games (e.g., (1,0,0) for X-axis)")]
        [SerializeField] private Vector3 _positioning3DAxis = Vector3.right;
        
        [Header("2D Games Positioning")]
        [Tooltip("Distance between 2D games to prevent visual overlap")]
        [SerializeField] private float _distance2DGames = 50f;
        
        [Tooltip("Base position for the first 2D game (should be far from 3D games)")]
        [SerializeField] private Vector3 _base2DPosition = new Vector3(1000f, 0f, 0f);
        
        [Tooltip("Axis along which to position 2D games")]
        [SerializeField] private Vector3 _positioning2DAxis = Vector3.right;
        
        [Header("UI Games Settings")]
        [Tooltip("Whether each UI game should have its own Canvas")]
        [SerializeField] private bool _createSeparateCanvasForUIGames = true;
        
        [Tooltip("Canvas sort order increment for each UI game")]
        [SerializeField] private int _canvasSortOrderIncrement = 100;
        
        // Properties for access
        public float Distance3DGames => _distance3DGames;
        public Vector3 Base3DPosition => _base3DPosition;
        public Vector3 Positioning3DAxis => _positioning3DAxis.normalized;
        
        public float Distance2DGames => _distance2DGames;
        public Vector3 Base2DPosition => _base2DPosition;
        public Vector3 Positioning2DAxis => _positioning2DAxis.normalized;
        
        public bool CreateSeparateCanvasForUIGames => _createSeparateCanvasForUIGames;
        public int CanvasSortOrderIncrement => _canvasSortOrderIncrement;
        
        /// <summary>
        /// Gets the position for a 3D game based on its index
        /// </summary>
        public Vector3 GetPosition3D(int gameIndex)
        {
            return Base3DPosition + (Positioning3DAxis * (Distance3DGames * gameIndex));
        }
        
        /// <summary>
        /// Gets the position for a 2D game based on its index
        /// </summary>
        public Vector3 GetPosition2D(int gameIndex)
        {
            return Base2DPosition + (Positioning2DAxis * (Distance2DGames * gameIndex));
        }
        
        /// <summary>
        /// Gets the canvas sort order for a UI game based on its index
        /// </summary>
        public int GetCanvasSortOrder(int gameIndex)
        {
            return gameIndex * CanvasSortOrderIncrement;
        }
        
        #if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>
        /// Creates a default configuration
        /// </summary>
        public static GamePositioningConfig CreateDefault()
        {
            var config = CreateInstance<GamePositioningConfig>();
            config._distance3DGames = 100f;
            config._base3DPosition = Vector3.zero;
            config._positioning3DAxis = Vector3.right;
            config._distance2DGames = 50f;
            config._base2DPosition = new Vector3(1000f, 0f, 0f);
            config._positioning2DAxis = Vector3.right;
            config._createSeparateCanvasForUIGames = true;
            config._canvasSortOrderIncrement = 100;
            return config;
        }
        #endif
    }
}

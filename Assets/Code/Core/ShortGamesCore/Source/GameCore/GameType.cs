namespace Code.Core.ShortGamesCore.Source.GameCore
{
    /// <summary>
    /// Defines the rendering type of a short game
    /// </summary>
    public enum GameType
    {
        /// <summary>
        /// 3D game with perspective camera, lights, and 3D physics
        /// </summary>
        ThreeD,
        
        /// <summary>
        /// 2D game with orthographic camera and 2D physics
        /// </summary>
        TwoD,
        
        /// <summary>
        /// UI-based game rendered on Canvas
        /// </summary>
        UI
    }
}

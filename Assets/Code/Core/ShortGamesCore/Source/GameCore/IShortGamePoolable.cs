namespace Code.Core.ShortGamesCore.Source.GameCore
{
    /// <summary>
    /// Interface for games that support object pooling.
    /// Implement this interface along with your game type interface (IShortGame3D, IShortGame2D, IShortGameUI)
    /// to enable pooling for your game.
    /// </summary>
    public interface IShortGamePoolable : IShortGame
    {
        /// <summary>
        /// Called when the game instance is returned to the pool.
        /// Use this to reset game state.
        /// </summary>
        void OnPooled();
        
        /// <summary>
        /// Called when the game instance is taken from the pool for reuse.
        /// Use this to prepare the game for a new session.
        /// </summary>
        void OnUnpooled();
    }
}

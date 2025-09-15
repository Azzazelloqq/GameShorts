using System;

namespace Code.Core.ShortGamesCore.Source.GameCore
{
    /// <summary>
    /// Utility class for detecting game type from its interface implementation
    /// </summary>
    public static class GameTypeDetector
    {
        /// <summary>
        /// Detects the game type based on implemented interfaces
        /// </summary>
        public static GameType GetGameType(Type gameType)
        {
            if (!typeof(IShortGame).IsAssignableFrom(gameType))
            {
                throw new ArgumentException($"Type {gameType.Name} does not implement IShortGame");
            }
            
            if (typeof(IShortGameUI).IsAssignableFrom(gameType))
            {
                return GameType.UI;
            }
            
            if (typeof(IShortGame2D).IsAssignableFrom(gameType))
            {
                return GameType.TwoD;
            }
            
            if (typeof(IShortGame3D).IsAssignableFrom(gameType))
            {
                return GameType.ThreeD;
            }
            
            // Default to 3D for backward compatibility with games that only implement IShortGame
            return GameType.ThreeD;
        }
        
        /// <summary>
        /// Checks if a game type supports pooling
        /// </summary>
        public static bool IsPoolable(Type gameType)
        {
            return typeof(IShortGamePoolable).IsAssignableFrom(gameType);
        }
    }
}

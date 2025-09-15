namespace Code.Core.ShortGamesCore.Source.GameCore
{
    /// <summary>
    /// Interface for 2D short games.
    /// Games implementing this interface will be positioned in 2D space with proper separation
    /// and should include their own orthographic camera setup.
    /// </summary>
    public interface IShortGame2D : IShortGame
    {
        // 2D games will be positioned in world space with orthographic cameras
        // They should have their own camera as child objects
    }
}

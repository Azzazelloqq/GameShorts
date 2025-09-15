namespace Code.Core.ShortGamesCore.Source.GameCore
{
    /// <summary>
    /// Interface for 3D short games.
    /// Games implementing this interface will be positioned in 3D space with proper separation
    /// and should include their own camera and lighting setup.
    /// </summary>
    public interface IShortGame3D : IShortGame
    {
        // 3D games will be positioned in world space
        // They should have their own camera and lights as child objects
    }
}

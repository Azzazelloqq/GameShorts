namespace Code.Core.ShortGamesCore.Source.GameCore
{
    /// <summary>
    /// Interface for UI-based short games.
    /// Games implementing this interface will be rendered on Canvas
    /// and can use UI elements like buttons, panels, etc.
    /// </summary>
    public interface IShortGameUI : IShortGame
    {
        // UI games will be created with their own Canvas
        // They work with UI elements and events
    }
}

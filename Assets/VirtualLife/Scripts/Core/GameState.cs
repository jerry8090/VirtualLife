namespace VirtualLife.Core
{
    /// <summary>High-level application states. Gameplay-level states belong to later stages.</summary>
    public enum GameState
    {
        Initializing,
        MainMenu,
        LoadingWorld,
        Playing,
        Paused
    }
}

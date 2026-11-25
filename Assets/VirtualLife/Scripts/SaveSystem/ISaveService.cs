namespace VirtualLife.SaveSystem
{
    /// <summary>
    /// Service boundary for persistence. Stage 1 only defines the contract and a basic JSON file
    /// backend; player/world data models arrive in later stages.
    /// </summary>
    public interface ISaveService
    {
        string RootPath { get; }
        bool Exists(string key);
        bool Save<T>(string key, T data);
        bool TryLoad<T>(string key, out T data);
        bool Delete(string key);
    }
}

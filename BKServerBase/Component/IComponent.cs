namespace BKServerBase.Component
{
    public delegate void OnComponentInitializedHandler();

    public interface IComponent
    {
        (bool success, OnComponentInitializedHandler? InitDoneFunc) Initialize();
        bool Shutdown();
        bool OnUpdate(double delta);
    }

    public interface IComponentManager
    {
        bool AddComponent<T>(IComponent component, bool lazyLoad = false) where T : class, IComponent;
        bool RemoveComponent<T>(IComponent component) where T : class, IComponent;
        T? GetComponent<T>() where T : class, IComponent;
        bool Initialize();
        void Stop();
        OnComponentInitializedHandler? OnComponentInitialized { get; set; }
    }
}

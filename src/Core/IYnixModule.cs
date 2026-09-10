using NativeUI;

namespace YnixTrainer.Core
{
    public interface IYnixModule
    {
        string Name { get; }
        void Initialize(UIMenu mainMenu, MenuPool menuPool);
        void OnTick();
    }
}

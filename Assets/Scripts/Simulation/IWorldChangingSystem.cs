namespace Simulation
{
    public interface IWorldChangingSystem : ISystem
    {
        void OnChanging();
    }
}
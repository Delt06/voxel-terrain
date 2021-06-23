namespace Simulation
{
    public interface ITickSystem : ISystem
    {
        void OnTick();
    }
}
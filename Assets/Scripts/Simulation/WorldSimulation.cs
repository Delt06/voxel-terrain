using System;
using Chunks;
using UnityEngine;

namespace Simulation
{
    public class WorldSimulation : MonoBehaviour
    {
        [SerializeField, Min(0)] private int _ticksPerSecond = 30;
        [SerializeField] private World _world = default;

        private void Update()
        {
            _timeToNextTick += Time.deltaTime;
            if (_timeToNextTick < TickPeriod) return;

            _timeToNextTick -= TickPeriod;
            OnTick();
        }

        private void OnTick()
        {
            foreach (var system in _systems)
            {
                if (system is ITickSystem tickSystem)
                    tickSystem.OnTick();
            }
        }

        private float TickPeriod => 1f / _ticksPerSecond;

        private void OnEnable()
        {
            _world.ChunkChanging += _onChunkChanging;
        }

        private void OnDisable()
        {
            _world.ChunkChanging -= _onChunkChanging;
        }

        private void Awake()
        {
            _systems = GetComponentsInChildren<ISystem>();
            _onChunkChanging = (sender, chunk) =>
            {
                foreach (var system in _systems)
                {
                    if (system is IWorldChangingSystem worldChangingSystem)
                        worldChangingSystem.OnChanging();
                }
            };
        }

        private EventHandler<Chunk> _onChunkChanging;
        private ISystem[] _systems;
        private float _timeToNextTick;
    }
}
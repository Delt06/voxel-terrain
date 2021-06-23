using System;
using Blocks;
using Chunks;
using Unity.Mathematics;
using UnityEngine;

namespace Controls
{
    public class RaycastBlockDestroy : MonoBehaviour, IBlockDestruction
    {
        [SerializeField] private World _world = default;
        [SerializeField] private LayerMask _castMask = default;
        [SerializeField] private LayerMask _obstructionMask = default;
        [SerializeField, Min(0f)] private float _maxDistance = 3f;
        [SerializeField, Range(0f, 1f)] private float _castExtraDistance = 0.02f;
        [SerializeField, Min(0f)] private float _destructionResetTime = 0.2f;
        [SerializeField, Min(0f)] private float _destructionSpeed = 1f;

        public void Construct(IActiveBlockConsumer activeBlockConsumer)
        {
            _activeBlockConsumer = activeBlockConsumer;
        }

        public bool TryGetDestructionState(out DestructionState destructionState)
        {
            destructionState = _destructionState.GetValueOrDefault();
            return _destructionState.HasValue;
        }

        public World World => _world;

        private void Update()
        {
            if (_destructionState.HasValue && ShouldResetDestructionState(_destructionState.Value))
                _destructionState = null;
        }

        private bool ShouldResetDestructionState(DestructionState state)
        {
            if (TimeNow >= state.LastUpdateTime + _destructionResetTime) return true;

            var chunkWorldPosition = _world.ChunkToWorldCoordinates(state.ChunkPositionXZ);
            if (!_world.TryGetChunkAt(chunkWorldPosition, out var chunk)) return true;

            var block = chunk.GetBlockAt(state.LocalBlockPosition);
            if (!block.Equals(state.Block)) return true;

            return false;
        }

        private static float TimeNow => Time.time;

        public void TryDestroyAtScreenPosition(Vector2 screenPosition, float deltaTime)
        {
            TryDestroyAtViewportPosition(ToViewport(screenPosition), deltaTime);
        }

        public void TryPlaceAtScreenPosition(Vector2 screenPosition)
        {
            TryPlaceAtViewportPosition(ToViewport(screenPosition));
        }

        private Vector3 ToViewport(Vector2 screenPosition) => _camera.ScreenToViewportPoint(screenPosition);

        private void TryDestroyAtViewportPosition(Vector2 viewport, float deltaTime)
        {
            if (!TryRaycast(viewport, out var hit)) return;
            if (!TryGetLocalPositionInChunk(hit, _castExtraDistance, out var chunk, out var blockPosition)) return;
            if (!chunk.IsBusyAt(blockPosition)) return;

            var destroyedBlock = chunk.GetBlockAt(blockPosition);
            var deltaDestruction = deltaTime * _destructionSpeed;
            var chunkPositionXZ = _world.WorldToChunkCoordinates(chunk.Origin);
            var destructionState = _destructionState.GetValueOrDefault();

            if (_destructionState == null ||
                !destructionState.Block.Equals(destroyedBlock) ||
                !destructionState.ChunkPositionXZ.Equals(chunkPositionXZ) ||
                !destructionState.LocalBlockPosition.Equals(blockPosition))
            {
                _destructionState = new DestructionState
                {
                    Block = destroyedBlock,
                    Progress = deltaDestruction,
                    LastUpdateTime = TimeNow,
                    LocalBlockPosition = blockPosition,
                    ChunkPositionXZ = chunkPositionXZ,
                };
                return;
            }

            destructionState.LastUpdateTime = TimeNow;
            destructionState.Progress += deltaDestruction;

            if (_destructionState.Value.Progress >= 1f)
            {
                chunk.SetBlockAt(blockPosition, BlockData.Empty);
                _destructionState = null;
                DestroyedBlock?.Invoke(this, destroyedBlock);
            }
            else
            {
                _destructionState = destructionState;
            }
        }

        public event EventHandler<BlockData> DestroyedBlock;

        private void TryPlaceAtViewportPosition(Vector2 viewport)
        {
            if (!_activeBlockConsumer.TryGetBlock(out var block)) return;
            if (!TryRaycast(viewport, out var hit)) return;
            if (!TryGetLocalPositionInChunk(hit, -_castExtraDistance, out var chunk, out var blockPosition)) return;
            if (!chunk.GetBlockAt(blockPosition).CanPlaceOver()) return;
            if (IsObstructed(chunk, blockPosition)) return;

            chunk.SetBlockAt(blockPosition, block);
            _activeBlockConsumer.Consume();
        }

        private bool TryRaycast(Vector2 viewport, out RaycastHit hit)
        {
            var ray = _camera.ViewportPointToRay(viewport);
            return Physics.Raycast(ray, out hit, _maxDistance, _castMask) &&
                   hit.collider.GetComponentInParent<Chunk>() != null;
        }

        private bool TryGetLocalPositionInChunk(RaycastHit hit, float extraDistance, out Chunk chunk,
            out int3 localPosition)
        {
            chunk = default;
            localPosition = default;

            var worldPosition = hit.point - hit.normal * extraDistance;
            return _world.TryGetChunkAt(worldPosition, out chunk) &&
                   chunk.TryConvertToLocalPosition(worldPosition, out localPosition);
        }

        private bool IsObstructed(Chunk chunk, int3 blockPosition)
        {
            var center = chunk.GetBlockWorldCenter(blockPosition);
            var halfExtents = Vector3.one * 0.5f;
            return Physics.CheckBox(center, halfExtents, Quaternion.identity, _obstructionMask,
                QueryTriggerInteraction.Ignore
            );
        }

        private void Awake()
        {
            _camera = Camera.main;
        }

        private Camera _camera;
        private DestructionState? _destructionState;
        private IActiveBlockConsumer _activeBlockConsumer;
    }
}
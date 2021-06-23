using UnityEngine;

namespace Controls.Visuals
{
    [RequireComponent(typeof(Renderer))]
    public sealed class BlockDestructionVisuals : MonoBehaviour
    {
        [SerializeField] private RaycastBlockDestroy _blockDestroy = default;

        private void Update()
        {
            if (_blockDestroy.TryGetDestructionState(out var destructionState) &&
                _blockDestroy.World.TryGetChunkAt(destructionState.ChunkPositionXZ, out var chunk))
            {
                var center = chunk.GetBlockWorldCenter(destructionState.LocalBlockPosition);
                transform.SetPositionAndRotation(center, Quaternion.identity);
                _materialPropertyBlock.SetFloat(ProgressId, destructionState.Progress);
                _renderer.SetPropertyBlock(_materialPropertyBlock);
                ToggleRenderer(true);
            }
            else
            {
                ToggleRenderer(false);
            }
        }

        private void ToggleRenderer(bool enable)
        {
            if (_renderer.enabled != enable)
                _renderer.enabled = enable;
        }

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _renderer.enabled = false;
            _materialPropertyBlock = new MaterialPropertyBlock();
        }

        private Renderer _renderer;
        private MaterialPropertyBlock _materialPropertyBlock;
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    }
}
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Chunks
{
    public sealed class Chunk : MonoBehaviour
    {
        [SerializeField, Min(1)]
        private int _sizeX = 16;

        [SerializeField, Min(1)]
        private int _sizeZ = 16;

        [SerializeField, Min(1)]
        private int _sizeY = 50;

        public Vector3 Origin => transform.position;
        public int SizeX => _sizeX;

        public int SizeY => _sizeY;

        public int SizeZ => _sizeZ;

        public int2 PositionXZ { get; set; }

        public bool IsLocked => _locks.Count > 0;

        public void RequestLock([NotNull] object source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            _locks.Add(source);
        }

        public void ReleaseLock([NotNull] object source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            _locks.Remove(source);
        }

        public bool TryConvertToLocalPosition(Vector3 worldPosition, out int3 localPosition)
        {
            var offset = worldPosition - Origin;
            localPosition = offset.FloorToInt();
            return 0 <= localPosition.x && localPosition.x < SizeX &&
                   0 <= localPosition.y && localPosition.y < SizeY &&
                   0 <= localPosition.z && localPosition.z < SizeZ;
        }

        public Vector3 GetBlockWorldCenter(int3 localPosition)
        {
            var worldPosition = Origin + (Vector3) (float3) localPosition;
            return worldPosition + Vector3.one * 0.5f;
        }

        public bool IsBusyAt(int localX, int localY, int localZ) => GetBlockAt(localX, localY, localZ).Exists;

        public BlockData GetBlockAt(int localX, int localY, int localZ)
        {
            var index = LocalPositionToIndex(localX, localY, localZ);
            return BlocksBuffer[index];
        }

        public void SetBlockAt(int localX, int localY, int localZ, BlockData block)
        {
            Changing?.Invoke(this, EventArgs.Empty);
            var index = LocalPositionToIndex(localX, localY, localZ);
            var blocks = BlocksBuffer;
            var oldBlock = blocks[index];
            blocks[index] = block;
            OnChanged();
            BlockChanged?.Invoke(this, (oldBlock, new int3(localX, localY, localZ)));
        }

        public event EventHandler Changing;

        public void OnWasGenerated()
        {
            OnChanged();
            WasGenerated?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler WasGenerated;

        private void OnChanged()
        {
            _isValid = true;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler<(BlockData oldBlock, int3 localPosition)> BlockChanged;

        public bool TryGetValidBlocks(out NativeArray<BlockData> blocks)
        {
            if (_isValid)
            {
                blocks = BlocksBuffer;
                return true;
            }

            blocks = default;
            return false;
        }

        public NativeArray<BlockData> BlocksBuffer => _blocks.IsCreated
            ? _blocks
            : _blocks = new NativeArray<BlockData>(_sizeX * _sizeZ * _sizeY, Allocator.Persistent);

        public event EventHandler Changed;

        private int LocalPositionToIndex(int localX, int localY, int localZ)
        {
            ValidateLocalPosition(localX, localY, localZ);
            return ChunkUtils.PositionToIndex(new int3(localX, localY, localZ), new int3(SizeX, SizeY, SizeZ));
        }

        private void ValidateLocalPosition(int localX, int localY, int localZ)
        {
            const string message = "Value is out of range.";
            if (localX < 0 || localX >= _sizeX) throw new ArgumentOutOfRangeException(nameof(localX), localX, message);
            if (localY < 0 || localY >= _sizeY) throw new ArgumentOutOfRangeException(nameof(localY), localY, message);
            if (localZ < 0 || localZ >= _sizeZ) throw new ArgumentOutOfRangeException(nameof(localZ), localZ, message);
        }

        private void OnEnable()
        {
            _isValid = false;
            if (_locks.Count > 0)
            {
                Debug.LogWarning("Chunk was disabled while it was locked.", this);
                _locks.Clear();
            }
        }

        private void OnDestroy()
        {
            if (_blocks.IsCreated)
                _blocks.Dispose();
        }

        private bool _isValid;
        private readonly HashSet<object> _locks = new HashSet<object>();
        private NativeArray<BlockData> _blocks;
    }
}
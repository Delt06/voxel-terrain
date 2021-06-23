using System;
using Chunks.MeshGeneration;
using Unity.Jobs;
using UnityEngine;

namespace Chunks.ColliderGeneration
{
    [RequireComponent(typeof(ChunkMeshGenerator)), RequireComponent(typeof(MeshCollider))]
    public class ChunkMeshColliderGenerator : MonoBehaviour
    {
        private void Update()
        {
            if (_meshBakingJob == null) return;
            if (!_meshBakingJob.Value.IsCompleted) return;

            _meshBakingJob.Value.Complete();
            _meshBakingJob = null;

            if (_jobIsDirty)
                ScheduleBaking();
            else
                _meshCollider.sharedMesh = _meshCollider.sharedMesh;
        }

        private void OnEnable()
        {
            _meshGenerator.MeshChanged += _onMeshChanged;
            if (_meshBakingJob != null)
                _jobIsDirty = true;
        }

        private void OnDisable()
        {
            _meshGenerator.MeshChanged -= _onMeshChanged;
        }

        private void Awake()
        {
            _meshGenerator = GetComponent<ChunkMeshGenerator>();
            _meshCollider = GetComponent<MeshCollider>();
            _onMeshChanged = (sender, args) =>
            {
                if (_meshBakingJob != null) return;
                ScheduleBaking();
            };
        }

        private void ScheduleBaking()
        {
            _meshBakingJob = new MeshColliderBakingJob
            {
                Convex = false,
                MeshInstanceId = Mesh.GetInstanceID(),
            }.Schedule();
            _jobIsDirty = false;
        }

        private Mesh Mesh => _meshCollider.sharedMesh;

        private MeshCollider _meshCollider;
        private bool _jobIsDirty;
        private JobHandle? _meshBakingJob;
        private ChunkMeshGenerator _meshGenerator;
        private EventHandler _onMeshChanged;
    }
}
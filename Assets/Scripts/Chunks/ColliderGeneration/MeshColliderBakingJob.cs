using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Chunks.ColliderGeneration
{
    [BurstCompile]
    public struct MeshColliderBakingJob : IJob
    {
        [ReadOnly] public int MeshInstanceId;
        [ReadOnly] public bool Convex;

        public void Execute()
        {
            Physics.BakeMesh(MeshInstanceId, Convex);
        }
    }
}
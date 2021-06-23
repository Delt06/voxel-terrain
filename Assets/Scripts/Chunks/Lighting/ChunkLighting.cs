using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Chunks.Lighting
{
    [RequireComponent(typeof(Chunk))]
    public class ChunkLighting : MonoBehaviour
    {
        [SerializeField] private FilterMode _lightmapFilterMode = FilterMode.Bilinear;
        [SerializeField] private Renderer[] _affectedRenderers = default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CalculateAttenuationJob CreateCalculateAttenuationJob(World world,
            NativeArray<byte> defaultLightmap) =>
            new CalculateAttenuationJob
            {
                ChunkSize = world.ChunkSize,
                Lightmaps =
                    LightmapNeighborhoodUtils.Create(world, _chunk.PositionXZ, defaultLightmap),
                LightmapAttenuationValues = _lightmapAttenuationValues,
            };

        public NativeArray<byte> LightmapValues => _lightmapValues;

        public void WriteAttenuationToTexture(float2 extraLightmapAttenuation = default)
        {
            _lightmapAttenuation.SetPixelData(_lightmapAttenuationValues, 0);
            _lightmapAttenuation.Apply();
            _materialPropertyBlock.SetVector(ExtraLightmapAttenuationId,
                new Vector4(extraLightmapAttenuation.x, extraLightmapAttenuation.y)
            );
            SetPropertyBlockForAllRenderers();
        }

        private void OnEnable()
        {
            _lightmapValues.EnsureCreated(ChunkVolume, Allocator);
            _lightmapAttenuationValues.EnsureCreated(_lightmapAttenuationVolume, Allocator);
            WriteAttenuationToTexture(InitialExtraLightAttenuation);
        }

        private static readonly float2 InitialExtraLightAttenuation = new float2(1f, 0f);

        private int ChunkVolume { get; set; }
        private static Allocator Allocator => Allocator.Persistent;

        private void OnDisable()
        {
            _lightmapValues.DisposeIfCreated();
            _lightmapAttenuationValues.DisposeIfCreated();
        }

        private void Awake()
        {
            _chunk = GetComponent<Chunk>();
            ChunkVolume = _chunk.SizeX * _chunk.SizeY * _chunk.SizeZ;
            var padding = new int3(1, 0, 1);
            _lightmapSize = new int3(_chunk.SizeX, _chunk.SizeY, _chunk.SizeZ) + padding * 2;
            _lightmapAttenuationVolume = _lightmapSize.x * _lightmapSize.y * _lightmapSize.z;

            _lightmapAttenuation = new Texture3D(_lightmapSize.x, _lightmapSize.y, _lightmapSize.z,
                TextureFormat.RGFloat, false
            )
            {
                anisoLevel = 0,
                filterMode = _lightmapFilterMode,
                wrapMode = TextureWrapMode.Clamp,
            };

            _materialPropertyBlock = new MaterialPropertyBlock();
            _materialPropertyBlock.SetTexture(LightMapId, _lightmapAttenuation);
            SetPropertyBlockForAllRenderers();
        }

        private void SetPropertyBlockForAllRenderers()
        {
            foreach (var affectedRenderer in _affectedRenderers)
            {
                affectedRenderer.SetPropertyBlock(_materialPropertyBlock);
            }
        }

        private void OnDestroy()
        {
            if (_lightmapAttenuation != null)
                Destroy(_lightmapAttenuation);
        }

        private Chunk _chunk;

        private int3 _lightmapSize;
        private int _lightmapAttenuationVolume;

        private MaterialPropertyBlock _materialPropertyBlock;
        private NativeArray<byte> _lightmapValues;
        private NativeArray<float2> _lightmapAttenuationValues;

        private Texture3D _lightmapAttenuation;
        private static readonly int LightMapId = Shader.PropertyToID("_LightMap");
        private static readonly int ExtraLightmapAttenuationId = Shader.PropertyToID("_ExtraLightmapAttenuation");
    }
}
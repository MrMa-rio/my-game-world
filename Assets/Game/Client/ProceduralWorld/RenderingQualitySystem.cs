using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MyGameWorld.Client.ProceduralWorld
{
    public enum RenderingQualityTier : byte { Low, Medium, High, Ultra }
    public enum ImageAntiAliasingMode : byte { Off, Fxaa, Smaa, Msaa, Temporal }

    [CreateAssetMenu(menuName = "My Game World/Rendering Quality Profile")]
    public sealed class RenderingQualityProfile : ScriptableObject
    {
        [SerializeField] private RenderingQualityTier _tier;
        [SerializeField] private ImageAntiAliasingMode _antiAliasing = ImageAntiAliasingMode.Smaa;
        [SerializeField, Range(1, 8)] private int _msaaSamples = 1;
        [SerializeField, Range(0.5f, 1.5f)] private float _renderScale = 1f;
        [SerializeField] private bool _temporalStability;
        [SerializeField] private AnisotropicFiltering _anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        [SerializeField, Range(1f, 3f)] private float _lodBias = 1.5f;
        [SerializeField, Range(-1f, 0f)] private float _mipMapBias = -0.15f;
        [SerializeField] private bool _alphaToCoverage;
        [SerializeField] private bool _distantWorldStabilization = true;
        [SerializeField, Range(0.5f, 4f)] private float _subpixelThreshold = 1.5f;
        [SerializeField, Range(0f, 1f)] private float _taaHistoryWeight = 0.88f;
        [SerializeField, Range(0f, 1f)] private float _taaSharpening = 0.2f;

        public RenderingQualityTier Tier => _tier;
        public ImageAntiAliasingMode AntiAliasing => _antiAliasing;
        public int MsaaSamples => NormalizeMsaa(_msaaSamples);
        public float RenderScale => _renderScale;
        public bool TemporalStability => _temporalStability;
        public AnisotropicFiltering AnisotropicFiltering => _anisotropicFiltering;
        public float LodBias => _lodBias;
        public float MipMapBias => _mipMapBias;
        public bool AlphaToCoverage => _alphaToCoverage;
        public bool DistantWorldStabilization => _distantWorldStabilization;
        public float SubpixelThreshold => _subpixelThreshold;
        public float TaaHistoryWeight => _taaHistoryWeight;
        public float TaaSharpening => _taaSharpening;

        public void Configure(RenderingQualityTier tier, ImageAntiAliasingMode antiAliasing, int msaaSamples,
            float renderScale, bool temporalStability, AnisotropicFiltering anisotropicFiltering,
            float lodBias, float mipMapBias, bool alphaToCoverage, bool distantWorldStabilization,
            float subpixelThreshold, float taaHistoryWeight = 0.88f, float taaSharpening = 0.2f)
        {
            _tier = tier; _antiAliasing = antiAliasing; _msaaSamples = NormalizeMsaa(msaaSamples);
            _renderScale = Mathf.Clamp(renderScale, 0.5f, 1.5f); _temporalStability = temporalStability;
            _anisotropicFiltering = anisotropicFiltering; _lodBias = Mathf.Clamp(lodBias, 1f, 3f);
            _mipMapBias = Mathf.Clamp(mipMapBias, -1f, 0f); _alphaToCoverage = alphaToCoverage;
            _distantWorldStabilization = distantWorldStabilization; _subpixelThreshold = Mathf.Clamp(subpixelThreshold, 0.5f, 4f);
            _taaHistoryWeight = Mathf.Clamp01(taaHistoryWeight); _taaSharpening = Mathf.Clamp01(taaSharpening);
        }

        private static int NormalizeMsaa(int value) => value >= 8 ? 8 : value >= 4 ? 4 : value >= 2 ? 2 : 1;
    }

    public readonly struct RenderingStabilityMetrics
    {
        public RenderingStabilityMetrics(RenderingQualityTier tier, ImageAntiAliasingMode mode, int msaa,
            float renderScale, float lodBias, float mipBias, AnisotropicFiltering anisotropic, bool temporal,
            int width, int height, float cpuMilliseconds, float gpuMilliseconds)
        { Tier = tier; Mode = mode; Msaa = msaa; RenderScale = renderScale; LodBias = lodBias; MipBias = mipBias; Anisotropic = anisotropic; Temporal = temporal; Width = width; Height = height; CpuMilliseconds = cpuMilliseconds; GpuMilliseconds = gpuMilliseconds; }
        public RenderingQualityTier Tier { get; } public ImageAntiAliasingMode Mode { get; } public int Msaa { get; }
        public float RenderScale { get; } public float LodBias { get; } public float MipBias { get; }
        public AnisotropicFiltering Anisotropic { get; } public bool Temporal { get; }
        public int Width { get; } public int Height { get; } public float CpuMilliseconds { get; } public float GpuMilliseconds { get; }
    }

    [DisallowMultipleComponent]
    public sealed class RenderingQualityManager : MonoBehaviour
    {
        [SerializeField] private RenderingQualityProfile[] _profiles = Array.Empty<RenderingQualityProfile>();
        [SerializeField] private RenderingQualityTier _startupTier = RenderingQualityTier.High;
        [SerializeField] private Camera _mainCamera;
        private UniversalRenderPipelineAsset _pipeline;
        private RenderingQualityProfile _active;
        private float _smoothedCpuMs;
        private int _originalMsaa;
        private float _originalRenderScale;
        private float _originalLodBias;
        private AnisotropicFiltering _originalAnisotropic;
        private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
        private RenderingQualityProfile _temporaryProfile;

        public RenderingQualityProfile ActiveProfile => _active;
        public RenderingStabilityMetrics Metrics { get; private set; }
        public void ConfigureProfiles(RenderingQualityProfile[] profiles, RenderingQualityTier startupTier)
        { _profiles = profiles ?? Array.Empty<RenderingQualityProfile>(); _startupTier = startupTier; }

        private void Awake()
        {
            if (_profiles == null || _profiles.Length == 0) _profiles = Resources.LoadAll<RenderingQualityProfile>("RenderingQuality");
            if (_profiles != null && _profiles.Length > 0) Initialize(Camera.main);
        }

        public void Initialize(Camera mainCamera, RenderingQualityProfile[] profiles = null)
        {
            _mainCamera = mainCamera != null ? mainCamera : Camera.main;
            if (profiles != null && profiles.Length > 0) _profiles = profiles;
            _pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (_pipeline == null) throw new InvalidOperationException("RenderingQualityManager requires URP.");
            _originalMsaa = _pipeline.msaaSampleCount; _originalRenderScale = _pipeline.renderScale;
            _originalLodBias = QualitySettings.lodBias; _originalAnisotropic = QualitySettings.anisotropicFiltering;
            Apply(_startupTier);
        }

        public void Apply(RenderingQualityTier tier)
        {
            RenderingQualityProfile profile = Find(tier);
            if (profile == null) throw new InvalidOperationException($"Rendering profile {tier} is not configured.");
            Apply(profile);
        }

        public void Apply(RenderingQualityProfile profile)
        {
            if (profile == null || _pipeline == null || _mainCamera == null) return;
            _active = profile;
            UniversalAdditionalCameraData cameraData = _mainCamera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = profile.AntiAliasing == ImageAntiAliasingMode.Fxaa ||
                profile.AntiAliasing == ImageAntiAliasingMode.Smaa || profile.AntiAliasing == ImageAntiAliasingMode.Temporal;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
            cameraData.antialiasing = ResolveUrpMode(profile.AntiAliasing);
            bool msaa = profile.AntiAliasing == ImageAntiAliasingMode.Msaa;
            _pipeline.msaaSampleCount = msaa ? profile.MsaaSamples : 1;
            _mainCamera.allowMSAA = msaa;
            _pipeline.renderScale = profile.RenderScale;
            QualitySettings.lodBias = profile.LodBias;
            QualitySettings.anisotropicFiltering = profile.AnisotropicFiltering;
            Shader.SetGlobalFloat("_WorldMipBias", profile.MipMapBias);
            Shader.SetGlobalFloat("_WorldAlphaToCoverage", profile.AlphaToCoverage && msaa ? 1f : 0f);
            Shader.SetGlobalFloat("_WorldSubpixelThreshold", profile.SubpixelThreshold);
            if (profile.AntiAliasing == ImageAntiAliasingMode.Temporal)
            {
                ref TemporalAA.Settings taa = ref cameraData.taaSettings;
                taa.quality = TemporalAAQuality.High;
                taa.baseBlendFactor = profile.TaaHistoryWeight;
                taa.jitterScale = 0.85f;
                taa.mipBias = profile.MipMapBias;
                taa.varianceClampScale = 0.9f;
                taa.contrastAdaptiveSharpening = profile.TaaSharpening;
                cameraData.resetHistory = true;
            }
            DistantWorldRenderer distant = FindAnyObjectByType<DistantWorldRenderer>();
            distant?.ApplyImageStability(profile.LodBias, profile.SubpixelThreshold,
                profile.DistantWorldStabilization, profile.TemporalStability);
            FindAnyObjectByType<ProceduralRuntimeManager>()?.ConfigureImageStability(profile.LodBias, profile.SubpixelThreshold);
        }

        public void CycleTier() => Apply((RenderingQualityTier)(((int)(_active != null ? _active.Tier : _startupTier) + 1) % 4));
        public void Reapply() { if (_active != null) Apply(_active); }
        public void SetDebugAa(ImageAntiAliasingMode mode)
        {
            RenderingQualityProfile basis = _active;
            if (basis == null || basis == _temporaryProfile)
                basis = Find(_temporaryProfile != null ? _temporaryProfile.Tier : _startupTier);

            if (_temporaryProfile != null) Destroy(_temporaryProfile);
            RenderingQualityProfile temporary = ScriptableObject.CreateInstance<RenderingQualityProfile>();
            temporary.Configure(basis.Tier, mode, mode == ImageAntiAliasingMode.Msaa ? 4 : 1, basis.RenderScale,
                mode == ImageAntiAliasingMode.Temporal, basis.AnisotropicFiltering, basis.LodBias, basis.MipMapBias,
                basis.AlphaToCoverage, basis.DistantWorldStabilization, basis.SubpixelThreshold);
            Apply(temporary);
            _temporaryProfile = temporary;
        }
        public void CycleAaMode() => SetDebugAa((ImageAntiAliasingMode)(((int)(_active != null ? _active.AntiAliasing : ImageAntiAliasingMode.Off) + 1) % 5));

        private void Update()
        {
            if (_active == null) return;
            _smoothedCpuMs = Mathf.Lerp(_smoothedCpuMs, Time.unscaledDeltaTime * 1000f, 0.08f);
            float gpuMs = 0f; FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, _frameTimings) > 0) gpuMs = (float)_frameTimings[0].gpuFrameTime;
            Metrics = new RenderingStabilityMetrics(_active.Tier, _active.AntiAliasing,
                _active.AntiAliasing == ImageAntiAliasingMode.Msaa ? _active.MsaaSamples : 1,
                _active.RenderScale, _active.LodBias, _active.MipMapBias, _active.AnisotropicFiltering,
                _active.AntiAliasing == ImageAntiAliasingMode.Temporal, Screen.width, Screen.height, _smoothedCpuMs, gpuMs);
        }

        private RenderingQualityProfile Find(RenderingQualityTier tier)
        { for (int i = 0; i < _profiles.Length; i++) if (_profiles[i] != null && _profiles[i].Tier == tier) return _profiles[i]; return null; }
        private static AntialiasingMode ResolveUrpMode(ImageAntiAliasingMode mode)
        { switch (mode) { case ImageAntiAliasingMode.Fxaa: return AntialiasingMode.FastApproximateAntialiasing; case ImageAntiAliasingMode.Smaa: return AntialiasingMode.SubpixelMorphologicalAntiAliasing; case ImageAntiAliasingMode.Temporal: return AntialiasingMode.TemporalAntiAliasing; default: return AntialiasingMode.None; } }
        private void OnDestroy()
        { if (_pipeline != null) { _pipeline.msaaSampleCount = _originalMsaa; _pipeline.renderScale = _originalRenderScale; } QualitySettings.lodBias = _originalLodBias; QualitySettings.anisotropicFiltering = _originalAnisotropic; if (_temporaryProfile != null) Destroy(_temporaryProfile); }
    }
}

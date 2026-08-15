using System.Text;
using UnityEngine;

namespace MyGameWorld.Client.ProceduralWorld
{
    [DisallowMultipleComponent]
    public sealed class ProceduralWorldDebugHud : MonoBehaviour
    {
        private readonly StringBuilder _text = new StringBuilder(512);
        private GUIStyle _boxStyle;
        private GUIStyle _textStyle;
        private ProceduralWorldSandbox _sandbox;
        private DevelopmentFreeCamera _developmentCamera;
        private float _smoothedDelta = 1f / 60f;

        private void Start()
        {
            _sandbox = GetComponent<ProceduralWorldSandbox>();
            _developmentCamera = FindAnyObjectByType<DevelopmentFreeCamera>();
        }

        private void Update()
        {
            _smoothedDelta = Mathf.Lerp(_smoothedDelta, Time.unscaledDeltaTime, 0.08f);
        }

        private void OnGUI()
        {
            if (_sandbox == null || !_sandbox.IsHudVisible)
            {
                return;
            }

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = Texture2D.whiteTexture }
                };
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 14,
                    richText = true,
                    padding = new RectOffset(12, 12, 10, 10)
                };
                _textStyle.normal.textColor = new Color(0.93f, 0.97f, 0.91f);
            }

            Color previousColor = GUI.color;
            GUI.color = new Color(0.045f, 0.075f, 0.06f, 0.88f);
            GUI.Box(new Rect(16f, 16f, 370f, 385f), GUIContent.none, _boxStyle);
            GUI.color = previousColor;

            Vector3 cameraPosition = _developmentCamera != null
                ? _developmentCamera.transform.position
                : Vector3.zero;
            float fps = _smoothedDelta > 0f ? 1f / _smoothedDelta : 0f;
            _text.Clear();
            _text.AppendLine("<b>PROCEDURAL WORLD SANDBOX</b>");
            _text.Append("FPS: ").Append(fps.ToString("0")).AppendLine();
            _text.Append("Camera: ").Append(cameraPosition.ToString("F1")).AppendLine();
            _text.Append("Camera speed: ").Append(_developmentCamera != null ? _developmentCamera.MovementSpeed.ToString("0.0") : "-").AppendLine();
            _text.AppendLine();
            _text.Append("Zone: TEST_").Append(_sandbox.ZoneId.ToString("000")).AppendLine();
            _text.Append("Seed: ").Append(_sandbox.ZoneSeed).AppendLine();
            _text.Append("Generator: V").Append(_sandbox.GeneratorVersion).AppendLine();
            _text.Append("Fingerprint: ").Append(_sandbox.Fingerprint.ToString("X16")).AppendLine();
            _text.Append("Generated: ").Append(_sandbox.GenerationMilliseconds.ToString("0.0")).AppendLine(" ms");
            _text.AppendLine();
            _text.Append("Terrain: ").Append(_sandbox.TerrainWidth.ToString("0")).Append(" × ").Append(_sandbox.TerrainDepth.ToString("0")).AppendLine(" m");
            _text.Append("Grid: ").Append(_sandbox.ResolvedResolution).Append(" × ").Append(_sandbox.ResolvedResolution).AppendLine();
            _text.Append("Logical vertices: ").Append(_sandbox.LogicalVertexCount).AppendLine();
            _text.Append("Rendered vertices: ").Append(_sandbox.RenderedVertexCount).AppendLine();
            _text.Append("Triangles: ").Append(_sandbox.TriangleCount).Append(" / ").Append(_sandbox.TriangleBudget).AppendLine(" budget");
            _text.Append("Chunks: ").Append(_sandbox.ChunkCount).AppendLine();
            _text.Append("Singular terrain elements: ").Append(_sandbox.SingularTerrainFeatureCount).AppendLine();
            _text.Append("Objects: ").Append(_sandbox.DecorationCount)
                .Append(" (T ").Append(_sandbox.TreeCount)
                .Append(", R ").Append(_sandbox.RockCount)
                .Append(", B ").Append(_sandbox.BushCount).AppendLine(")");
            ProceduralRuntimeMetrics metrics = _sandbox.RuntimeMetrics;
            _text.Append("Queue: ").Append(metrics.QueueCount)
                .Append(" | Mesh cache: ").Append(metrics.CachedMeshes).AppendLine();
            _text.Append("Cache H/M: ").Append(metrics.CacheHits).Append('/').Append(metrics.CacheMisses)
                .Append(" | Generated: ").Append(metrics.GeneratedMeshes)
                .Append(" | Assets: ").Append(metrics.ResolvedFiniteAssets).AppendLine();
            _text.Append("Object geometry: ").Append(metrics.VisibleVertices).Append(" vertices / ")
                .Append(metrics.VisibleTriangles).AppendLine(" triangles");
            _text.Append("Estimated renderer passes: ").Append(metrics.EstimatedDrawCalls).AppendLine();
            _text.Append("Runtime generation: ").Append(metrics.LastFrameGenerationMilliseconds.ToString("0.00")).AppendLine(" ms");
            _text.AppendLine();
            _text.AppendLine("WASD move | Q/E down/up | RMB look");
            _text.AppendLine("Shift fast | Scroll speed");
            _text.AppendLine("F1 HUD | F2 wire | F3 same | F4 next seed");

            GUI.Label(new Rect(20f, 20f, 362f, 377f), _text.ToString(), _textStyle);
        }
    }
}

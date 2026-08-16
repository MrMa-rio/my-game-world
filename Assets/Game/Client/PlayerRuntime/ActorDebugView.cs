using System;
using System.Text;
using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using UnityEngine;

namespace MyGameWorld.Client.PlayerRuntime
{
    public readonly struct ActorDebugSnapshot
    {
        public ActorDebugSnapshot(long entityId, GlobalPosition worldPosition, WorldSpatialContext spatial,
            string controller, int capabilities, int sensors, ProprioceptionSnapshot proprioception,
            int biomeId, int surfaceId, PlayerCameraModeId? cameraMode)
        {
            EntityId = entityId; WorldPosition = worldPosition; Spatial = spatial; Controller = controller;
            CapabilityCount = capabilities; SensorCount = sensors; Proprioception = proprioception;
            BiomeId = biomeId; SurfaceId = surfaceId; CameraMode = cameraMode;
        }
        public long EntityId { get; }
        public GlobalPosition WorldPosition { get; }
        public WorldSpatialContext Spatial { get; }
        public string Controller { get; }
        public int CapabilityCount { get; }
        public int SensorCount { get; }
        public ProprioceptionSnapshot Proprioception { get; }
        public int BiomeId { get; }
        public int SurfaceId { get; }
        public PlayerCameraModeId? CameraMode { get; }
    }

    [DisallowMultipleComponent]
    public sealed class ActorDebugView : MonoBehaviour
    {
        [SerializeField] private bool _showHud = true;
        [SerializeField] private bool _drawGizmos = true;
        private readonly StringBuilder _text = new StringBuilder(512);
        private Actor _actor; private PlayerCameraSystem _camera;
        private IProprioceptionSensor _proprioception; private IEnvironmentContextSensor _environment;
        public ActorDebugSnapshot Snapshot { get; private set; }

        public void Initialize(Actor actor, PlayerCameraSystem camera = null)
        {
            _actor = actor != null && actor.IsInitialized ? actor : throw new InvalidOperationException("Actor debug requires an initialized Actor.");
            _camera = camera; _actor.Sensors.TryGet(out _proprioception); _actor.Sensors.TryGet(out _environment); RefreshSnapshot();
        }
        public void RefreshSnapshot()
        {
            if (_actor == null) return;
            WorldEnvironmentSnapshot environment = _environment != null ? _environment.Current : default;
            Snapshot = new ActorDebugSnapshot(_actor.Entity.EntityId.Value, _actor.Context.Presence.GlobalPosition,
                _actor.Context.Presence.SpatialContext, _actor.Controller?.GetType().Name ?? "None",
                _actor.Capabilities.Count, _actor.Sensors.Count, _proprioception != null ? _proprioception.Current : default,
                environment.BiomeId, environment.SurfaceId, _camera?.Modes.ActiveMode?.Id);
        }
        private void LateUpdate() => RefreshSnapshot();
        private void OnGUI()
        {
            if (!_showHud || _actor == null) return; _text.Clear(); ActorDebugSnapshot s = Snapshot;
            _text.Append("Entity: ").Append(s.EntityId).Append("  Controller: ").AppendLine(s.Controller);
            _text.Append("World: ").Append(s.WorldPosition).Append("  Cell: ").AppendLine(s.Spatial.Cell?.ToString() ?? "None");
            _text.Append("Biome/Surface: ").Append(s.BiomeId).Append('/').AppendLine(s.SurfaceId.ToString());
            _text.Append("Capabilities/Sensors: ").Append(s.CapabilityCount).Append('/').AppendLine(s.SensorCount.ToString());
            _text.Append("Grounded: ").Append(s.Proprioception.IsGrounded).Append(" Velocity: ").Append(s.Proprioception.Velocity)
                .Append(" Slope: ").Append(s.Proprioception.Slope.ToString("0.0")).Append(" State: ").AppendLine(s.Proprioception.MovementState.ToString());
            _text.Append("Camera: ").Append(s.CameraMode?.ToString() ?? "None");
            GUI.Box(new Rect(10f, 70f, 560f, 125f), _text.ToString());
        }
        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos || _actor == null) return; Vector3 origin = _actor.transform.position;
            Gizmos.color = Color.green; Gizmos.DrawLine(origin + Vector3.up, origin + Vector3.down * 0.3f);
            if (_actor.Sensors.TryGet(out IVisionSensor vision) && vision is VisionSensor concreteVision && concreteVision.Profile != null)
            {
                Vector3 eye = origin + Vector3.up * concreteVision.Profile.EyeHeight; float half = concreteVision.Profile.FieldOfView * 0.5f;
                Gizmos.color = Color.yellow; Gizmos.DrawRay(eye, Quaternion.Euler(0f, -half, 0f) * _actor.transform.forward * concreteVision.Profile.Range);
                Gizmos.DrawRay(eye, Quaternion.Euler(0f, half, 0f) * _actor.transform.forward * concreteVision.Profile.Range);
            }
            if (_actor.Sensors.TryGet(out IHearingSensor hearing) && hearing is HearingSensor concreteHearing && concreteHearing.Profile != null)
            { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(origin, concreteHearing.Profile.BaseRange); }
            if (_actor.Sensors.TryGet(out ISmellSensor smell) && smell is SmellSensor concreteSmell && concreteSmell.Profile != null)
            { Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(origin, concreteSmell.Profile.Range); }
            if (_camera != null && _camera.IsInitialized)
            { Gizmos.color = Color.white; Gizmos.DrawLine(origin + Vector3.up * 1.6f, _camera.Rig.Root.position); }
        }
    }
}

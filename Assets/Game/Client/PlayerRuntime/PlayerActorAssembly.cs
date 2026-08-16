using System;
using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.Core;
using MyGameWorld.Shared.World;
using UnityEngine;
using UnityEngine.InputSystem;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Client.PlayerRuntime
{
    public sealed class PlayerAssemblyRequest
    {
        public EntityId EntityId { get; set; }
        public GlobalPosition GlobalPosition { get; set; }
        public WorldCoordinateFrame CoordinateFrame { get; set; }
        public IWorldEntityRegistry EntityRegistry { get; set; }
        public ActorLocomotionScheduler LocomotionScheduler { get; set; }
        public ActorSensorScheduler SensorScheduler { get; set; }
        public PerceptionSoundStream SoundStream { get; set; }
        public ScentField ScentField { get; set; }
        public InputActionAsset InputActions { get; set; }
        public LocomotionProfile Locomotion { get; set; }
        public WalkProfile Walk { get; set; }
        public RunProfile Run { get; set; }
        public JumpProfile Jump { get; set; }
        public PhysicalBodyProfile PhysicalBody { get; set; }
        public VisionProfile Vision { get; set; }
        public HearingProfile Hearing { get; set; }
        public SmellProfile Smell { get; set; }
    }

    public sealed class PlayerActorAssembly
    {
        public Actor Assemble(GameObject root, PlayerAssemblyRequest request)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            Validate(request);
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(request.EntityId, request.GlobalPosition, request.CoordinateFrame, request.EntityRegistry);
            entity.Spawn(); entity.Activate();
            Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity);

            ActorLocomotion locomotion = root.AddComponent<ActorLocomotion>();
            locomotion.Configure(request.Locomotion, request.LocomotionScheduler, WorldPhysicsLayers.GroundMask);
            actor.AddCapability<IActorLocomotion>(locomotion);
            WalkCapability walk = root.AddComponent<WalkCapability>(); walk.Configure(request.Walk); actor.AddCapability<IWalkCapability>(walk);
            RunCapability run = root.AddComponent<RunCapability>(); run.Configure(request.Run); actor.AddCapability<IRunCapability>(run);
            JumpCapability jump = root.AddComponent<JumpCapability>(); jump.Configure(request.Jump); actor.AddCapability<IJumpCapability>(jump);
            PhysicalBody body = root.AddComponent<PhysicalBody>(); body.Configure(request.PhysicalBody); actor.AddCapability<IPhysicalBody>(body);

            ProprioceptionSensor proprioception = root.AddComponent<ProprioceptionSensor>();
            proprioception.ConfigureScheduling(SensorTickMode.Physics, scheduler: request.SensorScheduler);
            actor.AddSensor<IProprioceptionSensor>(proprioception);
            TouchSensor touch = root.AddComponent<TouchSensor>(); touch.ConfigureScheduling(SensorTickMode.EventDriven); actor.AddSensor<ITouchSensor>(touch);
            VisionSensor vision = root.AddComponent<VisionSensor>(); vision.Configure(request.Vision);
            vision.ConfigureScheduling(SensorTickMode.Interval, 0.2f, request.SensorScheduler); actor.AddSensor<IVisionSensor>(vision);
            HearingSensor hearing = root.AddComponent<HearingSensor>(); hearing.Configure(request.Hearing, request.SoundStream);
            hearing.ConfigureScheduling(SensorTickMode.EventDriven); actor.AddSensor<IHearingSensor>(hearing);
            SmellSensor smell = root.AddComponent<SmellSensor>(); smell.Configure(request.Smell, request.ScentField);
            smell.ConfigureScheduling(SensorTickMode.Interval, 0.5f, request.SensorScheduler); actor.AddSensor<ISmellSensor>(smell);
            TasteSensor taste = root.AddComponent<TasteSensor>(); taste.ConfigureScheduling(SensorTickMode.EventDriven); actor.AddSensor<ITasteSensor>(taste);

            HumanActorController controller = root.AddComponent<HumanActorController>(); controller.Configure(request.InputActions);
            actor.SetController(controller);
            return actor;
        }

        private static void Validate(PlayerAssemblyRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.EntityRegistry == null || request.LocomotionScheduler == null ||
                request.SensorScheduler == null || request.SoundStream == null || request.ScentField == null || request.InputActions == null ||
                request.Locomotion == null || request.Walk == null || request.Run == null || request.Jump == null ||
                request.PhysicalBody == null || request.Vision == null || request.Hearing == null || request.Smell == null)
                throw new InvalidOperationException("Player assembly request is incomplete.");
        }
    }
}

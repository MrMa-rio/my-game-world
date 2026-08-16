using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Client.PlayerRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class PlayerActorAssemblyTests
    {
        [Test]
        public void Assemble_CompleteRequest_ComposesPlayerWithoutDuplicatingActorSystems()
        {
            GameObject root = new GameObject("Player Assembly Test"); GameObject services = new GameObject("Player Services Test");
            PlayerAssemblyRequest request = CreateRequest(services);
            try
            {
                Actor actor = new PlayerActorAssembly().Assemble(root, request);
                Assert.That(actor.Entity.Lifecycle.State, Is.EqualTo(WorldEntityLifecycleState.Active));
                Assert.That(actor.Controller, Is.TypeOf<HumanActorController>());
                Assert.That(actor.Capabilities.Count, Is.EqualTo(5));
                Assert.That(actor.Sensors.Count, Is.EqualTo(6));
                Assert.That(actor.Capabilities.TryGet(out IActorLocomotion _), Is.True);
                Assert.That(actor.Sensors.TryGet(out IProprioceptionSensor _), Is.True);
                Assert.That(actor.Sensors.TryGet(out ITasteSensor _), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(services); DestroyProfiles(request);
            }
        }

        private static PlayerAssemblyRequest CreateRequest(GameObject services)
        {
            InputActionAsset input = ScriptableObject.CreateInstance<InputActionAsset>(); InputActionMap map = input.AddActionMap("Player");
            map.AddAction("Move", InputActionType.Value); map.AddAction("Look", InputActionType.Value);
            map.AddAction("Sprint", InputActionType.Button); map.AddAction("Jump", InputActionType.Button); map.AddAction("Interact", InputActionType.Button);
            return new PlayerAssemblyRequest
            {
                EntityId = new EntityId(1801), GlobalPosition = new GlobalPosition(0d, 0d, 0d),
                CoordinateFrame = new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), EntityRegistry = new WorldEntityRegistry(),
                LocomotionScheduler = services.AddComponent<ActorLocomotionScheduler>(), SensorScheduler = services.AddComponent<ActorSensorScheduler>(),
                SoundStream = new PerceptionSoundStream(), ScentField = new ScentField(), InputActions = input,
                Locomotion = ScriptableObject.CreateInstance<LocomotionProfile>(), Walk = ScriptableObject.CreateInstance<WalkProfile>(),
                Run = ScriptableObject.CreateInstance<RunProfile>(), Jump = ScriptableObject.CreateInstance<JumpProfile>(),
                PhysicalBody = ScriptableObject.CreateInstance<PhysicalBodyProfile>(), Vision = ScriptableObject.CreateInstance<VisionProfile>(),
                Hearing = ScriptableObject.CreateInstance<HearingProfile>(), Smell = ScriptableObject.CreateInstance<SmellProfile>()
            };
        }

        private static void DestroyProfiles(PlayerAssemblyRequest request)
        {
            Object.DestroyImmediate(request.InputActions); Object.DestroyImmediate(request.Locomotion); Object.DestroyImmediate(request.Walk);
            Object.DestroyImmediate(request.Run); Object.DestroyImmediate(request.Jump); Object.DestroyImmediate(request.PhysicalBody);
            Object.DestroyImmediate(request.Vision); Object.DestroyImmediate(request.Hearing); Object.DestroyImmediate(request.Smell);
        }
    }
}

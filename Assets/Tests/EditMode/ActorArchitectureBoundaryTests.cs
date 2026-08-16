using System;
using System.Linq;
using System.Reflection;
using MyGameWorld.Client.ActorRuntime;
using NUnit.Framework;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ActorArchitectureBoundaryTests
    {
        [Test]
        public void ActorRuntimeAssembly_DoesNotReferencePlayerPresentationAssembly()
        {
            Assembly actorAssembly = typeof(Actor).Assembly;
            string[] references = actorAssembly.GetReferencedAssemblies().Select(value => value.Name).ToArray();
            Assert.That(references, Does.Not.Contain("MyGameWorld.Client.PlayerRuntime"));
        }

        [Test]
        public void GenericActionSystems_DoNotDependOnHumanCameraOrHudTypes()
        {
            Type[] genericSystems = { typeof(Actor), typeof(ActorLocomotion), typeof(WalkCapability),
                typeof(RunCapability), typeof(JumpCapability), typeof(PhysicalBody), typeof(ActorAnimationDriver) };
            foreach (Type system in genericSystems)
            foreach (FieldInfo field in system.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                string dependency = field.FieldType.FullName ?? string.Empty;
                Assert.That(dependency, Does.Not.Contain("PlayerRuntime"), $"{system.Name}.{field.Name}");
                Assert.That(dependency, Does.Not.Contain("HumanActorController"), $"{system.Name}.{field.Name}");
                Assert.That(dependency, Does.Not.Contain("UnityEngine.Camera"), $"{system.Name}.{field.Name}");
            }
        }

        [Test]
        public void CapabilitiesAndSensors_DoNotDeclarePerInstanceFrameLoops()
        {
            Type[] systems = { typeof(ActorLocomotion), typeof(WalkCapability), typeof(RunCapability), typeof(JumpCapability),
                typeof(ProprioceptionSensor), typeof(TouchSensor), typeof(VisionSensor), typeof(HearingSensor), typeof(SmellSensor), typeof(TasteSensor) };
            foreach (Type system in systems)
            {
                Assert.That(system.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), Is.Null, system.Name);
                Assert.That(system.GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), Is.Null, system.Name);
            }
        }
    }
}

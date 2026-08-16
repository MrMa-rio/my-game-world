using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class HearingSensorTests
    {
        [Test]
        public void Emit_SoundInRange_IsPerceivedWithoutAudioSource()
        {
            GameObject root = new GameObject("Hearing Actor Test");
            HearingProfile profile = ScriptableObject.CreateInstance<HearingProfile>();
            try
            {
                Actor actor = CreateActor(root); PerceptionSoundStream stream = new PerceptionSoundStream();
                HearingSensor hearing = root.AddComponent<HearingSensor>();
                hearing.Configure(profile, stream); hearing.ConfigureScheduling(SensorTickMode.EventDriven);
                actor.AddSensor<IHearingSensor>(hearing);
                int heard = 0; HeardSound perceived = default;
                hearing.SoundHeard += sound => { heard++; perceived = sound; };
                PerceptionSoundEvent emitted = new PerceptionSoundEvent(new Vector3(0f, 0f, 5f), 1f, PerceptionSoundCategory.Impact);

                stream.Emit(in emitted);

                Assert.That(heard, Is.EqualTo(1));
                Assert.That(perceived.Source.Category, Is.EqualTo(PerceptionSoundCategory.Impact));
                Assert.That(perceived.PerceivedIntensity, Is.GreaterThan(0f));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }

        [Test]
        public void Emit_SoundOutsideRange_IsIgnored()
        {
            GameObject root = new GameObject("Hearing Range Test");
            HearingProfile profile = ScriptableObject.CreateInstance<HearingProfile>();
            try
            {
                Actor actor = CreateActor(root); PerceptionSoundStream stream = new PerceptionSoundStream();
                HearingSensor hearing = root.AddComponent<HearingSensor>();
                hearing.Configure(profile, stream); hearing.ConfigureScheduling(SensorTickMode.EventDriven);
                actor.AddSensor<IHearingSensor>(hearing); int heard = 0; hearing.SoundHeard += _ => heard++;
                PerceptionSoundEvent emitted = new PerceptionSoundEvent(new Vector3(0f, 0f, 1000f), 1f, PerceptionSoundCategory.General);
                stream.Emit(in emitted);
                Assert.That(heard, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(profile); }
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(1501), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate(); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }
    }
}

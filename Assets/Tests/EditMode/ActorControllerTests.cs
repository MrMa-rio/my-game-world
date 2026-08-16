using MyGameWorld.Client.ActorRuntime;
using MyGameWorld.Client.EntityRuntime;
using MyGameWorld.Shared.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using EntityId = MyGameWorld.Shared.Core.EntityId;

namespace MyGameWorld.Tests.EditMode
{
    public sealed class ActorControllerTests
    {
        [Test]
        public void HumanController_ProcessInput_ProducesIntentsWithoutExecutingMovement()
        {
            GameObject root = new GameObject("Human Controller Test"); InputActionAsset actions = CreateInputActions();
            try
            {
                Actor actor = CreateActor(root); IntentRecorder capability = root.AddComponent<IntentRecorder>();
                actor.AddCapability<IIntentRecorder>(capability);
                HumanActorController controller = root.AddComponent<HumanActorController>(); controller.Configure(actions);
                actor.SetController(controller);
                HumanInputSnapshot snapshot = new HumanInputSnapshot(new Vector2(0.5f, 1f), new Vector2(3f, -2f), true, true, true);
                controller.ProcessInput(in snapshot);
                Assert.That(capability.Move.Direction.magnitude, Is.EqualTo(1f).Within(0.001f));
                Assert.That(capability.Run.Requested, Is.True); Assert.That(capability.Look.Delta, Is.EqualTo(new Vector2(3f, -2f)));
                Assert.That(capability.JumpCount, Is.EqualTo(1)); Assert.That(capability.InteractCount, Is.EqualTo(1));
                Assert.That(root.transform.position, Is.EqualTo(Vector3.zero));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(actions); }
        }

        [Test]
        public void Actor_SetController_CanReplaceHumanWithAnotherDecisionSource()
        {
            GameObject root = new GameObject("Replace Human Controller Test"); InputActionAsset actions = CreateInputActions();
            try
            {
                Actor actor = CreateActor(root); HumanActorController human = root.AddComponent<HumanActorController>(); human.Configure(actions);
                TestController replacement = new TestController(); actor.SetController(human); actor.SetController(replacement);
                Assert.That(human.IsBound, Is.False); Assert.That(replacement.IsBound, Is.True);
                Assert.That(actor.Controller, Is.SameAs(replacement));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(actions); }
        }

        [Test]
        public void HumanController_ConfigurationCannotChangeWhileBound()
        {
            GameObject root = new GameObject("Bound Human Controller Test"); InputActionAsset actions = CreateInputActions();
            try
            {
                Actor actor = CreateActor(root); HumanActorController human = root.AddComponent<HumanActorController>(); human.Configure(actions);
                actor.SetController(human);
                Assert.Throws<System.InvalidOperationException>(() => human.Configure(actions));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(actions); }
        }

        private static InputActionAsset CreateInputActions()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>(); InputActionMap map = new InputActionMap("Player");
            InputAction move = map.AddAction("Move", InputActionType.Value); move.expectedControlType = "Vector2";
            InputAction look = map.AddAction("Look", InputActionType.Value); look.expectedControlType = "Vector2";
            map.AddAction("Sprint", InputActionType.Button); map.AddAction("Jump", InputActionType.Button); map.AddAction("Interact", InputActionType.Button);
            asset.AddActionMap(map); return asset;
        }

        private static Actor CreateActor(GameObject root)
        {
            WorldEntity entity = root.AddComponent<WorldEntity>();
            entity.Initialize(new EntityId(501), new GlobalPosition(0d, 0d, 0d),
                new WorldCoordinateFrame(new GlobalPosition(0d, 0d, 0d)), new WorldEntityRegistry());
            entity.Spawn(); entity.Activate(); Actor actor = root.AddComponent<Actor>(); actor.Initialize(entity); return actor;
        }

        private interface IIntentRecorder : IActorCapability { }
        private sealed class IntentRecorder : ActorCapability, IIntentRecorder, IActorIntentHandler<MoveIntent>,
            IActorIntentHandler<LookIntent>, IActorIntentHandler<RunIntent>, IActorIntentHandler<JumpIntent>, IActorIntentHandler<InteractIntent>
        {
            public MoveIntent Move { get; private set; } public LookIntent Look { get; private set; } public RunIntent Run { get; private set; }
            public int JumpCount { get; private set; } public int InteractCount { get; private set; }
            protected override void OnInitialized()
            {
                RegisterIntentHandler<MoveIntent>(this); RegisterIntentHandler<LookIntent>(this); RegisterIntentHandler<RunIntent>(this);
                RegisterIntentHandler<JumpIntent>(this); RegisterIntentHandler<InteractIntent>(this);
            }
            public void HandleIntent(in MoveIntent intent) => Move = intent;
            public void HandleIntent(in LookIntent intent) => Look = intent;
            public void HandleIntent(in RunIntent intent) => Run = intent;
            public void HandleIntent(in JumpIntent intent) => JumpCount++;
            public void HandleIntent(in InteractIntent intent) => InteractCount++;
        }

        private sealed class TestController : IActorController
        {
            public bool IsBound { get; private set; }
            public void Bind(ActorContext context) => IsBound = true;
            public void Unbind() => IsBound = false;
        }
    }
}

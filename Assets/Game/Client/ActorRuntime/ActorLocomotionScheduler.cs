using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGameWorld.Client.ActorRuntime
{
    [DisallowMultipleComponent]
    public sealed class ActorLocomotionScheduler : MonoBehaviour
    {
        private readonly List<ActorLocomotion> _locomotions = new List<ActorLocomotion>();
        public int RegisteredCount => _locomotions.Count;

        public void Register(ActorLocomotion locomotion)
        {
            if (locomotion == null) throw new ArgumentNullException(nameof(locomotion));
            if (_locomotions.Contains(locomotion)) throw new InvalidOperationException("Locomotion is already scheduled.");
            _locomotions.Add(locomotion);
        }

        public void Unregister(ActorLocomotion locomotion) => _locomotions.Remove(locomotion);

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            for (int index = _locomotions.Count - 1; index >= 0; index--)
            {
                ActorLocomotion locomotion = _locomotions[index];
                if (locomotion == null) { _locomotions.RemoveAt(index); continue; }
                locomotion.Simulate(deltaTime);
            }
        }
    }
}

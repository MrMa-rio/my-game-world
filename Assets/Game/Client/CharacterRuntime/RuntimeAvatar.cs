using System;
using System.Collections.Generic;
using MyGameWorld.Shared.Procedural;
using UnityEngine;

namespace MyGameWorld.Client.CharacterRuntime
{
    public sealed class RuntimeAvatar : MonoBehaviour
    {
        private readonly Dictionary<CharacterPartSlot, Transform> _anchors = new Dictionary<CharacterPartSlot, Transform>();
        private readonly List<GameObject> _parts = new List<GameObject>();

        public CharacterAppearanceDNA Appearance { get; private set; }
        public int PartCount => _parts.Count;

        public void RegisterAnchor(CharacterPartSlot slot, Transform anchor)
        {
            if (anchor == null) throw new ArgumentNullException(nameof(anchor));
            _anchors[slot] = anchor;
        }

        internal Transform ResolveAnchor(CharacterPartSlot slot) => _anchors.TryGetValue(slot, out Transform anchor) ? anchor : transform;
        internal void Initialize(CharacterAppearanceDNA appearance) => Appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        internal void AddPart(GameObject part) => _parts.Add(part);

        internal void ResetAvatar()
        {
            for (int i = _parts.Count - 1; i >= 0; i--)
                if (_parts[i] != null) DestroyObject(_parts[i]);
            _parts.Clear(); Appearance = null;
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (Application.isPlaying) Destroy(target); else DestroyImmediate(target);
        }
    }
}

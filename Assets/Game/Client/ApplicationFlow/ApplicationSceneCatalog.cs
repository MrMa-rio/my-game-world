using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyGameWorld.Client.ApplicationFlow
{
    [CreateAssetMenu(menuName = "My Game World/Application Scene Catalog")]
    public sealed class ApplicationSceneCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [SerializeField]
            private SceneId _id;

            [SerializeField]
            private string _sceneName;

            public Entry(SceneId id, string sceneName)
            {
                _id = id;
                _sceneName = sceneName;
            }

            public SceneId Id => _id;

            public string SceneName => _sceneName;
        }

        [SerializeField]
        private List<Entry> _entries = new List<Entry>();

        public bool TryGetSceneName(SceneId id, out string sceneName)
        {
            for (int index = 0; index < _entries.Count; index++)
            {
                Entry entry = _entries[index];
                if (entry.Id != id)
                {
                    continue;
                }

                sceneName = entry.SceneName;
                return !string.IsNullOrWhiteSpace(sceneName);
            }

            sceneName = string.Empty;
            return false;
        }

#if UNITY_EDITOR
        public void Configure(IReadOnlyList<Entry> entries)
        {
            _entries.Clear();
            for (int index = 0; index < entries.Count; index++)
            {
                _entries.Add(entries[index]);
            }
        }
#endif
    }
}

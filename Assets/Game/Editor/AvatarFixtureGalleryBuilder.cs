using System;
using System.Collections.Generic;
using System.IO;
using MyGameWorld.Client.AssetResolution;
using MyGameWorld.Client.CharacterRuntime;
using MyGameWorld.Shared.Procedural;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameWorld.Editor
{
    public static class AvatarFixtureGalleryBuilder
    {
        private const string PartsRoot = "Assets/ia assets/avatar-reference/system-g6/normalized/parts";
        private const string ContentRoot = "Assets/Game/Content/AvatarValidation";
        private const string ScenePath = "Assets/Scenes/AvatarFixtureGallery.unity";

        [MenuItem("My Game World/Build Avatar Fixture Gallery")]
        public static void Build()
        {
            EnsureFolder("Assets/Game", "Content"); EnsureFolder("Assets/Game/Content", "AvatarValidation");
            List<UnityAssetBinding> bindings = new List<UnityAssetBinding>();
            List<AvatarPartCatalogEntry> entries = new List<AvatarPartCatalogEntry>();
            HashSet<uint> ids = new HashSet<uint>();
            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { PartsRoot });
            Array.Sort(guids, (left, right) => string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(left), AssetDatabase.GUIDToAssetPath(right)));
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid); string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (!TryClassify(name, out CharacterPartSlot slot, out AssetCategory category, out bool optional)) continue;
                uint id = StableAssetId(path); if (!ids.Add(id)) throw new InvalidOperationException($"Avatar AssetId collision at {path}.");
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                AssetTrait family = path.Contains("/parts/male/") ? AssetTrait.MasculineFrame : AssetTrait.FeminineFrame;
                AssetTrait required = AssetTrait.HumanoidSkeleton | AssetTrait.MediumFrame | family;
                bindings.Add(new UnityAssetBinding(id, model)); entries.Add(new AvatarPartCatalogEntry(id, slot, category, required, required, 1, optional));
            }
            if (entries.Count < 50) throw new InvalidOperationException($"Expected modular fixture parts, found {entries.Count}.");

            UnityAssetCatalog assetCatalog = LoadOrCreate<UnityAssetCatalog>($"{ContentRoot}/SystemG6UnityAssetCatalog.asset"); assetCatalog.Configure(10, bindings);
            AvatarPartCatalog partCatalog = LoadOrCreate<AvatarPartCatalog>($"{ContentRoot}/SystemG6AvatarPartCatalog.asset"); partCatalog.Configure(entries);
            EditorUtility.SetDirty(assetCatalog); EditorUtility.SetDirty(partCatalog); AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject galleryRoot = new GameObject("Avatar Fixture Gallery"); Transform avatarAnchor = new GameObject("Avatar Anchor").transform;
            avatarAnchor.SetParent(galleryRoot.transform, false); avatarAnchor.rotation = Quaternion.Euler(0f, 180f, 0f);
            AvatarFixtureGallery gallery = galleryRoot.AddComponent<AvatarFixtureGallery>(); gallery.Configure(assetCatalog, partCatalog, avatarAnchor);
            Camera camera = new GameObject("Gallery Camera").AddComponent<Camera>(); camera.transform.SetPositionAndRotation(new Vector3(0f, 1.25f, -4.5f), Quaternion.Euler(4f, 0f, 0f)); camera.tag = "MainCamera";
            Light key = new GameObject("Key Light").AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.2f; key.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder); floor.name = "Gallery Platform"; floor.transform.position = new Vector3(0f, -0.08f, 0f); floor.transform.localScale = new Vector3(1.5f, 0.08f, 1.5f);
            EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets(); Debug.Log($"Avatar fixture gallery built with {entries.Count} parts at {ScenePath}.");
        }

        public static void BuildFromCommandLine() { Build(); }

        private static bool TryClassify(string name, out CharacterPartSlot slot, out AssetCategory category, out bool optional)
        {
            optional = false;
            if (name.Contains("hair")) { slot = CharacterPartSlot.Hair; category = AssetCategory.Hair; optional = true; return true; }
            if (name.Contains("head")) { slot = CharacterPartSlot.Head; category = AssetCategory.Head; return true; }
            if (name.Contains("body")) { slot = CharacterPartSlot.Body; category = AssetCategory.CharacterBody; return true; }
            if (name.Contains("armor")) { slot = CharacterPartSlot.UpperClothing; category = AssetCategory.Equipment; return true; }
            if (name.Contains("boots") || name.Contains("feet")) { slot = CharacterPartSlot.Feet; category = AssetCategory.Equipment; return true; }
            if (name.Contains("gloves") || name.Contains("hands")) { slot = CharacterPartSlot.Hands; category = name.Contains("gloves") ? AssetCategory.Equipment : AssetCategory.CharacterBody; return true; }
            if (name.Contains("helmet")) { slot = CharacterPartSlot.Accessory; category = AssetCategory.Equipment; optional = true; return true; }
            slot = default; category = default; return false;
        }

        private static uint StableAssetId(string value)
        { unchecked { uint hash = 2166136261; for (int i = 0; i < value.Length; i++) { hash ^= char.ToLowerInvariant(value[i]); hash *= 16777619; } return 0x40000000u | (hash & 0x3fffffffu); } }
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        { T asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset != null) return asset; asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset; }
        private static void EnsureFolder(string parent, string child) { string path = $"{parent}/{child}"; if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child); }
    }
}

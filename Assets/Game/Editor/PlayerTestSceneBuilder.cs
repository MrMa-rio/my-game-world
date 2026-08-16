using MyGameWorld.Client.PlayerRuntime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MyGameWorld.Editor
{
    public static class PlayerTestSceneBuilder
    {
        [MenuItem("My Game World/Build Player Test Scene")]
        public static void Build()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreatePrimitive("Flat Terrain", PrimitiveType.Cube, new Vector3(0f, -0.5f, 0f), new Vector3(80f, 1f, 80f));
            CreatePrimitive("Walkable Slope", PrimitiveType.Cube, new Vector3(10f, 1f, 2f), new Vector3(10f, 1f, 8f), new Vector3(0f, 0f, 18f));
            CreatePrimitive("Blocked Slope", PrimitiveType.Cube, new Vector3(22f, 3f, 2f), new Vector3(10f, 1f, 8f), new Vector3(0f, 0f, 58f));
            for (int index = 0; index < 7; index++) CreatePrimitive($"Stair {index + 1}", PrimitiveType.Cube,
                new Vector3(-10f, index * 0.2f, 4f + index * 0.65f), new Vector3(4f, 0.2f, 0.65f));
            CreatePrimitive("Rock", PrimitiveType.Sphere, new Vector3(5f, 1f, 12f), new Vector3(3f, 2f, 2.5f));
            CreatePrimitive("Wall", PrimitiveType.Cube, new Vector3(-2f, 2f, 16f), new Vector3(12f, 4f, 0.5f));
            CreatePrimitive("Bush Soft Area", PrimitiveType.Sphere, new Vector3(-14f, 0.8f, -4f), new Vector3(3f, 1.5f, 3f));
            CreatePrimitive("Tree Trunk", PrimitiveType.Cylinder, new Vector3(-18f, 2f, 8f), new Vector3(1.5f, 4f, 1.5f));
            CreatePrimitive("Platform", PrimitiveType.Cube, new Vector3(8f, 4f, -14f), new Vector3(10f, 0.5f, 10f));
            CreatePrimitive("Small Drop Landing", PrimitiveType.Cube, new Vector3(8f, 1.5f, -23f), new Vector3(10f, 0.5f, 8f));
            CreatePrimitive("Large Drop Landing", PrimitiveType.Cube, new Vector3(22f, 0f, -20f), new Vector3(10f, 0.5f, 10f));
            GameObject light = new GameObject("Directional Light"); Light sun = light.AddComponent<Light>(); sun.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            GameObject bootstrap = new GameObject("Player Test Bootstrap"); PlayerTestSceneBootstrap component = bootstrap.AddComponent<PlayerTestSceneBootstrap>();
            component.SetInputActions(AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions"));
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, "Assets/Scenes/PlayerTestScene.unity");
            AssetDatabase.SaveAssets();
        }
        private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Vector3 rotation = default)
        { GameObject item = GameObject.CreatePrimitive(type); item.name = name; item.transform.position = position; item.transform.localScale = scale; item.transform.eulerAngles = rotation; return item; }
    }
}

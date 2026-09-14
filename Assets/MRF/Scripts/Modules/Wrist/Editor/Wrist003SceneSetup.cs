#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MRF.Modules.Wrist.Editor
{
    /// <summary>Explicit, one-time migration of the user's 003Watch scene. Never runs on import.</summary>
    public static class Wrist003SceneSetup
    {
        private const string ScenePath = "Assets/MRF/Scenes/Watch/003Watch.unity";
        [MenuItem("Tools/MRF/Modules/Configure 003Watch")]
        public static void Configure()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded || EditorApplication.isPlaying)
                throw new InvalidOperationException("Open 003Watch in Edit Mode before configuring it.");
            if (Find<SceneModuleManager>(scene) != null)
            {
                Selection.activeGameObject = Find<SceneModuleManager>(scene).gameObject;
                return;
            }
            var oldWatch = Find<WatchController>(scene);
            var oldMenu = Find<WatchShortcutMenuController>(scene);
            var hand = Find<DominantHandWristSelector>(scene);
            if (oldWatch == null || oldMenu == null || hand == null) throw new InvalidOperationException("Expected the existing watch, menu and hand selector.");
            var screen = oldWatch.GetComponentsInChildren<Transform>(true).Single(x => x.name == "Screen");
            var renderer = screen.GetComponent<Renderer>();
            if (renderer == null) throw new InvalidOperationException("The watch Screen has no renderer.");
            var oldMenuData = new SerializedObject(oldMenu);
            var anchor = (Transform)oldMenuData.FindProperty("wristMenuAnchor").objectReferenceValue;
            var attachment = (Transform)oldMenuData.FindProperty("menuAttachment").objectReferenceValue;
            var panel = (RectTransform)oldMenuData.FindProperty("menuRoot").objectReferenceValue;
            var canvas = (CanvasGroup)oldMenuData.FindProperty("canvasGroup").objectReferenceValue;
            Vector3 offset = Quaternion.Inverse(anchor.rotation) * (attachment.position - anchor.position);
            Vector3 rotation = (Quaternion.Inverse(anchor.rotation) * attachment.rotation).eulerAngles;
            Directory.CreateDirectory("Temp/WristModuleValidation");
            // Preserve a complete copy, including any currently unsaved user edits, before migration.
            EditorSceneManager.SaveScene(scene, "Temp/WristModuleValidation/003Watch-before.unity", true);
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Configure modular wrist system in 003Watch");
            var global = NewObject("SceneModules", null, scene);
            var manager = Undo.AddComponent<SceneModuleManager>(global);
            var module = manager;
            var menuHost = NewObject("Shortcuts", null, scene);
            var menu = Undo.AddComponent<DeviceAnchoredMenuPresenter>(menuHost);
            var content = Undo.AddComponent<ShortcutMenuContent>(menuHost);
            EditorUtility.CopySerializedManagedFieldsOnly(oldMenu, content);
            var oldToggle = oldWatch.GetComponent<InspectorGameObjectToggle>();
            if (oldToggle != null)
            {
                var toggle = Undo.AddComponent<InspectorGameObjectToggle>(menuHost);
                EditorUtility.CopySerializedManagedFieldsOnly(oldToggle, toggle);
                var contents = new SerializedObject(content);
                var iterator = contents.GetIterator();
                while (iterator.Next(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == oldToggle)
                        iterator.objectReferenceValue = toggle;
                contents.ApplyModifiedPropertiesWithoutUndo();
                Undo.DestroyObjectImmediate(oldToggle);
            }
            var slot = NewObject("WristDeviceSlot", oldWatch.transform.parent, scene);
            var deviceRoot = NewObject("WatchDevice", slot.transform, scene);
            Undo.SetTransformParent(oldWatch.transform, deviceRoot.transform, "Place watch visual inside device");
            var device = Undo.AddComponent<WristDevice>(deviceRoot);
            Undo.SetTransformParent(anchor, deviceRoot.transform, "Move device menu anchor");
            Undo.SetTransformParent(attachment, menuHost.transform, "Separate menu from device prefab");
            var inputRoot = NewObject("ActivationSurface", deviceRoot.transform, scene);
            Bounds bounds = renderer.localBounds;
            inputRoot.transform.position = screen.parent.TransformPoint(screen.localPosition + screen.localRotation *
                Vector3.Scale(new Vector3(bounds.center.x, bounds.max.y + 0.003f, bounds.center.z), screen.localScale));
            inputRoot.transform.rotation = screen.rotation * Quaternion.Euler(-90, 0, 0);
            Vector3 scale = screen.lossyScale;
            Vector3 parentScale = deviceRoot.transform.lossyScale;
            inputRoot.transform.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, scale.z / parentScale.z);
            var input = Undo.AddComponent<MetaWristPressInput>(inputRoot);
            ConfigureObject(input, data =>
            {
                data.FindProperty("surface").objectReferenceValue = inputRoot.transform;
                data.FindProperty("radius").floatValue = Mathf.Min(bounds.extents.x, bounds.extents.z);
            });
            var stylesRoot = NewObject("Styles", deviceRoot.transform, scene);
            var led = Undo.AddComponent<LedCubesStyle>(NewObject("LedCubes", stylesRoot.transform, scene));
            EditorUtility.CopySerializedManagedFieldsOnly(oldWatch, led);
            ConfigureObject(led, data =>
            {
                data.FindProperty("styleId").stringValue = "led-cubes";
                data.FindProperty("displayName").stringValue = "LedCubes";
                data.FindProperty("screen").objectReferenceValue = screen;
                data.FindProperty("screenRenderer").objectReferenceValue = renderer;
            });
            var bw = Undo.AddComponent<BlackAndWhiteStyle>(NewObject("BlackAndWhite", stylesRoot.transform, scene));
            ConfigureObject(bw, data =>
            {
                data.FindProperty("styleId").stringValue = "black-and-white";
                data.FindProperty("displayName").stringValue = "BlackAndWhite";
                data.FindProperty("screen").objectReferenceValue = renderer;
                data.FindProperty("unlitShader").objectReferenceValue = Shader.Find("Universal Render Pipeline/Unlit");
            });
            ConfigureObject(device, data =>
            {
                data.FindProperty("deviceId").stringValue = "watch";
                data.FindProperty("input").objectReferenceValue = input;
                data.FindProperty("menuAnchor").objectReferenceValue = anchor;
                var styles = data.FindProperty("styles"); styles.arraySize = 2;
                styles.GetArrayElementAtIndex(0).objectReferenceValue = led;
                styles.GetArrayElementAtIndex(1).objectReferenceValue = bw;
            });
            ConfigureObject(hand, data => data.FindProperty("watchVisual").objectReferenceValue = slot.transform);
            ConfigureObject(menu, data =>
            {
                data.FindProperty("menuId").stringValue = "shortcuts";
                data.FindProperty("attachment").objectReferenceValue = attachment;
                data.FindProperty("panel").objectReferenceValue = panel;
                data.FindProperty("canvasGroup").objectReferenceValue = canvas;
                data.FindProperty("offsetMeters").vector3Value = offset;
                data.FindProperty("rotationOffset").vector3Value = rotation;
                data.FindProperty("widthMeters").floatValue = oldMenuData.FindProperty("panelWidthMeters").floatValue * oldMenuData.FindProperty("panelScaleMultiplier").floatValue;
            });
            ConfigureObject(module, data =>
            {
                data.FindProperty("deviceSlot").objectReferenceValue = slot.transform;
                var devices = data.FindProperty("devices"); devices.arraySize = 1;
                devices.GetArrayElementAtIndex(0).objectReferenceValue = device;
                var menus = data.FindProperty("menus"); menus.arraySize = 1;
                menus.GetArrayElementAtIndex(0).objectReferenceValue = menu;
            });
            var oldManager = Find<WatchModuleManager>(scene);
            if (oldManager != null) Undo.DestroyObjectImmediate(oldManager);
            Undo.DestroyObjectImmediate(oldMenu);
            Undo.DestroyObjectImmediate(oldWatch);
            Directory.CreateDirectory("Assets/MRF/Presets/Wrist");
            Directory.CreateDirectory("Assets/MRF/Prefabs/Modules/Wrist");
            AssetDatabase.Refresh();
            var ledPreset = CreatePreset("Watch-LedCubes", "led-cubes");
            CreatePreset("Watch-BlackAndWhite", "black-and-white");
            PrefabUtility.SaveAsPrefabAssetAndConnect(deviceRoot, "Assets/MRF/Prefabs/Modules/Wrist/WatchDevice.prefab", InteractionMode.AutomatedAction);
            menu.SetOpen(false);
            if (!module.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
            Undo.CollapseUndoOperations(undo);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = global;
            WristModuleChecks.ValidateBindings();
            File.WriteAllText("Temp/WristModuleValidation/setup.txt", "003Watch configured: global manager, device prefab, two styles, independent menu and client presets.");
            Debug.Log("003Watch modular wrist setup complete. Select SceneModules to configure it.");
        }
        private static WristModulePreset CreatePreset(string name, string style)
        {
            string path = "Assets/MRF/Presets/Wrist/" + name + ".asset";
            var preset = AssetDatabase.LoadAssetAtPath<WristModulePreset>(path);
            if (preset != null) return preset;
            preset = ScriptableObject.CreateInstance<WristModulePreset>();
            preset.Capture(new WristModuleSettings { styleId = style });
            AssetDatabase.CreateAsset(preset, path);
            return preset;
        }
        private static void ConfigureObject(UnityEngine.Object target, Action<SerializedObject> configure)
        {
            Undo.RecordObject(target, "Configure module references");
            var data = new SerializedObject(target);
            configure(data);
            data.ApplyModifiedProperties();
        }
        private static GameObject NewObject(string name, Transform parent, Scene scene)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create module object");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetParent(parent, false);
            return go;
        }
        private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(x => x.GetComponentsInChildren<T>(true)).SingleOrDefault();
    }
}
#endif

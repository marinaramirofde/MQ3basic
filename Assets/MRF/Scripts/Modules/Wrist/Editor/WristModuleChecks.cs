#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MRF.Modules.Wrist.Editor
{
    /// <summary>Explicit checks for the configured scene and a temporary Play Mode session.</summary>
    [InitializeOnLoad]
    public static class WristModuleChecks
    {
        private const string RunningKey = "MRF.WristModuleChecks.Running";
        static WristModuleChecks() => EditorApplication.playModeStateChanged += OnPlayState;
        private static int checks;
        private static double runAfter;
        [MenuItem("Tools/MRF/Modules/Validate 003Watch Bindings")]
        public static void ValidateBindings()
        {
            checks = 0;
            var manager = GetManager();
            var module = manager;
            Check(manager.transform.parent == null, "Manager is a scene root");
            Check(module != null && module.ValidateConfiguration(out _), "Module configuration is valid");
            Check(module.Devices.Length > 0, "Device catalogue is populated");
            foreach (var device in module.Devices)
            {
                Check(PrefabUtility.IsPartOfPrefabInstance(device), "Device is a prefab instance");
                Check(device.FindStyle("led-cubes") != null, "LedCubes is installed");
                Check(device.FindStyle("black-and-white") != null, "BlackAndWhite is installed");
                foreach (var style in device.Styles)
                {
                    var serialized = new SerializedObject(style);
                    string[] required = style is LedCubesStyle ? new[] { "screen", "screenRenderer", "indicatorShader" } : new[] { "screen", "unlitShader" };
                    foreach (string field in required)
                        Check(serialized.FindProperty(field).objectReferenceValue != null, style.DisplayName + " has " + field);
                }
            }
            var scene = manager.gameObject.scene;
            Check(!scene.GetRootGameObjects().Any(x => x.GetComponentInChildren<WatchController>(true) != null), "003Watch has no legacy watch controller");
            Check(!scene.GetRootGameObjects().Any(x => x.GetComponentInChildren<WatchShortcutMenuController>(true) != null), "003Watch has no legacy menu controller");
            Check(module.Menus.All(x => !x.transform.IsChildOf(module.ActiveDevice != null ? module.ActiveDevice.transform : module.Devices[0].transform)), "Menus have independent hosts");
            WriteReport("bindings.txt", checks + " binding checks passed.");
        }
        [MenuItem("Tools/MRF/Modules/Run 003Watch Play Checks")]
        public static void StartPlayChecks()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start checks from Edit Mode.");
            ValidateBindings();
            SessionState.SetBool(RunningKey, true);
            EditorApplication.isPlaying = true;
        }
        private static void OnPlayState(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(RunningKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                runAfter = EditorApplication.timeSinceStartup + 1;
                EditorApplication.update += RunWhenReady;
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.update -= RunWhenReady;
                SessionState.SetBool(RunningKey, false);
            }
        }
        private static void RunWhenReady()
        {
            if (EditorApplication.timeSinceStartup < runAfter) return;
            EditorApplication.update -= RunWhenReady;
            checks = 0;
            try
            {
                var manager = GetManager();
                var module = manager;
                var ledPreset = AssetDatabase.LoadAssetAtPath<WristModulePreset>("Assets/MRF/Presets/Wrist/Watch-LedCubes.asset");
                module.LoadPreset(ledPreset);
                Check(!module.IsOn && module.ActiveDevice != null, "Starts off with a device");
                module.TogglePower();
                Check(module.IsOn && module.ActiveMenu.IsOpen, "Activation opens selected menu");
                Check(module.ActiveStyle.IsAnimating, "LedCubes animates");
                module.TogglePower();
                Check(!module.IsOn && !module.ActiveMenu.IsOpen, "Second activation reverses power");
                module.TogglePower();
                module.SelectStyle("black-and-white");
                Check(module.IsOn && module.ActiveStyle is BlackAndWhiteStyle, "Style swap preserves active power");
                Check(!module.ActiveStyle.IsAnimating, "BlackAndWhite has no animation");
                var screen = (Renderer)new SerializedObject(module.ActiveStyle).FindProperty("screen").objectReferenceValue;
                Check(screen.sharedMaterial.GetColor("_BaseColor") == Color.white, "BlackAndWhite ON is white");
                module.SetActivationEnabled(false);
                Check(!module.IsOn && !module.ActiveMenu.IsOpen, "Disabling activation closes menu");
                module.TogglePower();
                Check(!module.IsOn, "Disabled activation rejects presses");
                Check(screen.sharedMaterial.GetColor("_BaseColor") == Color.black, "BlackAndWhite OFF is black");
                module.SetActivationEnabled(true);
                Check(!module.IsOn, "Re-enabling does not reopen the menu");
                module.SelectStyle("led-cubes");
                module.SetAnimationsEnabled(false);
                module.TogglePower();
                Check(module.IsOn && !module.ActiveStyle.IsAnimating, "Animation checkbox preserves instant activation");
                module.SetMenuEnabled(false);
                Check(module.IsOn && module.ActiveMenu == null, "Menu is optional independently of power");
                module.SetDeviceEnabled(false);
                Check(!module.IsOn && module.ActiveDevice == null, "Device can be hidden entirely");
                module.SetDeviceEnabled(true);
                Check(!module.IsOn && module.ActiveDevice != null, "Device returns in the off state");
                module.SetMenuEnabled(true);
                CheckDeviceAndMenuReplacement(module);
                module.TogglePower();
                manager.enabled = false;
                Check(!module.IsOn && module.ActiveDevice == null, "Global manager shutdown releases the module");
                manager.enabled = true;
                Check(module.ActiveDevice != null && !module.IsOn, "Global manager restart is clean");
                WriteReport("playmode.txt", checks + " Play Mode checks passed. Physical XR input still requires a headset check.");
            }
            catch (Exception error)
            {
                WriteReport("playmode.txt", "FAILED after " + checks + " checks: " + error);
                Debug.LogException(error);
            }
            finally { EditorApplication.isPlaying = false; }
        }
        private static void CheckDeviceAndMenuReplacement(SceneModuleManager module)
        {
            var originalDevice = module.ActiveDevice;
            var originalMenu = module.ActiveMenu;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MRF/Prefabs/Modules/Wrist/WatchDevice.prefab");
            var temporary = UnityEngine.Object.Instantiate(prefab, originalDevice.transform.parent);
            var alternate = temporary.GetComponent<WristDevice>();
            var deviceData = new SerializedObject(alternate);
            deviceData.FindProperty("deviceId").stringValue = "test-alternate";
            deviceData.ApplyModifiedPropertiesWithoutUndo();
            var service = new GameObject("Temporary Menu Test");
            var attachment = new GameObject("Temporary Attachment");
            var panel = new GameObject("Temporary Panel", typeof(RectTransform), typeof(CanvasGroup));
            panel.transform.SetParent(attachment.transform, false);
            var menu = service.AddComponent<DeviceAnchoredMenuPresenter>();
            var menuData = new SerializedObject(menu);
            menuData.FindProperty("menuId").stringValue = "test-menu";
            menuData.FindProperty("attachment").objectReferenceValue = attachment.transform;
            menuData.FindProperty("panel").objectReferenceValue = panel.GetComponent<RectTransform>();
            menuData.FindProperty("canvasGroup").objectReferenceValue = panel.GetComponent<CanvasGroup>();
            menuData.ApplyModifiedPropertiesWithoutUndo();
            var data = new SerializedObject(module);
            var devices = data.FindProperty("devices"); devices.arraySize = 2;
            devices.GetArrayElementAtIndex(1).objectReferenceValue = alternate;
            var menus = data.FindProperty("menus"); menus.arraySize = 2;
            menus.GetArrayElementAtIndex(1).objectReferenceValue = menu;
            data.ApplyModifiedPropertiesWithoutUndo();
            module.SetPower(true);
            module.SelectMenu("test-menu");
            Check(menu.IsOpen && !originalMenu.IsOpen, "Switching menus closes the old menu");
            module.SelectDevice("test-alternate");
            Check(module.ActiveDevice == alternate && !originalDevice.gameObject.activeSelf && !module.IsOn, "Replacing the device resets power and hides the previous device");
            module.SetPower(true);
            Check(menu.IsOpen && attachment.transform.parent == alternate.MenuAnchor, "Menu follows the replacement device anchor");
            module.SelectDevice(originalDevice.Id);
            module.SelectMenu(originalMenu.Id);
            data.Update();
            data.FindProperty("devices").arraySize = 1;
            data.FindProperty("menus").arraySize = 1;
            data.ApplyModifiedPropertiesWithoutUndo();
            UnityEngine.Object.Destroy(temporary);
            UnityEngine.Object.Destroy(service);
            UnityEngine.Object.Destroy(attachment);
        }
        private static SceneModuleManager GetManager()
        {
            var scene = SceneManager.GetSceneByPath("Assets/MRF/Scenes/Watch/003Watch.unity");
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open 003Watch first.");
            return scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<SceneModuleManager>(true)).Single();
        }
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            checks++;
        }
        private static void WriteReport(string file, string text)
        {
            Directory.CreateDirectory("Temp/WristModuleValidation");
            File.WriteAllText("Temp/WristModuleValidation/" + file, text);
            Debug.Log(text);
        }
    }
}
#endif

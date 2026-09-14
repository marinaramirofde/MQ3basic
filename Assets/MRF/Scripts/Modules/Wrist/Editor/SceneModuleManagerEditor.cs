#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace MRF.Modules.Wrist.Editor
{
    [CustomEditor(typeof(SceneModuleManager))]
    public sealed class SceneModuleManagerEditor : UnityEditor.Editor
    {
        private readonly WristConfigurationPanel panel = new WristConfigurationPanel();
        public override void OnInspectorGUI()
        {
            panel.Draw((SceneModuleManager)target);
        }
    }

    [CustomEditor(typeof(WristDevice))]
    public sealed class WristDeviceEditor : UnityEditor.Editor
    {
        private bool wiring;
        public override void OnInspectorGUI()
        {
            var device = (WristDevice)target;
            EditorGUILayout.LabelField(device.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This prefab defines the device and its compatible styles. Its name is used in the model selector. Customize the scene from SceneModules.", MessageType.None);
            EditorGUILayout.LabelField("Available styles", EditorStyles.boldLabel);
            var styles = device.Styles ?? Array.Empty<WristStyle>();
            if (styles.Length == 0) EditorGUILayout.HelpBox("No styles have been assigned to this device.", MessageType.Info);
            foreach (var style in styles)
            {
                if (style == null)
                {
                    EditorGUILayout.LabelField("Missing style reference");
                    continue;
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(style.DisplayName);
                if (GUILayout.Button("Locate", GUILayout.Width(60))) EditorGUIUtility.PingObject(style.gameObject);
                EditorGUILayout.EndHorizontal();
            }
            wiring = EditorGUILayout.Foldout(wiring, "Prefab wiring", true);
            if (!wiring) return;
            EditorGUILayout.HelpBox("Style references point to configured components on this device, not C# source files. Edit these bindings when authoring a device prefab.", MessageType.None);
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("input"), new GUIContent("Device input"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("menuAnchor"), new GUIContent("Menu attachment point"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("styles"), new GUIContent("Style components"), true);
            serializedObject.ApplyModifiedProperties();
        }
    }

    internal sealed class WristConfigurationPanel
    {
        private bool appearance = true, menuDetails, catalogue, copies;
        private WristModulePreset savedSelection;
        private UnityEngine.Object deviceToAdd, styleToAdd, menuToAdd;
        private Action registerOption;
        private string registrationMessage;
        private bool registrationFailed;
        private bool structure;
        public void Draw(SceneModuleManager module)
        {
            var data = new SerializedObject(module);
            data.Update();
            var config = data.FindProperty("settings");
            bool refresh = false;
            EditorGUILayout.LabelField("Scene customization", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Choose a device and configure its appearance and behavior here. Changes are saved in this scene.", MessageType.None);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("1. Device", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(config.FindPropertyRelative("deviceEnabled"), new GUIContent("Show on wrist"));
            var devices = module.Devices?.Where(x => x != null).ToArray() ?? Array.Empty<WristDevice>();
            bool changedDevice = Choice("Model", config.FindPropertyRelative("deviceId"), devices.Select(x => x.Id).ToArray(), devices.Select(x => Label(x.DisplayName, x)).ToArray());
            var device = devices.FirstOrDefault(x => x.Id == config.FindPropertyRelative("deviceId").stringValue);
            var styles = device?.Styles?.Where(x => x != null).ToArray() ?? Array.Empty<WristStyle>();
            if (changedDevice && !styles.Any(x => x.Id == config.FindPropertyRelative("styleId").stringValue))
                config.FindPropertyRelative("styleId").stringValue = styles.FirstOrDefault()?.Id ?? "";
            DrawAddField("Add model", "Prepared device prefab or device from this scene.", ref deviceToAdd,
                source => WristOptionRegistration.AddDevice(module, source));
            if (changedDevice) styleToAdd = null;
            if (device == null)
                EditorGUILayout.HelpBox("Select a model to configure its appearance and interaction.", MessageType.Info);
            else
            {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(Label(device.DisplayName, device) + " settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Appearance for " + Label(device.DisplayName, device), EditorStyles.boldLabel);
            Choice("Style", config.FindPropertyRelative("styleId"), styles.Select(x => x.Id).ToArray(), styles.Select(x => Label(x.DisplayName, x)).ToArray());
            EditorGUILayout.PropertyField(config.FindPropertyRelative("animationsEnabled"), new GUIContent("Animate changes", "Allow the selected style to play its animations, if any."));
            var style = styles.FirstOrDefault(x => x.Id == config.FindPropertyRelative("styleId").stringValue);
            appearance = EditorGUILayout.Foldout(appearance, "Adjust selected style", true);
            if (appearance && style != null)
                refresh |= DrawStyle(style);
            using (new EditorGUI.DisabledScope(device == null))
                DrawAddField("Add style", "Configured style component inside the selected device.", ref styleToAdd,
                    source => WristOptionRegistration.AddStyle(module, device, source));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Interaction for " + Label(device.DisplayName, device), EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Input source", device.Input != null ? ObjectNames.NicifyVariableName(device.Input.GetType().Name) : "No input source configured");
            EditorGUILayout.PropertyField(config.FindPropertyRelative("activationEnabled"), new GUIContent("Enable device activation", "Uses this model's input source. When disabled, the device stays visible and powered off."));
            EditorGUILayout.PropertyField(data.FindProperty("whenPowerChanged"), new GUIContent("Actions on power change"), true);
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("2. Menu opened by the device", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(config.FindPropertyRelative("menuEnabled"), new GUIContent("Open menu on activation"));
            var menus = module.Menus?.Where(x => x != null).ToArray() ?? Array.Empty<DeviceAnchoredMenuPresenter>();
            using (new EditorGUI.DisabledScope(!config.FindPropertyRelative("menuEnabled").boolValue))
            {
                Choice("Menu", config.FindPropertyRelative("menuId"), menus.Select(x => x.Id).ToArray(), menus.Select(x => Label(x.DisplayName, x)).ToArray());
                var menu = menus.FirstOrDefault(x => x.Id == config.FindPropertyRelative("menuId").stringValue);
                menuDetails = EditorGUILayout.Foldout(menuDetails, "Size, position and content", true);
                if (menuDetails && menu != null) refresh |= DrawMenu(menu);
            }
            DrawAddField("Add menu", "Complete menu prefab or menu service from this scene.", ref menuToAdd,
                source => WristOptionRegistration.AddMenu(module, source));
            EditorGUILayout.EndVertical();
            EditorGUI.EndChangeCheck();
            bool changed = data.ApplyModifiedProperties();
            if (changed || refresh) module.ApplyConfiguration();
            if (registerOption != null)
            {
                var action = registerOption;
                registerOption = null;
                try
                {
                    action();
                    deviceToAdd = styleToAdd = menuToAdd = null;
                    registrationMessage = "Option added and selected. Undo with Ctrl+Z. It will become active when Play Mode starts.";
                    registrationFailed = false;
                }
                catch (Exception error) { registrationMessage = error.Message; registrationFailed = true; }
            }
            if (!string.IsNullOrEmpty(registrationMessage))
                EditorGUILayout.HelpBox(registrationMessage, registrationFailed ? MessageType.Warning : MessageType.Info);
            structure = EditorGUILayout.Foldout(structure, "How the wrist hierarchy works", true);
            if (structure)
                EditorGUILayout.HelpBox("WristAnchor: hand tracking.\nWatchMount: calibration for that hand.\nWristDeviceSlot: container that switches hands.\nWatchDevice: interchangeable device.\nWatchVisual: model; ActivationSurface: press input; WristMenuAnchor: menu attachment point; Styles: available appearances.\n\nCustomize here. Adjust the mount only for wrist calibration; each device design belongs to its prefab.", MessageType.None);

            if (structure && GUILayout.Button("Open getting started guide"))
            {
                var guide = AssetDatabase.LoadMainAssetAtPath("Assets/MRF/Scripts/Modules/Wrist/QUICK_START.md");
                if (guide != null) AssetDatabase.OpenAsset(guide);
            }
            DrawCatalogue(module);
            DrawCopies(module);
            if (!module.ValidateConfiguration(out _))
                EditorGUILayout.HelpBox("An option is not configured. Check the selectors and the available options in this scene.", MessageType.Warning);
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("State", module.IsOn ? "On" : "Off");
                if (GUILayout.Button("Test activation")) module.TogglePower();
            }
            else EditorGUILayout.HelpBox("Test activation in Play Mode. Visual changes are applied when the scene starts.", MessageType.None);
        }

        private void DrawAddField(string label, string tooltip, ref UnityEngine.Object value, Action<UnityEngine.Object> add)
        {
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.BeginHorizontal();
                value = EditorGUILayout.ObjectField(new GUIContent(label, tooltip), value, typeof(UnityEngine.Object), true);
                using (new EditorGUI.DisabledScope(value == null))
                if (GUILayout.Button("Add", GUILayout.Width(60)))
                {
                    var selected = value;
                    registerOption = () => add(selected);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private static bool DrawStyle(WristStyle style)
        {
            var data = new SerializedObject(style);
            data.Update();
            var property = data.GetIterator();
            bool found = false;
            if (property.NextVisible(true)) do
            {
                if (property.name == "m_Script" || property.name == "displayName" || property.name == "styleId" ||
                    property.propertyType == SerializedPropertyType.ObjectReference) continue;
                EditorGUILayout.PropertyField(property, new GUIContent(StyleLabel(property.name, property.displayName)), true);
                found = true;
            } while (property.NextVisible(false));
            if (!found) EditorGUILayout.LabelField("This style has a fixed appearance.", EditorStyles.wordWrappedMiniLabel);
            return data.ApplyModifiedProperties();
        }
        private static string StyleLabel(string name, string fallback)
        {
            switch (name)
            {
                case "duration": return "Duration (seconds)";
                case "offColor": return "Off color";
                case "onColor": return "On color";
                case "emissionIntensity": return "Light intensity";
                case "indicatorSize": return "Indicator size";
                case "surfaceOffset": return "Screen offset";
                case "liftHeight": return "Cube lift height";
                default: return fallback;
            }
        }
        private static bool DrawMenu(DeviceAnchoredMenuPresenter menu)
        {
            var data = new SerializedObject(menu);
            data.Update();
            EditorGUILayout.PropertyField(data.FindProperty("widthMeters"), new GUIContent("Width (meters)"));
            EditorGUILayout.PropertyField(data.FindProperty("offsetMeters"), new GUIContent("Position relative to device"));
            EditorGUILayout.PropertyField(data.FindProperty("rotationOffset"), new GUIContent("Rotation"));
            bool changed = data.ApplyModifiedProperties();
            // Only inspect the content attached to this explicitly selected menu service.
            var content = menu.GetComponent<ShortcutMenuContent>();
            if (content == null) return changed;
            var contents = new SerializedObject(content);
            contents.Update();
            EditorGUILayout.PropertyField(contents.FindProperty("title"), new GUIContent("Title"));
            EditorGUILayout.PropertyField(contents.FindProperty("panelColor"), new GUIContent("Panel color"));
            EditorGUILayout.PropertyField(contents.FindProperty("onColor"), new GUIContent("Border color"));
            EditorGUILayout.PropertyField(contents.FindProperty("borderGlowIntensity"), new GUIContent("Border glow"));
            EditorGUILayout.PropertyField(contents.FindProperty("borderWidth"), new GUIContent("Border width"));
            var shortcuts = contents.FindProperty("shortcuts");
            for (int i = 0; i < shortcuts.arraySize; i++)
            {
                var item = shortcuts.GetArrayElementAtIndex(i);
                item.isExpanded = EditorGUILayout.Foldout(item.isExpanded, "Button " + (i + 1), true);
                if (!item.isExpanded) continue;
                EditorGUILayout.PropertyField(item.FindPropertyRelative("label"), new GUIContent("Text"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("description"), new GUIContent("Description"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("icon"), new GUIContent("Icon"));
                EditorGUILayout.PropertyField(item.FindPropertyRelative("onInvoked"), new GUIContent("On press"), true);
            }
            bool contentChanged = contents.ApplyModifiedProperties();
            if (contentChanged && Application.isPlaying) content.ApplyContent();
            return changed || contentChanged;
        }
        private void DrawCatalogue(SceneModuleManager module)
        {
            catalogue = EditorGUILayout.Foldout(catalogue, "Available options in this scene", true);
            if (!catalogue) return;
            EditorGUILayout.HelpBox("Extend this list when adding a new model or menu. Each device contains its compatible styles. Existing options can be selected without editing this list.", MessageType.None);
            var data = new SerializedObject(module);
            data.Update();
            EditorGUILayout.PropertyField(data.FindProperty("deviceSlot"), new GUIContent("Device slot"));
            EditorGUILayout.PropertyField(data.FindProperty("devices"), new GUIContent("Available devices"), true);
            EditorGUILayout.PropertyField(data.FindProperty("menus"), new GUIContent("Available menus"), true);
            if (data.ApplyModifiedProperties())
            {
                EnsureCatalogueIdentities(module);
                module.ApplyConfiguration();
            }
        }
        private static void EnsureCatalogueIdentities(SceneModuleManager module)
        {
            EnsureIdentities(module.Devices ?? Array.Empty<WristDevice>(), "deviceId");
            EnsureIdentities(module.Menus ?? Array.Empty<DeviceAnchoredMenuPresenter>(), "menuId");
            foreach (var device in module.Devices ?? Array.Empty<WristDevice>())
                if (device != null) EnsureIdentities(device.Styles ?? Array.Empty<WristStyle>(), "styleId");
        }
        private static void EnsureIdentities(UnityEngine.Object[] entries, string field)
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            var objects = new System.Collections.Generic.HashSet<UnityEngine.Object>();
            foreach (var entry in entries)
            {
                if (entry == null || !objects.Add(entry)) continue;
                var serialized = new SerializedObject(entry);
                var id = serialized.FindProperty(field);
                if (!string.IsNullOrWhiteSpace(id.stringValue) && ids.Add(id.stringValue)) continue;
                Undo.RecordObject(entry, "Assign option identity");
                id.stringValue = Guid.NewGuid().ToString("N");
                ids.Add(id.stringValue);
                serialized.ApplyModifiedProperties();
            }
        }
        private void DrawCopies(SceneModuleManager module)
        {
            copies = EditorGUILayout.Foldout(copies, "Save or restore selection (optional)", true);
            if (!copies) return;
            EditorGUILayout.HelpBox("A copy stores the selected device, style, menu and checkboxes. Colors, dimensions and content belong to the scene or prefab and are not included. Saving a copy is optional.", MessageType.None);
            savedSelection = (WristModulePreset)EditorGUILayout.ObjectField("Saved selection", savedSelection, typeof(WristModulePreset), false);
            using (new EditorGUI.DisabledScope(savedSelection == null))
            if (GUILayout.Button("Restore selection"))
            {
                Undo.RecordObject(module, "Recover wrist selection");
                module.LoadPreset(savedSelection);
                EditorUtility.SetDirty(module);
            }
            if (GUILayout.Button("Save selection as..."))
            {
                string path = EditorUtility.SaveFilePanelInProject("Save selection", "ClientSelection", "asset", "Choose where to save the copy.");
                if (string.IsNullOrEmpty(path)) return;
                path = AssetDatabase.GenerateUniqueAssetPath(path);
                savedSelection = ScriptableObject.CreateInstance<WristModulePreset>();
                savedSelection.Capture(module.Settings);
                AssetDatabase.CreateAsset(savedSelection, path);
            }
        }
        internal static string Label(string label, UnityEngine.Object fallback) => string.IsNullOrWhiteSpace(label) ? fallback.name : label;
        private static bool Choice(string label, SerializedProperty property, string[] ids, string[] labels)
        {
            int current = Array.IndexOf(ids, property.stringValue);
            var shown = (new[] { current < 0 ? "Select an available option" : "Select an option" }).Concat(labels).ToArray();
            int next = EditorGUILayout.Popup(label, current + 1, shown) - 1;
            if (next < 0 || next == current) return false;
            property.stringValue = ids[next];
            return true;
        }
    }

    [CustomEditor(typeof(WristModulePreset))]
    public sealed class WristModulePresetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var settings = ((WristModulePreset)target).CreateSettings();
            // Editor-only label resolution. No runtime discovery or scene references are stored in assets.
            var modules = Resources.FindObjectsOfTypeAll<SceneModuleManager>().Where(x => !EditorUtility.IsPersistent(x));
            var device = modules.SelectMany(x => x.Devices ?? Array.Empty<WristDevice>()).FirstOrDefault(x => x != null && x.Id == settings.deviceId);
            var style = device?.FindStyle(settings.styleId);
            var menu = modules.SelectMany(x => x.Menus ?? Array.Empty<DeviceAnchoredMenuPresenter>()).FirstOrDefault(x => x != null && x.Id == settings.menuId);
            EditorGUILayout.LabelField("Saved selection", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This asset is an optional copy of a scene's selections. Customize through the SceneModules selectors; no identifiers need to be entered.", MessageType.Info);
            EditorGUILayout.LabelField("Device", device != null ? WristConfigurationPanel.Label(device.DisplayName, device) : "Not available in the open scene");
            EditorGUILayout.LabelField("Style", style != null ? WristConfigurationPanel.Label(style.DisplayName, style) : "Not available in the open scene");
            EditorGUILayout.LabelField("Menu", menu != null ? WristConfigurationPanel.Label(menu.DisplayName, menu) : "Not available in the open scene");
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Show device", settings.deviceEnabled);
                EditorGUILayout.Toggle("Activate on press", settings.activationEnabled);
                EditorGUILayout.Toggle("Animate changes", settings.animationsEnabled);
                EditorGUILayout.Toggle("Open menu", settings.menuEnabled);
            }
        }
    }
}
#endif

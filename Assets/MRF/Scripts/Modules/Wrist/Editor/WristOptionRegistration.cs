#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace MRF.Modules.Wrist.Editor
{
    /// <summary>Explicit Inspector registration. Prefab placement and catalogue changes form one Undo operation.</summary>
    internal static class WristOptionRegistration
    {
        public static void AddDevice(SceneModuleManager module, UnityEngine.Object source)
        {
            Transaction(() =>
            {
                var data = new SerializedObject(module);
                var slot = (Transform)data.FindProperty("deviceSlot").objectReferenceValue;
                if (slot == null) throw new InvalidOperationException("The device slot is missing from this scene.");
                var original = Resolve<WristDevice>(source, "a prepared device with WristDevice on its root; a 3D model alone does not define interaction");
                var device = Place(original, slot, module);
                if (device.transform.parent != slot)
                    throw new InvalidOperationException("Place this scene device directly under WristDeviceSlot. Existing objects are not moved automatically.");
                if (device.Styles == null || device.Styles.Length == 0 || device.Styles.Any(x => x == null || !x.transform.IsChildOf(device.transform)))
                    throw new InvalidOperationException("The device needs at least one configured, device-local style.");
                if (device.Input != null && !device.Input.transform.IsChildOf(device.transform))
                    throw new InvalidOperationException("The input source must belong to this device.");
                if (device.MenuAnchor != null && !device.MenuAnchor.IsChildOf(device.transform))
                    throw new InvalidOperationException("The menu anchor must belong to this device.");
                UniqueId(device, "deviceId", module.Devices.Where(x => x != null && x != device).Select(x => x.Id).ToArray());
                Append(module, "devices", device);
                Select(module, "deviceId", device.Id);
                Select(module, "styleId", device.Styles[0].Id);
                Validate(module);
            });
        }
        public static void AddStyle(SceneModuleManager module, WristDevice device, UnityEngine.Object source)
        {
            Transaction(() =>
            {
                if (device == null) throw new InvalidOperationException("Select a device first.");
                var style = Resolve<WristStyle>(source, "a style component from the selected device");
                if (EditorUtility.IsPersistent(style) || !style.transform.IsChildOf(device.transform) || style.gameObject.scene != module.gameObject.scene)
                    throw new InvalidOperationException("Drag a configured style from inside this device. Its screen references must belong to that model.");
                UniqueId(style, "styleId", device.Styles.Where(x => x != null && x != style).Select(x => x.Id).ToArray());
                Append(device, "styles", style);
                Select(module, "styleId", style.Id);
                Validate(module);
            });
        }
        public static void AddMenu(SceneModuleManager module, UnityEngine.Object source)
        {
            Transaction(() =>
            {
                var original = Resolve<DeviceAnchoredMenuPresenter>(source, "a complete menu with DeviceAnchoredMenuPresenter on its root and configured panel and attachment references");
                var menu = Place(original, null, module);
                if (!menu.IsConfigured) throw new InvalidOperationException("The menu needs a panel, CanvasGroup and attachment. A standalone Canvas does not provide that configuration.");
                UniqueId(menu, "menuId", module.Menus.Where(x => x != null && x != menu).Select(x => x.Id).ToArray());
                Append(module, "menus", menu);
                Select(module, "menuId", menu.Id);
                Validate(module);
            });
        }
        private static T Resolve<T>(UnityEngine.Object source, string description) where T : Component
        {
            var component = source as T;
            if (component == null && source is GameObject go) component = go.GetComponent<T>();
            if (component == null) throw new InvalidOperationException("Drag " + description + ".");
            return component;
        }
        private static T Place<T>(T source, Transform parent, SceneModuleManager module) where T : Component
        {
            if (!EditorUtility.IsPersistent(source))
            {
                if (source.gameObject.scene != module.gameObject.scene)
                    throw new InvalidOperationException("The option must belong to this scene.");
                return source;
            }
            if (!PrefabUtility.IsPartOfPrefabAsset(source) || source.transform.parent != null)
                throw new InvalidOperationException("Drag the root of a prepared prefab.");
            var instance = (GameObject)(parent != null
                ? PrefabUtility.InstantiatePrefab(source.gameObject, parent)
                : PrefabUtility.InstantiatePrefab(source.gameObject, module.gameObject.scene));
            Undo.RegisterCreatedObjectUndo(instance, "Add wrist option");
            // Authored scene configuration is applied on entering Play Mode.
            instance.SetActive(source is WristDevice ? false : source.gameObject.activeSelf);
            return instance.GetComponent<T>();
        }
        private static void Append(UnityEngine.Object owner, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(owner);
            var array = data.FindProperty(field);
            for (int i = 0; i < array.arraySize; i++) if (array.GetArrayElementAtIndex(i).objectReferenceValue == value) return;
            array.arraySize++;
            array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value;
            data.ApplyModifiedProperties();
        }
        private static void Select(SceneModuleManager module, string field, string id)
        {
            var data = new SerializedObject(module);
            data.FindProperty("settings").FindPropertyRelative(field).stringValue = id;
            data.ApplyModifiedProperties();
        }
        private static void UniqueId(UnityEngine.Object owner, string field, string[] occupied)
        {
            var data = new SerializedObject(owner);
            var id = data.FindProperty(field);
            if (!string.IsNullOrWhiteSpace(id.stringValue) && !occupied.Contains(id.stringValue)) return;
            id.stringValue = Guid.NewGuid().ToString("N");
            data.ApplyModifiedProperties();
        }
        private static void Validate(SceneModuleManager module)
        {
            if (!module.ValidateConfiguration(out string error))
                throw new InvalidOperationException("The option was not added: " + error);
        }
        private static void Transaction(Action action)
        {
            if (Application.isPlaying) throw new InvalidOperationException("Add options outside Play Mode to preserve them.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Add wrist option");
            try { action(); Undo.CollapseUndoOperations(group); }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }
    }
}
#endif

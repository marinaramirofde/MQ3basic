#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WatchModuleManager))]
public sealed class WatchModuleManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        DrawPropertiesExcluding(serializedObject, "m_Script", "selectedMenu");
        var options = serializedObject.FindProperty("menus");
        var selection = serializedObject.FindProperty("selectedMenu");
        if (options.arraySize > 0)
        {
            var labels = new string[options.arraySize];
            for (int i = 0; i < labels.Length; i++)
                labels[i] = i + ": " + options.GetArrayElementAtIndex(i).FindPropertyRelative("label").stringValue;
            selection.intValue = EditorGUILayout.Popup("Selected Menu", selection.intValue, labels);
        }
        bool changed = EditorGUI.EndChangeCheck();
        serializedObject.ApplyModifiedProperties();
        if (changed && Application.isPlaying) ((WatchModuleManager)target).ApplyConfiguration();
    }
}
#endif
